# Inventory AI Assistant

The Inventory AI Assistant is a local, read-only chat assistant inside the MVC application.

## Runtime

- Local model runtime: Ollama
- Default model: `qwen3:4b`
- Default endpoint: `http://localhost:11434`
- .NET integration: OllamaSharp + Microsoft.Extensions.AI

Qwen3 4B is available from the Ollama model library and supports tools/function calling. Microsoft also documents Ollama + `IChatClient` + function invocation for local .NET AI applications.

## Install Ollama

On Linux, install Ollama using the official Ollama installer, then start the service.

Pull the model:

```bash
ollama pull qwen3:4b
```

Verify:

```bash
ollama run qwen3:4b
```

The application expects Ollama at `http://localhost:11434` by default.

## Configuration

The application uses:

```json
"Ollama": {
  "Endpoint": "http://localhost:11434",
  "Model": "qwen3:4b"
}
```

Change these values when another local model or endpoint is preferred.

## What the assistant can query

The assistant currently has read-only tools for:

- inventory overview
- product search
- product stock
- low-stock products
- top-selling products
- sales summary
- purchase summary
- recent stock movements
- product movement history
- supplier activity
- customer sales
- category summary

The model chooses the appropriate tool from the user's natural-language question.

## Example questions

```
Which products are low in stock?
How much stock do we have for SKU-1001?
What were our top selling products in the last 30 days?
How much did we spend on purchases this month?
Show me the recent stock movements.
How much did customer Ahmed spend in the last 90 days?
Which supplier supplied the most products?
```

Arabic is supported too.

## Security

The assistant is read-only. No AI tool can create, update, or delete inventory records.

Access to the chat page is currently limited to Admin and Manager roles.

## ML.NET

The existing ML.NET forecasting implementation remains separate and continues to power the Daily Inventory Report. The chat assistant is the conversational layer that can query live application data.
