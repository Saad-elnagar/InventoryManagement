using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.BLL.Service;

public class InventoryAiAssistant : IInventoryAiAssistant
{
    private const int MaxHistoryMessages = 4;

    private readonly HttpClient _httpClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryAiAssistant> _logger;

    public InventoryAiAssistant(
        HttpClient httpClient,
        IUnitOfWork unitOfWork,
        ILogger<InventoryAiAssistant> logger)
    {
        _httpClient = httpClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string> ChatAsync(
        IReadOnlyCollection<InventoryAiMessageDTO> messages,
        CancellationToken cancellationToken = default)
    {
        var latestUserMessage = messages
            .LastOrDefault(x =>
                x.Role == "user" &&
                !string.IsNullOrWhiteSpace(x.Content))
            ?.Content
            ?.Trim();

        if (string.IsNullOrWhiteSpace(latestUserMessage))
            return "Ask me anything.";

        try
        {
            if (!LooksLikeInventoryQuestion(latestUserMessage))
            {
                return await SendToOllamaAsync(
                    messages,
                    cancellationToken,
                    BuildNormalSystemPrompt());
            }

            var inventoryContext =
                await BuildInventoryContextAsync(
                    latestUserMessage,
                    messages);

            var inventorySystemPrompt =
                $"""
                You are a helpful conversational assistant for an Inventory Management System.

                Answer naturally and directly.
                Use the supplied live database context as the source of truth.
                Never invent inventory values.
                Keep the answer concise unless the user asks for details.
                You may calculate totals, differences, rankings, and trends from the supplied data.
                When LOW STOCK DATA is supplied and the user asks for product names, list the product names
                with their current stock and reorder level. Do not say that names are unavailable when they
                are present in LOW STOCK DATA.
                Use recent conversation context for follow-up questions.
                Answer in the same language as the user's latest message.
                You are read-only.

                {inventoryContext}
                """;

            return await SendToOllamaAsync(
                messages,
                cancellationToken,
                inventorySystemPrompt);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return "The AI assistant took too long to respond. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory AI request failed.");

            throw new InvalidOperationException(
                "Inventory AI is unavailable. Make sure Ollama is running and the configured model is installed.",
                ex);
        }
    }

    private string BuildNormalSystemPrompt()
    {
        return """
        You are a helpful conversational assistant for an Inventory Management System.

        You can have normal conversations too.
        Answer greetings, casual questions, explanations, small talk, and general knowledge naturally.

        For inventory questions, use the live database context supplied by the application.
        Never invent inventory values.
        Answer in the same language as the user.
        You are read-only.
        """;
    }

    private async Task<string> SendToOllamaAsync(
        IReadOnlyCollection<InventoryAiMessageDTO> messages,
        CancellationToken cancellationToken,
        string systemPrompt)
    {
        const string model = "qwen3:4b";

        var ollamaMessages = new List<object>
        {
            new
            {
                role = "system",
                content = systemPrompt
            }
        };

        foreach (var message in messages
                     .Where(x =>
                         !string.IsNullOrWhiteSpace(x.Content) &&
                         (x.Role == "user" || x.Role == "assistant"))
                     .TakeLast(MaxHistoryMessages))
        {
            ollamaMessages.Add(new
            {
                role = message.Role,
                content = message.Content.Trim()
            });
        }

        var payload = new
        {
            model,
            messages = ollamaMessages,
            stream = false,
            think = false,
            keep_alive = "30m",
            options = new
            {
                temperature = 0.2,
                top_p = 0.85,
                repeat_penalty = 1.1,
                num_ctx = 4096,
                num_predict = 240
            }
        };

        HttpResponseMessage response;

        try
        {
            response =
                await _httpClient.PostAsJsonAsync(
                    "/api/chat",
                    payload,
                    cancellationToken);
        }
        catch (TaskCanceledException ex)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                ex,
                "Inventory AI request timed out while waiting for Ollama.");

