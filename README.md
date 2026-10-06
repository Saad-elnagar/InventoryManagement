# Inventory Management System

A full-featured **Inventory Management System** built with **ASP.NET Core MVC, .NET 10, Entity Framework Core, SQL Server, ASP.NET Core Identity, JWT authentication, ML.NET, and Gemini AI**.

The application is designed around a service-oriented business layer, transactional stock operations, role-based access control, reporting, and a read-only AI assistant that can answer natural-language questions using live inventory data.

---

## 1. Project Overview

This system manages the complete inventory lifecycle:

- Products and categories
- Suppliers and supplier-product relationships
- Customers
- Purchases and purchase items
- Sales and sale items
- Stock movements and inventory adjustments
- Low-stock monitoring
- Dashboard analytics
- Daily inventory reports
- ML.NET forecasting
- Gemini-powered AI analysis and conversational inventory queries
- Email delivery for daily reports
- Role-based employee access

The key business rule is simple:

> **Stock changes must be traceable and consistent.**

Purchases increase stock, sales decrease stock, and manual inventory events such as damage, lost items, found items, returns, adjustments, opening stock, and vendor gifts are represented as stock movements.

---

## 2. Main Features

### Inventory Management

- Create, edit, view, search, paginate, and delete products
- SKU validation and duplicate-SKU protection
- Category management
- Configurable reorder level / low-stock threshold
- Dedicated low-stock view
- Inventory value calculation
- Product history through stock movements
- Protection against deleting products with sales or purchase history

### Purchasing

- Create purchases against suppliers
- Add multiple purchase items
- Validate supplier and product references
- Increase product stock when a purchase is completed
- Create linked stock-movement records
- Keep the header, items, stock update, and movement history inside one database transaction

### Sales

- Create sales against customers
- Add multiple sale items
- Validate customer and product references
- Prevent selling more than available stock
- Decrease product stock automatically
- Create linked stock-movement records
- Roll back the complete operation when validation or persistence fails

### Stock Movements

Supported movement types:

- Purchase
- Sale
- Damage
- Lost
- Found
- Adjustment
- Purchase Return
- Sale Return
- Opening Stock
- Vendor Gift

Each movement can contain:

- Product
- Quantity delta
- Movement type
- Movement date
- Reference type / reference ID
- Reason
- Notes

### Dashboard

The dashboard provides live KPIs and analytics including:

- Total products
- Total categories
- Total suppliers
- Total customers
- Total stock units
- Low-stock count
- Out-of-stock count
- Inventory value
- Total purchases
- Total sales
- Category statistics
- Monthly inbound/outbound movement data
- Lowest-stock products

### Reporting

The daily report is built from the last 24 hours and includes:

- Total products
- Stock movement count
- Movement breakdown by type
- Low-stock products
- AI-generated management summary
- ML.NET forecast insights

The report can be previewed in the UI and sent by email.

### AI Assistant

The Inventory AI page is a **read-only conversational assistant** available to Admin and Manager users.

It can answer questions about:

- Current inventory
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

### AI Safety / Read-Only Design

The chat assistant does **not** create, update, or delete inventory records.

The application supplies targeted live database context to the model, and the prompt explicitly instructs the model to:

- use supplied data as the source of truth
- never invent inventory values
- remain read-only
- answer in the user's language

Low-stock questions also have a direct database fallback so the application can return real stock data even when Gemini is temporarily unavailable.

---

## 3. Architecture

The solution is split into clear layers:

```text
┌───────────────────────────────────────────────┐
│               Inventory.Web                  │
│ ASP.NET Core MVC / Controllers / Views / UI  │
└──────────────────────┬────────────────────────┘
                       │
                       ▼
┌───────────────────────────────────────────────┐
│         InventoryManagementSystem.BLL        │
│ DTOs / Services / Interfaces / Business Logic│
│ Pagination / AI / Reporting / Forecasting    │
└──────────────────────┬────────────────────────┘
                       │
                       ▼
┌───────────────────────────────────────────────┐
│         InventoryManagementSystem.DAL        │
│ EF Core / Entities / DbContexts / Repository │
│ Unit of Work / SQL Server                    │
└──────────────────────┬────────────────────────┘
                       │
                       ▼
                 SQL Server
```

