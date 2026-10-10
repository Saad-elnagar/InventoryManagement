# Inventory AI Assistant

The Inventory AI Assistant is a **read-only conversational assistant** inside the ASP.NET Core MVC application.

The current active provider is **Google Gemini** through the Gemini REST API.

## Runtime

- Provider: Google Gemini
- Primary model: `gemini-3.1-flash-lite`
- Fallback model: `gemini-3.5-flash-lite`
- Backup model: `gemini-3.8-flash`
- Endpoint: `https://generativelanguage.googleapis.com/v1beta/`

The application sends the Gemini API key through the `x-goog-api-key` request header.

## Configuration

From the MVC project:

```bash
cd InventoryManagementSystem
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY"
```

The active model configuration is in `appsettings.json`:

```json
"Gemini": {
  "Model": "gemini-3.1-flash-lite",
  "FallbackModel": "gemini-3.5-flash-lite",
  "BackupModel": "gemini-3.8-flash"
}
```

You can also use an environment variable:

```bash
export GEMINI_API_KEY="YOUR_GEMINI_KEY"
```

Do not commit API keys.

## Architecture

The request flow is:

```text
InventoryAiController
        ↓
InventoryAiAssistant
        ↓
Intent detection
        ↓
Targeted live database context
        ↓
Gemini REST API
        ↓
Natural-language answer
```

The assistant keeps the model focused by loading only the database data relevant to the current question.

## Supported Questions

The assistant can work with:

- Inventory overview
- Product search
- Product stock
- Low-stock products
- Top-selling products
- Sales summaries
- Purchase summaries
- Recent stock movements
- Product movement history
- Supplier activity
- Customer sales
- Category summaries

Examples:

```text
Which products are low in stock?
What is the current stock for SKU-1001?
What were our top-selling products in the last 30 days?
How much did we spend on purchases this month?
Show recent stock movements.
How much did customer Ahmed spend in the last 90 days?
Which supplier supplied the most products?
```

Arabic and English natural-language questions are supported.

## Read-Only Safety

The AI assistant cannot create, update, or delete inventory records.

Its prompts explicitly instruct it to:

- use supplied live data as the source of truth
- never invent inventory values
- stay read-only
- answer in the user's language

Low-stock queries also have a direct database fallback, so the application can still return real low-stock data when Gemini is temporarily unavailable.

## Error Handling

Temporary Gemini failures such as:

- HTTP 429
- HTTP 500
- HTTP 502
- HTTP 503
- HTTP 504

are retried and can fall back to another configured model.

Normal chat requests also have a bounded request timeout so a model outage does not leave the MVC request waiting indefinitely.

## Related Files

```text
InventoryManagementSystem/Controllers/InventoryAiController.cs
InventoryManagementSystem.BLL/Interfaces/IInventoryAiAssistant.cs
InventoryManagementSystem.BLL/Service/InventoryAiAssistant.cs
InventoryManagementSystem.BLL/Service/GeminiReportSummarizer.cs
InventoryManagementSystem/Views/InventoryAi/Index.cshtml
InventoryManagementSystem/wwwroot/js/inventory-ai.js
```

## Reporting Integration

Gemini is also used to summarize the daily inventory report.

The reporting pipeline combines:

- live report data
- low-stock information
- stock movement analysis
- ML.NET forecast output

and can present the result in the report UI or send it by email.

## Provider Status

The current startup configuration registers `GeminiReportSummarizer` as the active `IAiReportSummarizer` implementation and configures the chat assistant to call Gemini. ML.NET is registered separately for forecasting/report analysis.

`OllamaReportSummarizer.cs` is still present in the source tree, but it is not the active `IAiReportSummarizer` registration in `Program.cs`. Do not assume Ollama is used at runtime unless the dependency-injection registration is changed.

If the provider is changed later, update this guide and verify the service registrations in `InventoryManagementSystem/Program.cs`.
