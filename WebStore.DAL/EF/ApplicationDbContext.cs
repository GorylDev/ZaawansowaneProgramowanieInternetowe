using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebStore.Model.DataModels;

namespace WebStore.DAL.EF;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<User, IdentityRole<int>, int>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<StationaryStoreEmployee> Employees => Set<StationaryStoreEmployee>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductStock> ProductStocks => Set<ProductStock>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderProduct> OrderProducts => Set<OrderProduct>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<StationaryStore> Stores => Set<StationaryStore>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<User>().HasDiscriminator<string>("UserType")
            .HasValue<User>("User").HasValue<Customer>("Customer")
            .HasValue<Supplier>("Supplier").HasValue<StationaryStoreEmployee>("Employee");
        model.Entity<Product>(b =>
        {
            b.Property(p => p.Price).HasPrecision(18, 2);
            b.HasOne(p => p.Supplier).WithMany(s => s.Products).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
            b.ToTable("Products", t => t.HasCheckConstraint("CK_Product_Nonnegative", "[Price] >= 0 AND [Weight] >= 0"));
        });
        model.Entity<ProductStock>(b =>
        {
            b.HasOne(s => s.Product).WithMany(p => p.Stocks).HasForeignKey(s => s.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.ToTable("ProductStocks", t => t.HasCheckConstraint("CK_Stock_Quantity", "[Quantity] >= 0"));
        });
        model.Entity<Order>(b =>
        {
            b.Property(o => o.TotalAmount).HasPrecision(18, 2);
            b.HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(o => o.Invoice).WithMany(i => i.Orders).HasForeignKey(o => o.InvoiceId).OnDelete(DeleteBehavior.Restrict);
            b.ToTable("Orders", t => t.HasCheckConstraint("CK_Order_Values", "[TotalAmount] >= 0 AND [TrackingNumber] >= 0 AND [DeliveryDate] >= [OrderDate]"));
        });
        model.Entity<OrderProduct>(b =>
        {
            b.HasKey(i => new { i.OrderId, i.ProductId });
            b.HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(i => i.Product).WithMany(p => p.OrderProducts).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.ToTable("OrderProducts", t => t.HasCheckConstraint("CK_OrderProduct_Quantity", "[Quantity] > 0"));
        });
        model.Entity<Invoice>(b =>
        {
            b.Property(i => i.Number).HasMaxLength(100);
            b.HasIndex(i => i.Number).IsUnique();
        });
        model.Entity<Address>(b =>
        {
            b.HasOne(a => a.Customer).WithMany(c => c.Addresses).HasForeignKey(a => a.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.StationaryStore).WithMany(s => s.Addresses).HasForeignKey(a => a.StationaryStoreId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(a => new { a.CustomerId, a.Role }).IsUnique().HasFilter("[CustomerId] IS NOT NULL");
            b.ToTable("Addresses", t => t.HasCheckConstraint("CK_Address_OwnerRole",
                "([CustomerId] IS NOT NULL AND [StationaryStoreId] IS NULL AND [Role] IN (0, 1)) OR ([CustomerId] IS NULL AND [StationaryStoreId] IS NOT NULL AND [Role] = 2)"));
        });
        model.Entity<StationaryStoreEmployee>().HasOne(e => e.StationaryStore).WithMany(s => s.Employees)
            .HasForeignKey(e => e.StationaryStoreId).OnDelete(DeleteBehavior.Restrict);

        // Również relacje infrastruktury Identity nie usuwają danych kaskadowo.
        foreach (var foreignKey in model.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