### Layer responsibilities

**Presentation — `InventoryManagementSystem`**

- MVC controllers
- Razor views
- Authentication flow
- Authorization
- UI
- JavaScript for AI chat and report rendering

**Business Logic — `InventoryManagementSystem.BLL`**

- DTOs
- Service interfaces
- Business validation
- Transactions orchestration
- Pagination
- Dashboard calculations
- Reports
- AI assistant
- AI report summarization
- Background report scheduling

**Data Access — `InventoryManagementSystem.DAL`**

- EF Core entities
- `ApplicationDbContext`
- `AuthDbContext`
- Generic repository
- Unit of Work
- Database transaction handling

**Tests — `InventoryManagementSystem.Tests`**

- xUnit
- Moq
- Service-level business logic tests

---

## 4. Core Transaction Flows

### Purchase flow

```text
Create Purchase
      │
      ├── Validate Supplier
      ├── Validate Items
      ├── Begin DB Transaction
      │
      ├── Save Purchase Header
      ├── Create Purchase Items
      ├── Increase Product Stock
      ├── Create StockMovement records
      ├── Save Changes
      │
      └── Commit
```

If anything fails:

```text
Rollback
   ↓
No partial purchase
No partial stock update
No partial stock movement history
```

### Sale flow

```text
Create Sale
    │
    ├── Validate Customer
    ├── Validate Items
    ├── Check Available Stock
    ├── Begin DB Transaction
    │
    ├── Save Sale Header
    ├── Create Sale Items
    ├── Decrease Product Stock
    ├── Create StockMovement records
    ├── Save Changes
    │
    └── Commit
```

This makes sales and purchases auditable and prevents the stock table from becoming disconnected from its movement history.

---

## 5. Database Model

The main inventory entities are:

```text
Category
   │
   └──< Product >──< SupplierProduct >── Supplier
          │
          ├──< PurchaseItem >── Purchase ── Supplier
          ├──< SaleItem >───── Sale ────── Customer
          └──< StockMovement
```

### Main tables

| Table | Purpose |
|---|---|
| Categories | Product classification |
| Products | Product master data and current stock |
| Suppliers | Supplier master data |
| SupplierProducts | Supplier ↔ product relationships and contract pricing |
| Customers | Customer master data |
| Purchases | Purchase headers |
| PurchaseItems | Products received through purchases |
| Sales | Sale headers |
| SaleItems | Products sold through sales |
| StockMovements | Complete stock audit trail |
| ASP.NET Identity tables | Users, roles, claims, authentication data |

The application uses **SQL Server + EF Core** and follows a database-first style for the inventory entities.

---

## 6. Authentication and Authorization

Authentication uses:

- ASP.NET Core Identity
- JWT bearer authentication
- JWT stored/read from the `access_token` cookie for MVC requests
- Role-based authorization

The application seeds these roles:

- `Admin`
- `Manager`
- `Employee`

Examples of current authorization boundaries:

| Area | Access |
|---|---|
| Dashboard | Authenticated users |
| Sales | Admin / Manager / Employee |
| Customers | Authenticated; management actions restricted |
| Purchases | Admin / Manager |
| Suppliers | Admin / Manager |
| Stock movement management | Admin / Manager |
| Inventory AI | Admin / Manager |
| Reports | Admin only |
| User management | Admin only |
| Product deletion | Admin only |

---

## 7. AI Architecture

The current active AI provider is **Google Gemini** through the Gemini REST API.

### Model chain

```text
gemini-3.1-flash-lite
        ↓
gemini-3.5-flash-lite
        ↓
gemini-3.8-flash
```