            return "The AI assistant is taking too long to respond. Please try again.";
        }

        var responseBody =
            await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Ollama request failed. Status: {StatusCode}. Body: {Body}",
                response.StatusCode, responseBody);

            throw new InvalidOperationException(
                $"Ollama returned {(int)response.StatusCode}: {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);

        if (!document.RootElement.TryGetProperty("message", out var messageElement) ||
            !messageElement.TryGetProperty("content", out var contentElement))
        {
            _logger.LogError("Ollama returned an unexpected response: {Body}", responseBody);
            return "I couldn't generate a response.";
        }

        var content = contentElement.GetString();

        return string.IsNullOrWhiteSpace(content)
            ? "I couldn't generate a response."
            : content.Trim();
    }

    private async Task<string> BuildInventoryContextAsync(
        string question,
        IReadOnlyCollection<InventoryAiMessageDTO> messages)
    {
        var q = question.Trim().ToLowerInvariant();

        var recentConversation =
            string.Join(
                " ",
                messages
                    .TakeLast(MaxHistoryMessages)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Content))
                    .Select(x => x.Content.Trim()))
                .ToLowerInvariant();

        var conversationMentionsLowStock =
            recentConversation.Contains("low stock") ||
            recentConversation.Contains("out of stock") ||
            recentConversation.Contains("reorder") ||
            recentConversation.Contains("ناقص") ||
            recentConversation.Contains("نفد") ||
            recentConversation.Contains("إعادة تخزين");

        var isLowStockFollowUp =
            q.Contains("this product") ||
            q.Contains("these products") ||
            q.Contains("which product") ||
            q.Contains("what product") ||
            q.Contains("product name") ||
            q.Contains("product names") ||
            q.Contains("name of this") ||
            q.Contains("name of these") ||
            q.Contains("اسم المنتج") ||
            q.Contains("اسم المنتجات") ||
            q.Contains("المنتج ده") ||
            q.Contains("المنتج دا") ||
            q.Contains("المنتجات دي") ||
            q.Contains("انهي منتج") ||
            q.Contains("أي منتج");
        var contextParts = new List<string>();

        var days = ExtractPeriodDays(q);

        var wantsLowStock =
            q.Contains("low stock") ||
            q.Contains("out of stock") ||
            q.Contains("reorder") ||
            q.Contains("ناقص") ||
            q.Contains("نفد") ||
            q.Contains("إعادة تخزين");

        var wantsMovements =
            q.Contains("movement") ||
            q.Contains("adjustment") ||
            q.Contains("damage") ||
            q.Contains("damaged") ||
            q.Contains("lost") ||
            q.Contains("return") ||
            q.Contains("transfer") ||
            q.Contains("حركة") ||
            q.Contains("تالف") ||
            q.Contains("فاقد") ||
            q.Contains("مرتجع") ||
            q.Contains("تسوية");

        var wantsSales =
            q.Contains("sale") ||
            q.Contains("sales") ||
            q.Contains("sold") ||
            q.Contains("selling") ||
            q.Contains("revenue") ||
            q.Contains("مبيعات") ||
            q.Contains("مباع") ||
            q.Contains("بيع");

        var wantsPurchases =
            q.Contains("purchase") ||
            q.Contains("purchases") ||
            q.Contains("purchased") ||
            q.Contains("buy") ||
            q.Contains("bought") ||
            q.Contains("مشتريات") ||
            q.Contains("شراء");

        var wantsTopSelling =
            q.Contains("top selling") ||
            q.Contains("best selling") ||
            q.Contains("best-selling") ||
            q.Contains("most sold") ||
            q.Contains("الأكثر مبيع") ||
            q.Contains("اكثر مبيع");

        var wantsCategory =
            q.Contains("category") ||
            q.Contains("categories") ||
            q.Contains("فئة") ||
            q.Contains("فئات");

        var wantsOverview =
            q.Contains("inventory") ||
            q.Contains("overall stock") ||
            q.Contains("total stock") ||
            q.Contains("inventory value") ||
            q.Contains("المخزون بالكامل") ||
            q.Contains("إجمالي المخزون") ||
            q.Contains("قيمة المخزون");

        if (wantsLowStock ||
            (isLowStockFollowUp && conversationMentionsLowStock))
        {
            contextParts.Add(
                "LOW STOCK DATA:\n" +
                await GetLowStockProductsAsync());
        }

        if (wantsMovements)
            contextParts.Add(
                $"RECENT MOVEMENTS ({days} DAYS):\n" +
                await GetRecentStockMovementsAsync(days, 20));

        if (wantsTopSelling)
            contextParts.Add(
                $"TOP SELLING PRODUCTS ({days} DAYS):\n" +
                await GetTopSellingProductsAsync(days, 8));
        else if (wantsSales)
            contextParts.Add(
                $"SALES SUMMARY ({days} DAYS):\n" +
                await GetSalesSummaryAsync(days));

        if (wantsPurchases)
            contextParts.Add(
                $"PURCHASE SUMMARY ({days} DAYS):\n" +
                await GetPurchaseSummaryAsync(days));

        if (wantsCategory)
        {
            var categoryName =
                await FindCategoryNameInQuestionAsync(question);

            contextParts.Add(
                "CATEGORY DATA:\n" +
                await GetCategorySummaryAsync(categoryName));
        }

        var wantsProductDetails =
            q.Contains("product") ||
            q.Contains("products") ||
            q.Contains("item") ||
            q.Contains("items") ||
            q.Contains("sku") ||
            q.Contains("barcode") ||
            q.Contains("stock") ||
            q.Contains("price") ||
            q.Contains("quantity") ||
            q.Contains("منتج") ||
            q.Contains("منتجات") ||
            q.Contains("صنف") ||
            q.Contains("أصناف") ||
            q.Contains("كمية") ||
            q.Contains("حركة");

        if (wantsProductDetails)
        {
            var productName =
                await FindProductNameInQuestionAsync(question);

            if (productName != null)
            {
                contextParts.Add(
                    "PRODUCT DATA:\n" +
                    await GetProductStockAsync(productName));
            }
        }

        if (q.Contains("supplier") ||
            q.Contains("vendor") ||
            q.Contains("مورد"))
        {
            var supplierName =
                await FindSupplierNameInQuestionAsync(question);

            if (supplierName != null)
            {
                contextParts.Add(
                    $"SUPPLIER DATA ({days} DAYS):\n" +
                    await GetSupplierActivityAsync(supplierName, days));
            }
        }

        if (q.Contains("customer") ||
            q.Contains("client") ||
            q.Contains("عميل"))
        {
            var customerName =
                await FindCustomerNameInQuestionAsync(question);

            if (customerName != null)
            {
                contextParts.Add(
                    $"CUSTOMER DATA ({days} DAYS):\n" +
                    await GetCustomerSalesAsync(customerName, days));
            }
        }

        if (contextParts.Count == 0 || wantsOverview)
        {
            contextParts.Insert(
                0,
                "INVENTORY OVERVIEW:\n" +
                await GetInventoryOverviewAsync());
        }

        return
            """
            TARGETED LIVE INVENTORY CONTEXT

            This context was selected from the user's question.
            Use only the supplied data as the source of truth.
            Never invent values.
            Keep the answer concise.
            Answer in the same language as the user.
            You are read-only.

            """ +
            string.Join("\n\n", contextParts);
    }

    private static int ExtractPeriodDays(string question)
    {
        if (question.Contains("today") ||
            question.Contains("today's") ||
            question.Contains("اليوم"))
            return 1;

        if (question.Contains("this month") ||
            question.Contains("current month") ||
            question.Contains("هذا الشهر") ||
            question.Contains("الشهر الحالي"))
            return Math.Max(1, DateTime.UtcNow.Day);

        var match =
            Regex.Match(
                question,
                @"\b(\d{1,3})\s*(day|days|week|weeks|month|months)\b",
                RegexOptions.IgnoreCase);

        if (!match.Success)
            return 30;

        var value = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value.ToLowerInvariant();

        return unit switch
        {
            "week" or "weeks" => Math.Clamp(value * 7, 1, 365),
            "month" or "months" => Math.Clamp(value * 30, 1, 365),
            _ => Math.Clamp(value, 1, 365)
        };
    }

    private async Task<string?> FindProductNameInQuestionAsync(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();

        var products =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetAllAsync();

        return products
            .Where(x =>
                (!string.IsNullOrWhiteSpace(x.Name) &&
                 normalized.Contains(x.Name.Trim().ToLowerInvariant())) ||
                (!string.IsNullOrWhiteSpace(x.Sku) &&
                 normalized.Contains(x.Sku.Trim().ToLowerInvariant())))
            .OrderByDescending(x =>
                Math.Max(
                    x.Name?.Length ?? 0,
                    x.Sku?.Length ?? 0))
            .Select(x => x.Name)
            .FirstOrDefault();
    }

    private async Task<string?> FindSupplierNameInQuestionAsync(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();

        var suppliers =
            await _unitOfWork
                .GenaricRepository<Supplier>()
                .GetAllAsync();

        return suppliers
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.SupplierName) &&
                normalized.Contains(
                    x.SupplierName.Trim().ToLowerInvariant()))
            .OrderByDescending(x => x.SupplierName.Length)
            .Select(x => x.SupplierName)
            .FirstOrDefault();
    }

    private async Task<string?> FindCustomerNameInQuestionAsync(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();

        var customers =
            await _unitOfWork
                .GenaricRepository<Customer>()
                .GetAllAsync();

        return customers
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.CustomerName) &&
                normalized.Contains(
                    x.CustomerName.Trim().ToLowerInvariant()))
            .OrderByDescending(x => x.CustomerName.Length)
            .Select(x => x.CustomerName)
            .FirstOrDefault();
    }

    private async Task<string?> FindCategoryNameInQuestionAsync(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();

        var categories =
            await _unitOfWork
                .GenaricRepository<Category>()
                .GetAllAsync();

        return categories
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Name) &&
                normalized.Contains(
                    x.Name.Trim().ToLowerInvariant()))
            .OrderByDescending(x => x.Name.Length)
            .Select(x => x.Name)
            .FirstOrDefault();
    }

    private static bool LooksLikeInventoryQuestion(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();

        string[] inventoryTerms =
        [
            // Inventory / stock
            "inventory", "inventories", "stock", "stock level", "stock levels",
            "stock quantity", "available stock", "available quantity",
            "quantity on hand", "on hand", "reorder", "reorder level",
            "reorder point", "restock", "restocking", "replenish", "replenishment",
            "low stock", "out of stock", "overstock", "shortage", "inventory value",

            // Products / SKU
            "product", "products", "item", "items", "sku", "barcode",
            "price", "unit price", "cost",

            // Stock movements / lifecycle
            "stock movement", "stock movements", "movement", "movements",
            "adjustment", "adjustments", "damage", "damaged", "lost", "loss",
            "found", "return", "returned", "opening stock", "opening balance",
            "receive", "received", "receiving", "issue", "issued",
            "transfer", "transferred", "vendor gift", "gifted stock",

            // Sales / purchases that affect stock
            "sale", "sales", "sell", "sold", "selling",
            "purchase", "purchases", "purchased", "buy", "bought",
            "units sold", "units purchased", "revenue",

            // Related entities / inventory reporting
            "supplier", "suppliers", "customer", "customers",
            "category", "categories", "warehouse", "inventory report",
            "stock report", "stock history", "movement history",

            // Arabic
            "مخزون", "المخزون", "رصيد المخزون", "كمية المخزون", "الكمية المتاحة",
            "بضاعة", "منتج", "منتجات", "صنف", "أصناف", "باركود",
            "مبيعات", "بيع", "مباع", "مشتريات", "شراء", "مشتريات",
            "مورد", "موردين", "عميل", "عملاء", "فئة", "فئات",
            "إعادة طلب", "إعادة تخزين", "ناقص", "نفد", "نفاذ المخزون",
            "تالف", "تالفه", "هالك", "فاقد", "فقد", "مرتجع", "مرتجعات",
            "تسوية", "حركة مخزون", "حركات المخزون", "إضافة للمخزون",
            "خصم من المخزون", "رصيد", "كمية", "جرد", "جرد المخزون"
        ];

        return inventoryTerms.Any(normalized.Contains);
    }

    [Description("Get a high-level overview of the current inventory: product count, total units, low-stock items, out-of-stock items, and inventory value.")]
    private async Task<string> GetInventoryOverviewAsync()
    {
        var products =
            (await _unitOfWork
                .GenaricRepository<Product>()
                .GetAllAsync())
            .ToList();

        var result = new
        {
            totalProducts = products.Count,
            totalUnits = products.Sum(x => x.StockQuantity),
            lowStockProducts = products.Count(
                x => x.StockQuantity > 0 &&
                     x.StockQuantity <= x.LowStockThreshold),
            outOfStockProducts = products.Count(
                x => x.StockQuantity <= 0),
            inventoryValue = products.Sum(
                x => x.StockQuantity * x.UnitPrice)
        };

        return JsonSerializer.Serialize(result);
    }

    [Description("Search inventory products by product name, SKU, or description.")]
    private async Task<string> SearchProductsAsync(
        [Description("Product name, SKU, or keyword to search for.")] string query,
        [Description("Maximum number of results to return.")] int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Search query is required.";

        maxResults = Math.Clamp(maxResults, 1, 20);

        var products =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetAllAsync();

        var normalized = query.Trim();

        var matches =
            products
                .Where(x =>
                    x.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    x.Sku.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    (x.Description?.Contains(
                        normalized,
                        StringComparison.OrdinalIgnoreCase) ?? false))
                .OrderBy(x => x.Name)
                .Take(maxResults)
                .Select(x => new
                {
                    x.Id,
                    x.Sku,
                    x.Name,
                    x.StockQuantity,
                    x.LowStockThreshold,
                    x.UnitPrice,
                    x.CategoryId
                })
                .ToList();

        return JsonSerializer.Serialize(matches);
    }

    [Description("Get the current stock, reorder level, price, category, and SKU for a specific product.")]
    private async Task<string> GetProductStockAsync(
        [Description("Product name or SKU.")] string product)
    {
        if (string.IsNullOrWhiteSpace(product))
            return "Product name or SKU is required.";

        var products =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetAllAsync();

        var normalized = product.Trim();

        var matches =
            products
                .Where(x =>
                    x.Name.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                    x.Sku.Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
                    x.Name.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
                    x.Sku.Contains(normalized, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .ToList();

        var categoryIds = matches.Select(x => x.CategoryId).Distinct().ToList();

        var categories =
            categoryIds.Count == 0
                ? Enumerable.Empty<Category>()
                : (await _unitOfWork
                    .GenaricRepository<Category>()
                    .GetWhereAsync(x => categoryIds.Contains(x.Id)))
                  .ToList();

        var categoryLookup =
            categories.ToDictionary(x => x.Id, x => x.Name);

        var result =
            matches.Select(x => new
            {
                x.Id,
                x.Sku,
                x.Name,
                Category = categoryLookup.TryGetValue(
                    x.CategoryId,
                    out var category)
                    ? category
                    : "Unknown",
                CurrentStock = x.StockQuantity,
                ReorderLevel = x.LowStockThreshold,
                x.UnitPrice,
                Status =
                    x.StockQuantity <= 0
                        ? "OutOfStock"
                        : x.StockQuantity <= x.LowStockThreshold
                            ? "LowStock"
                            : "InStock"
            });

        return JsonSerializer.Serialize(result);
    }

    [Description("Get all products that are currently at or below their reorder level, including out-of-stock products.")]
    private async Task<string> GetLowStockProductsAsync()
    {
        var products =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetWhereAsync(
                    x => x.StockQuantity <= x.LowStockThreshold);

        var result =
            products
                .OrderBy(x => x.StockQuantity)
                .Select(x => new
                {
                    x.Id,
                    x.Sku,
                    x.Name,
                    CurrentStock = x.StockQuantity,
                    ReorderLevel = x.LowStockThreshold,
                    MissingToReorder =
                        Math.Max(
                            0,
                            x.LowStockThreshold - x.StockQuantity),
                    x.UnitPrice
                })
                .ToList();

        return JsonSerializer.Serialize(result);
    }

    [Description("Get the best-selling products by units sold over a recent number of days.")]
    private async Task<string> GetTopSellingProductsAsync(
        [Description("Number of recent days to analyze.")] int days = 30,
        [Description("Number of products to return.")] int top = 10)
    {
        days = Math.Clamp(days, 1, 365);
        top = Math.Clamp(top, 1, 20);

        var from = DateTime.UtcNow.AddDays(-days);

        var sales =
            (await _unitOfWork
                .GenaricRepository<Sale>()
                .GetWhereAsync(x => x.SaleDate >= from))
            .ToList();

        if (sales.Count == 0)
            return "No sales found for the requested period.";

        var saleIds = sales.Select(x => x.Id).ToList();

        var items =
            (await _unitOfWork
                .GenaricRepository<SaleItem>()
                .GetWhereAsync(
                    x => saleIds.Contains(x.SaleId)))
            .ToList();

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();

        var products =
            productIds.Count == 0
                ? Enumerable.Empty<Product>()
                : (await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(x => productIds.Contains(x.Id)))
                  .ToList();

        var productLookup =
            products.ToDictionary(x => x.Id, x => x);

        var result =
            items
                .GroupBy(x => x.ProductId)
                .Select(g =>
                {
                    productLookup.TryGetValue(g.Key, out var product);

                    return new
                    {
                        ProductId = g.Key,
                        ProductName = product?.Name ?? $"Product #{g.Key}",
                        Sku = product?.Sku,
                        UnitsSold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(
                            x => x.Quantity * x.UnitPrice)
                    };
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(top)
                .ToList();

        return JsonSerializer.Serialize(new
        {
            periodDays = days,
            from,
            to = DateTime.UtcNow,
            products = result
        });
    }

    [Description("Get total sales count, revenue, and units sold over a recent number of days.")]
    private async Task<string> GetSalesSummaryAsync(
        [Description("Number of recent days to analyze.")] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);

        var from = DateTime.UtcNow.AddDays(-days);

        var sales =
            (await _unitOfWork
                .GenaricRepository<Sale>()
                .GetWhereAsync(x => x.SaleDate >= from))
            .ToList();

        var saleIds = sales.Select(x => x.Id).ToList();

        var items =
            saleIds.Count == 0
                ? Enumerable.Empty<SaleItem>()
                : (await _unitOfWork
                    .GenaricRepository<SaleItem>()
                    .GetWhereAsync(
                        x => saleIds.Contains(x.SaleId)))
                  .ToList();

        var result = new
        {
            periodDays = days,
            salesCount = sales.Count,
            revenue = sales.Sum(x => x.TotalAmount),
            unitsSold = items.Sum(x => x.Quantity),
            averageSaleValue =
                sales.Count == 0
                    ? 0
                    : sales.Average(x => x.TotalAmount)
        };

        return JsonSerializer.Serialize(result);
    }

    [Description("Get total purchases, purchase spending, and units purchased over a recent number of days.")]
    private async Task<string> GetPurchaseSummaryAsync(
        [Description("Number of recent days to analyze.")] int days = 30)
    {
        days = Math.Clamp(days, 1, 365);

        var from = DateTime.UtcNow.AddDays(-days);

        var purchases =
            (await _unitOfWork
                .GenaricRepository<Purchase>()
                .GetWhereAsync(x => x.PurchaseDate >= from))
            .ToList();

        var purchaseIds =
            purchases
                .Select(x => x.Id)
                .ToList();

        var items =
            purchaseIds.Count == 0
                ? Enumerable.Empty<PurchaseItem>()
                : (await _unitOfWork
                    .GenaricRepository<PurchaseItem>()
                    .GetWhereAsync(
                        x => purchaseIds.Contains(x.PurchaseId)))
                  .ToList();

        var result = new
        {
            periodDays = days,
            purchasesCount = purchases.Count,
            spending = purchases.Sum(x => x.TotalAmount),
            unitsPurchased = items.Sum(x => x.Quantity),
            averagePurchaseValue =
                purchases.Count == 0
                    ? 0
                    : purchases.Average(x => x.TotalAmount)
        };

        return JsonSerializer.Serialize(result);
    }

    [Description("Get recent stock movements such as Sale, Purchase, Damage, Lost, Found, Adjustment, VendorGift, returns, and opening stock.")]
    private async Task<string> GetRecentStockMovementsAsync(
        [Description("Number of recent days to inspect.")] int days = 7,
        [Description("Maximum number of movements to return.")] int maxResults = 20)
    {
        days = Math.Clamp(days, 1, 365);
        maxResults = Math.Clamp(maxResults, 1, 50);

        var from = DateTime.UtcNow.AddDays(-days);

        var movements =
            (await _unitOfWork
                .GenaricRepository<StockMovement>()
                .GetWhereAsync(x => x.MovementDate >= from))
            .OrderByDescending(x => x.MovementDate)
            .Take(maxResults)
            .ToList();

        var productIds = movements.Select(x => x.ProductId).Distinct().ToList();

        var products =
            productIds.Count == 0
                ? Enumerable.Empty<Product>()
                : (await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(x => productIds.Contains(x.Id)))
                  .ToList();

        var productLookup =
            products.ToDictionary(x => x.Id, x => x.Name);

        var result =
            movements.Select(x => new
            {
                x.Id,
                Product = productLookup.TryGetValue(
                    x.ProductId,
                    out var name)
                    ? name
                    : $"Product #{x.ProductId}",
                x.Quantity,
                Direction =
                    x.Quantity >= 0
                        ? "Increase"
                        : "Decrease",
                x.MovementType,
                x.MovementDate,
                x.ReferenceType,
                x.ReferenceId,
                x.Reason,
                x.Notes
            });

        return JsonSerializer.Serialize(result);
    }

    [Description("Get stock movement history for a specific product over a recent number of days.")]
    private async Task<string> GetProductMovementHistoryAsync(
        [Description("Product name or SKU.")] string product,
        [Description("Number of recent days to inspect.")] int days = 30)
    {
        if (string.IsNullOrWhiteSpace(product))
            return "Product name or SKU is required.";

        days = Math.Clamp(days, 1, 365);

        var products =
            await _unitOfWork
                .GenaricRepository<Product>()
                .GetAllAsync();

        var normalized = product.Trim();

        var matchedProduct =
            products
                .FirstOrDefault(x =>
                    x.Name.Equals(
                        normalized,
                        StringComparison.OrdinalIgnoreCase) ||
                    x.Sku.Equals(
                        normalized,
                        StringComparison.OrdinalIgnoreCase) ||
                    x.Name.Contains(
                        normalized,
                        StringComparison.OrdinalIgnoreCase) ||
                    x.Sku.Contains(
                        normalized,
                        StringComparison.OrdinalIgnoreCase));

        if (matchedProduct == null)
            return $"No product found matching '{product}'.";

        var from = DateTime.UtcNow.AddDays(-days);

        var movements =
            (await _unitOfWork
                .GenaricRepository<StockMovement>()
                .GetWhereAsync(
                    x => x.ProductId == matchedProduct.Id &&
                         x.MovementDate >= from))
            .OrderByDescending(x => x.MovementDate)
            .Take(100)
            .Select(x => new
            {
                x.MovementDate,
                x.MovementType,
                x.Quantity,
                x.ReferenceType,
                x.ReferenceId,
                x.Reason,
                x.Notes
            })
            .ToList();

        return JsonSerializer.Serialize(new
        {
            product = new
            {
                matchedProduct.Id,
                matchedProduct.Sku,
                matchedProduct.Name,
                matchedProduct.StockQuantity,
                matchedProduct.LowStockThreshold
            },
            periodDays = days,
            movements
        });
    }

    [Description("Get supplier purchasing activity and the products received from a supplier over a recent number of days.")]
    private async Task<string> GetSupplierActivityAsync(
        [Description("Supplier name or part of the supplier name.")] string supplierName,
        [Description("Number of recent days to analyze.")] int days = 90)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
            return "Supplier name is required.";

        days = Math.Clamp(days, 1, 365);

        var suppliers =
            await _unitOfWork
                .GenaricRepository<Supplier>()
                .GetAllAsync();

        var supplier =
            suppliers.FirstOrDefault(x =>
                x.SupplierName.Equals(
                    supplierName.Trim(),
                    StringComparison.OrdinalIgnoreCase) ||
                x.SupplierName.Contains(
                    supplierName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (supplier == null)
            return $"No supplier found matching '{supplierName}'.";

        var from = DateTime.UtcNow.AddDays(-days);

        var purchases =
            (await _unitOfWork
                .GenaricRepository<Purchase>()
                .GetWhereAsync(
                    x => x.SupplierId == supplier.Id &&
                         x.PurchaseDate >= from))
            .ToList();

        var purchaseIds = purchases.Select(x => x.Id).ToList();

        var items =
            purchaseIds.Count == 0
                ? Enumerable.Empty<PurchaseItem>()
                : (await _unitOfWork
                    .GenaricRepository<PurchaseItem>()
                    .GetWhereAsync(
                        x => purchaseIds.Contains(x.PurchaseId)))
                  .ToList();

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();

        var products =
            productIds.Count == 0
                ? Enumerable.Empty<Product>()
                : (await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(x => productIds.Contains(x.Id)))
                  .ToList();

        var productLookup =
            products.ToDictionary(x => x.Id, x => x.Name);

        var result = new
        {
            supplier = new
            {
                supplier.Id,
                supplier.SupplierName,
                supplier.ContactName,
                supplier.Phone,
                supplier.Email
            },
            periodDays = days,
            purchaseCount = purchases.Count,
            spending = purchases.Sum(x => x.TotalAmount),
            receivedUnits = items.Sum(x => x.Quantity),
            products = items
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName =
                        productLookup.TryGetValue(
                            g.Key,
                            out var name)
                            ? name
                            : $"Product #{g.Key}",
                    Quantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Quantity)
                .ToList()
        };

        return JsonSerializer.Serialize(result);
    }

    [Description("Get a customer's recent purchases, total spending, and order count.")]
    private async Task<string> GetCustomerSalesAsync(
        [Description("Customer name or part of the customer name.")] string customerName,
        [Description("Number of recent days to analyze.")] int days = 90)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            return "Customer name is required.";

        days = Math.Clamp(days, 1, 365);

        var customers =
            await _unitOfWork
                .GenaricRepository<Customer>()
                .GetAllAsync();

        var customer =
            customers.FirstOrDefault(x =>
                x.CustomerName.Equals(
                    customerName.Trim(),
                    StringComparison.OrdinalIgnoreCase) ||
                x.CustomerName.Contains(
                    customerName.Trim(),
                    StringComparison.OrdinalIgnoreCase));

        if (customer == null)
            return $"No customer found matching '{customerName}'.";

        var from = DateTime.UtcNow.AddDays(-days);

        var sales =
            (await _unitOfWork
                .GenaricRepository<Sale>()
                .GetWhereAsync(
                    x => x.CustomerId == customer.Id &&
                         x.SaleDate >= from))
            .ToList();

        return JsonSerializer.Serialize(new
        {
            customer = new
            {
                customer.Id,
                customer.CustomerName,
                customer.ContactName,
                customer.Phone,
                customer.Email
            },
            periodDays = days,
            orderCount = sales.Count,
            totalSpent = sales.Sum(x => x.TotalAmount),
            averageOrderValue =
                sales.Count == 0
                    ? 0
                    : sales.Average(x => x.TotalAmount)
        });
    }

    [Description("Get product count, total stock, and stock value for one category, or all categories when no category name is provided.")]
    private async Task<string> GetCategorySummaryAsync(
        [Description("Optional category name or part of the category name.")] string? categoryName = null)
    {
        var categories =
            await _unitOfWork
                .GenaricRepository<Category>()
                .GetAllAsync();

        IEnumerable<Category> selectedCategories = categories;

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            selectedCategories =
                categories.Where(x =>
                    x.Name.Equals(
                        categoryName.Trim(),
                        StringComparison.OrdinalIgnoreCase) ||
                    x.Name.Contains(
                        categoryName.Trim(),
                        StringComparison.OrdinalIgnoreCase));
        }

        var selectedIds =
            selectedCategories.Select(x => x.Id).ToList();

        var products =
            selectedIds.Count == 0
                ? Enumerable.Empty<Product>()
                : (await _unitOfWork
                    .GenaricRepository<Product>()
                    .GetWhereAsync(
                        x => selectedIds.Contains(x.CategoryId)))
                  .ToList();

        var result =
            selectedCategories
                .Select(c =>
                {
                    var categoryProducts =
                        products
                            .Where(x => x.CategoryId == c.Id)
                            .ToList();

                    return new
                    {
                        c.Id,
                        c.Name,
                        ProductCount = categoryProducts.Count,
                        TotalUnits = categoryProducts.Sum(x => x.StockQuantity),
                        InventoryValue =
                            categoryProducts.Sum(
                                x => x.StockQuantity * x.UnitPrice),
                        LowStockProducts =
                            categoryProducts.Count(
                                x =>
                                    x.StockQuantity <=
                                    x.LowStockThreshold)
                    };
                })
                .OrderBy(x => x.Name)
                .ToList();

        return JsonSerializer.Serialize(result);
    }
}