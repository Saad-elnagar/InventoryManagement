using System.ComponentModel;
using System.Text.Json;
using InventoryManagementSystem.BLL.DTOs;
using InventoryManagementSystem.BLL.Interfaces;
using InventoryManagementSystem.DAL.Entities;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace InventoryManagementSystem.BLL.Service;

public class InventoryAiAssistant : IInventoryAiAssistant
{
    private const int MaxHistoryMessages = 20;

    private readonly IChatClient _chatClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryAiAssistant> _logger;

    public InventoryAiAssistant(
        IChatClient chatClient,
        IUnitOfWork unitOfWork,
        ILogger<InventoryAiAssistant> logger)
    {
        _chatClient = chatClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<string> ChatAsync(
        IReadOnlyCollection<InventoryAiMessageDTO> messages,
        CancellationToken cancellationToken = default)
    {
        var chatMessages = new List<ChatMessage>
        {
            new(
                ChatRole.System,
                """
                You are a helpful conversational assistant for an Inventory Management System.

                You can have normal conversations too. Answer greetings, casual questions, explanations,
                small talk, and general knowledge questions naturally.

                You also have access to live inventory data through tools. Use the tools when the user asks about:
                products, stock, low stock, out of stock, sales, purchases, stock movements, suppliers,
                customers, categories, or inventory statistics.

                Never invent database values. If a required inventory fact is not available from the tools, say so clearly.
                Prefer exact numbers, dates, product names, and short calculations based on tool results.
                Do not describe tool calls to the user; just answer naturally.

                The user may speak Arabic or English.
                Answer in the same language as the latest user message.
                For Arabic, use clear Egyptian Arabic with practical business wording.

                You are read-only. Never claim that you changed, deleted, created, or updated inventory data.
                """
            )
        };

        foreach (var message in messages
                     .Where(x =>
                         !string.IsNullOrWhiteSpace(x.Content) &&
                         (x.Role == "user" || x.Role == "assistant"))
                     .TakeLast(MaxHistoryMessages))
        {
            chatMessages.Add(
                new ChatMessage(
                    message.Role == "assistant"
                        ? ChatRole.Assistant
                        : ChatRole.User,
                    message.Content.Trim()));
        }

        if (chatMessages.Count == 1)
            return "Ask me about your inventory, stock, sales, purchases, products, suppliers, customers, or reports.";

        var latestUserMessage = messages
            .LastOrDefault(x =>
                x.Role == "user" &&
                !string.IsNullOrWhiteSpace(x.Content))
            ?.Content
            ?.Trim();

        if (string.IsNullOrWhiteSpace(latestUserMessage))
            return "Ask me anything.";

        if (!LooksLikeInventoryQuestion(latestUserMessage))
        {
            try
            {
                var normalResponse =
                    await _chatClient.GetResponseAsync(
                        chatMessages,
                        new ChatOptions
                        {
                            Temperature = 0.7f,
                            MaxOutputTokens = 700
                        },
                        cancellationToken);

                var normalText = normalResponse.Text?.Trim();

                return string.IsNullOrWhiteSpace(normalText)
                    ? "I'm here. What would you like to talk about?"
                    : normalText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inventory AI normal chat request failed.");

                throw new InvalidOperationException(
                    "Inventory AI is unavailable. Make sure Ollama is running and the configured model is installed.",
                    ex);
            }
        }

        var deterministicAnswer =
            await TryHandleDeterministicQueryAsync(
                latestUserMessage,
                cancellationToken);

        if (deterministicAnswer != null)
            return deterministicAnswer;

        var options = new ChatOptions
        {
            Temperature = 0.1f,
            MaxOutputTokens = 700,
            Tools =
            [
                AIFunctionFactory.Create(GetInventoryOverviewAsync),
                AIFunctionFactory.Create(SearchProductsAsync),
                AIFunctionFactory.Create(GetProductStockAsync),
                AIFunctionFactory.Create(GetLowStockProductsAsync),
                AIFunctionFactory.Create(GetTopSellingProductsAsync),
                AIFunctionFactory.Create(GetSalesSummaryAsync),
                AIFunctionFactory.Create(GetPurchaseSummaryAsync),
                AIFunctionFactory.Create(GetRecentStockMovementsAsync),
                AIFunctionFactory.Create(GetProductMovementHistoryAsync),
                AIFunctionFactory.Create(GetSupplierActivityAsync),
                AIFunctionFactory.Create(GetCustomerSalesAsync),
                AIFunctionFactory.Create(GetCategorySummaryAsync)
            ]
        };

        try
        {
            var response =
                await _chatClient.GetResponseAsync(
                    chatMessages,
                    options,
                    cancellationToken);

            var text = response.Text?.Trim();

            return string.IsNullOrWhiteSpace(text)
                ? "I couldn't generate an answer from the available inventory data."
                : text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory AI request failed.");

            throw new InvalidOperationException(
                "Inventory AI is unavailable. Make sure Ollama is running and the configured model is installed.",
                ex);
        }
    }

    private static bool LooksLikeInventoryQuestion(string question)
    {
        var normalized = question.Trim().ToLowerInvariant();

        string[] inventoryTerms =
        [
            "inventory", "stock", "product", "products", "sku",
            "sale", "sales", "sell", "selling", "purchase", "purchases",
            "supplier", "suppliers", "customer", "customers", "category",
            "categories", "reorder", "low stock", "out of stock",
            "movement", "movements", "warehouse", "revenue", "profit",
            "units sold", "units purchased", "inventory value",
            "مخزون", "منتج", "منتجات", "مبيعات", "بيع", "مشتريات",
            "مورد", "عميل", "عملاء", "فئة", "رصيد المخزون"
        ];

        return inventoryTerms.Any(normalized.Contains);
    }

    private async Task<string?> TryHandleDeterministicQueryAsync(
        string question,
        CancellationToken cancellationToken)
    {
        var normalized = question.Trim().ToLowerInvariant();

        var isLowStockQuery =
            (normalized.Contains("low stock") ||
             normalized.Contains("low-stock") ||
             normalized.Contains("low in stock") ||
             normalized.Contains("below reorder") ||
             normalized.Contains("need reorder") ||
             normalized.Contains("needs reorder") ||
             normalized.Contains("reorder level")) &&
            (normalized.Contains("product") ||
             normalized.Contains("stock") ||
             normalized.Contains("inventory") ||
             normalized.Contains("item"));

        if (isLowStockQuery)
        {
            var json = await GetLowStockProductsAsync();

            if (string.IsNullOrWhiteSpace(json) || json == "[]")
                return "No products are currently low in stock.";

            var products =
                JsonSerializer.Deserialize<List<LowStockResult>>(json) ?? [];

            if (products.Count == 0)
                return "No products are currently low in stock.";

            var lines = products.Select((p, index) =>
                $"{index + 1}. {p.Name} ({p.Sku}) — stock: {p.CurrentStock}, reorder level: {p.ReorderLevel}, short by: {p.MissingToReorder}");

            return "These products are currently low in stock:\n\n" +
                   string.Join("\n", lines);
        }

        var isTopSellingThisMonth =
            (normalized.Contains("top selling") ||
             normalized.Contains("top-selling") ||
             normalized.Contains("best selling") ||
             normalized.Contains("best-selling")) &&
            normalized.Contains("month");

        if (isTopSellingThisMonth)
            return await GetCurrentMonthTopSellingAnswerAsync();

        return null;
    }

    private sealed class LowStockResult
    {
        public string? Sku { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderLevel { get; set; }
        public int MissingToReorder { get; set; }
    }

    private async Task<string> GetCurrentMonthTopSellingAnswerAsync()
    {
        var now = DateTime.Now;
        var from = new DateTime(now.Year, now.Month, 1);
        var to = from.AddMonths(1);

        var sales =
            (await _unitOfWork
                .GenaricRepository<Sale>()
                .GetWhereAsync(x => x.SaleDate >= from && x.SaleDate < to))
            .ToList();

        if (sales.Count == 0)
            return "No sales found for the current month.";

        var saleIds = sales.Select(x => x.Id).ToList();

        var items =
            (await _unitOfWork
                .GenaricRepository<SaleItem>()
                .GetWhereAsync(x => saleIds.Contains(x.SaleId)))
            .ToList();

        if (items.Count == 0)
            return "No sold products found for the current month.";

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();

        var products =
            (await _unitOfWork
                .GenaricRepository<Product>()
                .GetWhereAsync(x => productIds.Contains(x.Id)))
            .ToList();

        var lookup = products.ToDictionary(x => x.Id, x => x);

        var topProducts =
            items
                .GroupBy(x => x.ProductId)
                .Select(g =>
                {
                    lookup.TryGetValue(g.Key, out var product);

                    return new
                    {
                        Name = product?.Name ?? $"Product #{g.Key}",
                        Sku = product?.Sku ?? "N/A",
                        UnitsSold = g.Sum(x => x.Quantity),
                        Revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                    };
                })
                .OrderByDescending(x => x.UnitsSold)
                .ThenByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

        if (topProducts.Count == 0)
            return "No sold products found for the current month.";

        var lines = topProducts.Select((p, index) =>
            $"{index + 1}. {p.Name} ({p.Sku}) — {p.UnitsSold} units sold, revenue: {p.Revenue:N2}");

        return $"Top selling products for {from:MMMM yyyy}:\n\n" +
               string.Join("\n", lines);
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