The application retries temporary failures and falls back between configured models.

### AI request flow

```text
User question
      │
      ▼
InventoryAiController
      │
      ▼
InventoryAiAssistant
      │
      ├── Detect normal vs inventory question
      ├── Build targeted live DB context
      ├── Preserve recent conversation context
      └── Send read-only prompt to Gemini
                  │
                  ▼
              Gemini API
                  │
                  ▼
            Natural-language answer
```

### Live inventory context

The assistant does not dump the entire database into every request.

Instead, it detects the intent and loads only the relevant data, for example:

```text
"Which products are low in stock?"
        ↓
LOW STOCK DATA

"What did we sell in the last 30 days?"
        ↓
SALES SUMMARY (30 DAYS)

"Show recent stock movements."
        ↓
RECENT MOVEMENTS
```

This keeps prompts focused and reduces unnecessary database and model work.

---

## 8. Reporting and Forecasting

The daily reporting pipeline is:

```text
ReportService
     │
     ├── Last 24h stock movements
     ├── Current products
     └── Low-stock analysis
              │
              ▼
      AI / Forecast layer
              │
              ▼
      Daily Inventory Report
              │
        ┌─────┴─────┐
        ▼           ▼
     Preview     Email
```

The application also contains ML.NET-based forecasting/report analysis infrastructure. Gemini is used as the natural-language reporting layer, while the report itself is still based on application data and forecasting output.

---

## 9. Background Reporting

A hosted background service schedules the daily report using:

```json
"Report": {
  "SendHour": 23,
  "SendMinute": 0,
  "RecipientEmail": ""
}
```

At the configured time the service:

1. Builds the daily report.
2. Generates the AI summary.
3. Sends the report through SMTP.

If the recipient email is not configured, the scheduler logs the configuration problem instead of silently pretending the report was sent.

---

## 10. Pagination and Search

Pagination is centralized in:

```text
InventoryManagementSystem.BLL/Pagination
```

Components include:

- `PaginationParams`
- `PaginationResult<T>`
- `PaginationHelper`

This avoids duplicating pagination calculations across services.

Product search uses SQL Server-aware filtering for:

- Name
- SKU
- Description

---

## 11. Repository and Unit of Work

The data layer uses:

- Generic repository
- Unit of Work
- Cached repository instances per entity type
- EF Core `AsNoTracking()` for read queries where appropriate
- Explicit database transactions for stock-changing workflows

The Unit of Work is responsible for:

- Accessing repositories
- Saving changes
- Beginning transactions
- Committing transactions
- Rolling transactions back

This keeps transaction ownership in the business workflow rather than spreading transaction logic across controllers.

---

## 12. Project Structure

```text
InventoryManagement/
│
├── InventoryManagement.sln
│
├── InventoryManagementSystem/              # ASP.NET Core MVC
│   ├── Controllers/
│   ├── Authentication/
│   ├── Models/
│   ├── ViewModel/
│   ├── Views/
│   ├── wwwroot/
│   ├── Program.cs
│   └── appsettings.json
│
├── InventoryManagementSystem.BLL/          # Business layer
│   ├── DTOs/
│   ├── Enums/
│   ├── Interfaces/
│   ├── Pagination/
│   └── Service/
│
├── InventoryManagementSystem.DAL/          # Data access layer
│   ├── DataBase/
│   ├── Entities/
│   ├── Migrations/
│   ├── Repository/
│   └── UnitOfWork/
│
├── InventoryManagementSystem.Tests/        # Unit tests
│   ├── TestDoubles/
│   └── *ServiceTests.cs
│
├── .github/workflows/tests.yml              # CI
├── compose.yaml
├── Dockerfile(s)
└── INVENTORY_AI_SETUP.md
```

---

## 13. Technology Stack

### Backend

- .NET 10
- ASP.NET Core MVC
- C#
- Entity Framework Core 10
- SQL Server

### Authentication

- ASP.NET Core Identity
- JWT Bearer Authentication
- Role-based Authorization

