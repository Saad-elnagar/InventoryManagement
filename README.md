# Inventory Management System

A web-based inventory management application built with **C#**, **ASP.NET Core MVC**, **Entity Framework Core**, and **SQL Server**. The project helps businesses manage inventory operations in one place, from product and supplier records to purchases, sales, stock movements, and reporting.

> **Project focus:** backend development, layered architecture, relational data modeling, and practical inventory workflows.

## Overview

Inventory data is connected to day-to-day business operations. This application brings core inventory workflows together so users can maintain catalog data, record purchases and sales, monitor stock levels, and review operational reports.

## Key Features

- **Product and category management** — organize inventory items and their categories.
- **Supplier and customer management** — maintain business partner records.
- **Purchasing and sales workflows** — record purchases and sales with their associated line items.
- **Stock tracking** — track inventory quantities and record stock movements.
- **Low-stock monitoring** — identify products that need replenishment.
- **Search and pagination** — navigate product data more efficiently.
- **Reports and dashboard** — review inventory and business activity.
- **AI-assisted inventory queries** — a read-only chat assistant can answer natural-language questions about inventory data when configured with a local Ollama model.
- **Role-based access** — restrict selected management features by user role.

## Technology Stack

| Area | Technologies |
| --- | --- |
| Language and framework | C#, ASP.NET Core MVC |
| Data access | Entity Framework Core, LINQ |
| Database | Microsoft SQL Server |
| Architecture | Layered architecture (Web, Business Logic, Data Access) |
| Data access patterns | Repository, Unit of Work |
| UI | Razor Views, HTML, CSS, JavaScript |
| Containerization | Docker |
| AI assistant | Ollama, OllamaSharp, Microsoft.Extensions.AI |
| Testing project | InventoryManagementSystem.Tests |

## Architecture

The solution separates presentation, business logic, and data access to keep responsibilities organized:

```text
InventoryManagement/
├── InventoryManagementSystem/          # ASP.NET Core MVC application
├── InventoryManagementSystem.BLL/      # Business logic, services, DTOs, mappings
├── InventoryManagementSystem.DAL/      # EF Core entities, database, repositories
├── InventoryManagementSystem.Tests/    # Test project
├── InventoryManagement.sln
└── INVENTORY_AI_SETUP.md               # Local AI assistant setup
```

- **Web layer:** MVC controllers, views, application configuration, and user-facing workflows.
- **Business Logic Layer (BLL):** application services, DTOs, validation-related logic, and mappings.
- **Data Access Layer (DAL):** database entities, EF Core configuration, repositories, and Unit of Work.
- **Repository and Unit of Work patterns:** help organize data operations and coordinate access to repositories.

## AI Inventory Assistant

The application includes an optional, **read-only** AI chat assistant for questions about inventory and business activity. With a local Ollama instance and the configured model, it can query information such as low-stock products, product stock, sales and purchase summaries, recent stock movements, suppliers, customers, and categories.

The assistant is designed to query application data; its tools do not create, update, or delete inventory records.

See [INVENTORY_AI_SETUP.md](INVENTORY_AI_SETUP.md) for setup and configuration instructions.

## Getting Started

### Prerequisites

- .NET SDK compatible with the target framework in the project file
- Microsoft SQL Server
- Git

Docker is also available for container-based workflows.

### Clone the repository

```bash
git clone https://github.com/Saad-elnagar/InventoryManagement.git
cd InventoryManagement
```

### Configure the database

Configure the SQL Server connection string using the application's configuration or local development settings. Do not commit real credentials or production secrets.

Ensure the database is available and apply the database setup required by the current project before running the application.

### Run the application

Open `InventoryManagement.sln` in your preferred .NET IDE, restore NuGet packages, set `InventoryManagementSystem` as the startup project, and run it.

> Exact database setup and local configuration can depend on your environment. Keep connection strings and secrets out of source control.

## What This Project Demonstrates

- Building an ASP.NET Core MVC application in C#.
- Separating UI, business logic, and data access responsibilities.
- Working with SQL Server and Entity Framework Core.
- Organizing data access with Repository and Unit of Work patterns.
- Modeling inventory workflows involving products, purchases, sales, and stock movements.
- Implementing search, pagination, reporting, role-based access, and optional local AI integration.

## Future Improvements

Potential next steps include expanding automated test coverage, improving deployment documentation, and continuing to refine reporting and inventory workflows.

## Author

**Saad Elnagar**

- GitHub: [Saad-elnagar](https://github.com/Saad-elnagar)
- Project repository: [InventoryManagement](https://github.com/Saad-elnagar/InventoryManagement)
