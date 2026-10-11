using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.DAL.Entities;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<Purchase> Purchases { get; set; }

    public virtual DbSet<PurchaseItem> PurchaseItems { get; set; }

    public virtual DbSet<Sale> Sales { get; set; }

    public virtual DbSet<SaleItem> SaleItems { get; set; }

    public virtual DbSet<StockMovement> StockMovements { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplierProduct> SupplierProducts { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Categori__19093A2BE43052F5");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC271B8F7848");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Address).HasMaxLength(250);
            entity.Property(e => e.ContactName).HasMaxLength(150);
            entity.Property(e => e.CustomerName).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(50);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Products__B40CC6ED2A091627");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CategoryId).HasColumnName("CategoryID");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.LowStockThreshold).HasDefaultValue(10);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Sku)
                .HasMaxLength(50)
                .HasColumnName("SKU");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Products_Categories");
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Purchase__6B0A6BDEDA6E8D57");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.PurchaseDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Purchases_Suppliers");
        });

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Purchase__B48BB6A79007EE25");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.PurchaseId).HasColumnName("PurchaseID");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PurchaseItems_Products");

            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseItems)
                .HasForeignKey(d => d.PurchaseId)
                .HasConstraintName("FK_PurchaseItems_Purchases");
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Sales__1EE3C41F7EBA0028");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.CustomerInfo).HasMaxLength(500);
            entity.Property(e => e.SaleDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Customer).WithMany(p => p.Sales)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK_Sales_Customers");
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SaleItem__C60594611C1E0FA5");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SaleId).HasColumnName("SaleID");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Product).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SaleItems_Products");

            entity.HasOne(d => d.Sale).WithMany(p => p.SaleItems)
                .HasForeignKey(d => d.SaleId)
                .HasConstraintName("FK_SaleItems_Sales");
        });

        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StockMov__3214EC27A23901B1");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.MovementDate).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.MovementType).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.Reason).HasMaxLength(250);
            entity.Property(e => e.ReferenceType).HasMaxLength(50);

            entity.HasOne(d => d.Product).WithMany(p => p.StockMovements)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StockMovements_Products");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Supplier__4BE6669468E62417");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ContactName).HasMaxLength(150);
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.SupplierName).HasMaxLength(200);
        });

        modelBuilder.Entity<SupplierProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Supplier__8FA6ECDE23A83FAD");

            entity.HasIndex(e => new { e.SupplierId, e.ProductId }, "UQ_SupplierProducts").IsUnique();

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.ContractPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ProductId).HasColumnName("ProductID");
            entity.Property(e => e.SupplierId).HasColumnName("SupplierID");
            entity.Property(e => e.SupplierSku)
                .HasMaxLength(100)
                .HasColumnName("SupplierSKU");

            entity.HasOne(d => d.Product).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierProducts_Products");

            entity.HasOne(d => d.Supplier).WithMany(p => p.SupplierProducts)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SupplierProducts_Suppliers");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