### AI / ML

- Google Gemini REST API
- ML.NET
- ML.NET TimeSeries
- Legacy Ollama integration code from the earlier local-model prototype remains in the repository, but Gemini is the active provider.

### Testing

- xUnit
- Moq
- Microsoft.NET.Test.Sdk
- Coverlet Collector

### Frontend

- Razor Views
- Bootstrap
- JavaScript
- jQuery validation
- Bootstrap Icons

### Infrastructure

- Docker support
- GitHub Actions CI
- SMTP email support
- .NET Hosted Background Service

---

## 14. Requirements

Before running the project, install/configure:

- .NET SDK 10
- SQL Server
- Git
- Optional: Docker
- Optional: SMTP account for email reports
- Gemini API key for AI features

Check .NET:

```bash
dotnet --version
```

---

## 15. Configuration

Do **not** commit secrets to `appsettings.json`.

Use .NET User Secrets for local development.

From the MVC project:

```bash
cd InventoryManagementSystem
```

### Required JWT configuration

```bash
dotnet user-secrets set "Jwt:Key" "YOUR_LONG_RANDOM_JWT_KEY"
```

The issuer and audience are already defined in `appsettings.json`.

### Gemini API key

```bash
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_KEY"
```

The application also accepts:

```bash
export GEMINI_API_KEY="YOUR_GEMINI_KEY"
```

### Email configuration

For daily report emails, configure secrets or environment variables for:

```text
Smtp:Host
Smtp:Port
Smtp:Username
Smtp:Password
Smtp:FromAddress
Report:RecipientEmail
```

---

## 16. Database Configuration

The default application connection string points to SQL Server on:

```text
localhost:1433
```

Example shape:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=InventoryManagementDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;"
}
```

For a new environment, update the connection string to match the local SQL Server instance.

The solution contains EF Core database/auth migration infrastructure under:

```text
InventoryManagementSystem.DAL/Migrations
```

---

## 17. Run Locally

Clone the repository:

```bash
git clone https://github.com/Saad-elnagar/InventoryManagement.git
cd InventoryManagement
```

Restore:

```bash
dotnet restore InventoryManagement.sln
```

Build:

```bash
dotnet build InventoryManagement.sln
```

Run the MVC application:

```bash
cd InventoryManagementSystem
dotnet run
```

Then open the URL printed by ASP.NET Core.

---

## 18. Running Tests

Run all tests:

```bash
dotnet test InventoryManagementSystem.Tests/InventoryManagementSystem.Tests.csproj
```

The test suite covers business logic such as:

- Category creation/deletion validation
- Customer operations
- Product operations
- Supplier validation
- Purchase transactions
- Sale transactions
- Stock movement behavior
- Daily report calculations

Important transaction cases are tested, including:

- insufficient stock
- invalid customer
- invalid supplier
- empty purchase items
- stock increase/decrease
- rollback behavior
- damage/lost/found adjustments

---

## 19. CI / GitHub Actions

The repository runs automated CI on pushes and pull requests.

Workflow:

```text
Checkout
   ↓
Setup .NET 10
   ↓
dotnet restore
   ↓
dotnet build --configuration Release
   ↓
