FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["InventoryManagementSystem/InventoryManagementSystem.csproj", "InventoryManagementSystem/"]
COPY ["InventoryManagementSystem.BLL/InventoryManagementSystem.BLL.csproj", "InventoryManagementSystem.BLL/"]
COPY ["InventoryManagementSystem.DAL/InventoryManagementSystem.DAL.csproj", "InventoryManagementSystem.DAL/"]

RUN dotnet restore "InventoryManagementSystem/InventoryManagementSystem.csproj"

COPY . .

WORKDIR "/src/InventoryManagementSystem"
RUN dotnet build "InventoryManagementSystem.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "InventoryManagementSystem.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventoryManagementSystem.dll"]