dotnet test --configuration Release
```

Workflow file:

```text
.github/workflows/tests.yml
```

---

## 20. Database Backup

For team development or demo handoff, SQL Server can be backed up as a `.bak` file.

Example backup flow inside a SQL Server Docker container:

```bash
docker exec -it sqlserver mkdir -p /var/opt/mssql/backup
```

Then run a SQL Server `BACKUP DATABASE` command using the credentials configured for the local SQL Server instance.

Copy the backup to the host:

```bash
docker cp sqlserver:/var/opt/mssql/backup/InventoryManagementDB.bak ~/Downloads/InventoryManagementDB.bak
```

The resulting `.bak` can be restored into another SQL Server instance.

> Never commit database backups or credentials to Git.

---

## 21. Security Notes

The repository intentionally keeps secrets outside source control.

Never commit:

- Gemini API keys
- JWT signing keys
- SMTP passwords
- SQL Server passwords
- Production connection strings
- Personal credentials

Use:

- .NET User Secrets for local development
- Environment variables for deployment
- Secret storage provided by the hosting platform in production

---

## 22. Design Principles

The project is built around several practical backend principles:

### Separation of concerns

Controllers handle HTTP concerns; services handle business rules; repositories handle persistence.

### DTO-based boundaries

MVC controllers exchange DTOs instead of directly binding database entities for business operations.

### Transactional consistency

A stock-changing operation should update:

```text
Header
+ Items
+ Stock
+ StockMovement
```

as one logical unit.

### Centralized pagination

Pagination logic is reusable instead of duplicated.

### Auditability

Stock changes are represented as explicit movement events so managers can understand how current stock was reached.

### Read-only AI

AI can inspect inventory data but cannot mutate it.

---

## 23. Important Business Rules

Some critical rules enforced by the system:

- SKU must be unique.
- Product category must exist.
- Purchase supplier must exist.
- Purchase must contain at least one valid item.
- Sale customer must exist.
- Sale must contain valid items.
- Sale quantity cannot exceed available stock.
- Damage/lost movements cannot reduce stock below what is available.
- Manual movement types are validated.
- Products with sales or purchase history cannot simply be deleted.
- Stock-changing workflows use explicit transactions.
- AI does not write to the inventory database.

---

## 24. What to Read First

For someone new to the project, the recommended reading order is:

1. `InventoryManagementSystem/Program.cs`
2. `InventoryManagementSystem/Controllers/`
3. `InventoryManagementSystem.BLL/Service/`
4. `InventoryManagementSystem.DAL/UnitOfWork/UnitOfWork.cs`
5. `InventoryManagementSystem.DAL/Repository/`
6. `InventoryManagementSystem.DAL/DataBase/ApplicationDbContext.cs`
7. `InventoryManagementSystem.Tests/`

For the AI subsystem:

1. `InventoryManagementSystem/Controllers/InventoryAiController.cs`
2. `InventoryManagementSystem.BLL/Service/InventoryAiAssistant.cs`
3. `InventoryManagementSystem.BLL/Service/GeminiReportSummarizer.cs`
4. `InventoryManagementSystem/wwwroot/js/inventory-ai.js`

---

## 25. Current Development Status

The repository is actively developed on the `main` branch.

The latest CI run successfully completed:

- restore
- build

The latest test run executed **27 tests**, with **25 passing and 2 currently failing** in `ProductServiceTests`. The failures are test/service expectation issues around product creation validation, not compilation failures.

This section is intentionally kept transparent so contributors know the current CI state instead of assuming the repository is fully green.

---

## 26. Future Extension Ideas

The architecture leaves room for:

- multi-warehouse support
- barcode scanning
- purchase/sale returns UI expansion
- richer supplier-product pricing history
- scheduled restocking recommendations
- more advanced ML.NET forecasting
- AI-powered anomaly detection
- notification channels beyond email
- audit log / user activity history
- production-grade secret management
- stronger integration and end-to-end test coverage

---

## 27. Project Goal

The goal is not only to store products and quantities.

The system is intended to provide a complete operational view of inventory:

```text
                    ┌─────────────┐
                    │  Products   │
                    └──────┬──────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
    Purchases            Sales         Stock Adjustments
        │                  │                  │
        └──────────────────┼──────────────────┘
                           ▼
                  Stock Movement History
                           │
             ┌─────────────┼─────────────┐
             ▼             ▼             ▼
          Dashboard      Reports          AI
             │             │             │
             └─────────────┼─────────────┘
                           ▼
                    Business Insight
```

The result is a system where the current stock number is not an isolated value: it is connected to the transactions and events that produced it, while reporting and AI turn that operational data into information a manager can actually use.
