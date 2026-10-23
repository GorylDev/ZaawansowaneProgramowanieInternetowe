using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Configuration;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;
using Xunit;

namespace WebStore.Tests;

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSTORE_SQL_TEST_CONNECTION")))
            Skip = "Ustaw WEBSTORE_SQL_TEST_CONNECTION, aby sprawdzić rzeczywisty SQL Server.";
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSTORE_SQL_TEST_CONNECTION")))
            Skip = "Ustaw WEBSTORE_SQL_TEST_CONNECTION, aby sprawdzić rzeczywisty SQL Server.";
    }
}

[Trait("Category", "SqlServer")]
public class SqlServerTests
{
    [SqlServerFact]
    public void MigrationPersistsFullWorkflowAndTphHierarchy()
    {
        using var test = new SqlTestDatabase();
        var db = test.Db;
        var products = test.Get<IProductService>();
        var orders = test.Get<IOrderService>();
        var invoices = test.Get<IInvoiceService>();
        var addresses = test.Get<IAddressService>();
        db.ProductStocks.AddRange(new() { ProductId = 1, Quantity = 2 }, new() { ProductId = 1, Quantity = 3 });
        db.SaveChanges();
        addresses.AddOrUpdateAddress(new() { CustomerId = 2, Role = AddressRole.Billing, Street = "Długa", BuildingNumber = "1", PostalCode = "00-001", City = "Warszawa", Country = "Polska" });
        addresses.AddOrUpdateAddress(new() { StationaryStoreId = 1, Role = AddressRole.Store, Street = "Krótka", BuildingNumber = "2", PostalCode = "00-002", City = "Warszawa", Country = "Polska" });
        var first = orders.AddOrUpdateOrder(Input());
        var second = orders.AddOrUpdateOrder(Input());
        var invoice = invoices.AddOrUpdateInvoice(new() { Number = "FV/SQL/1", IssueDate = new(2026, 10, 23), OrderIds = [first.Id, second.Id] });
        db.ChangeTracker.Clear();
        Assert.Equal(5000, invoices.GetInvoice(i => i.Id == invoice.Id)?.TotalAmount);
        Assert.Equal(2, invoices.GetInvoice(i => i.Id == invoice.Id)!.OrderIds.Count);
        Assert.Equal(5, products.GetProduct(p => p.Id == 1)?.Quantity);
        Assert.Single(db.Suppliers);
        Assert.Equal(2, db.Customers.Count());
        Assert.Single(db.Employees);
        Assert.IsType<Customer>(db.Users.Single(u => u.Id == 2));
        Assert.NotNull(db.Customers.Include(c => c.Addresses).Single(c => c.Id == 2).BillingAddress);
        Assert.Single(test.Get<IStoreService>().GetStore(s => s.Id == 1)!.Addresses);
        Assert.Single(test.Get<IStoreService>().GetStore(s => s.Id == 1)!.Employees);
        Assert.NotEmpty(db.Database.GetAppliedMigrations());
    }

    [SqlServerTheory]
    [InlineData("no-owner")][InlineData("two-owners")][InlineData("wrong-role")][InlineData("foreign-key")][InlineData("duplicate-role")]
    public void SqlConstraintsRejectInvalidAddresses(string reason)
    {
        using var test = new SqlTestDatabase();
        var address = new Address { CustomerId = 2, Role = AddressRole.Billing, Street = "Długa", BuildingNumber = "1", PostalCode = "00-001", City = "Warszawa", Country = "Polska" };
        switch (reason)
        {
            case "no-owner": address.CustomerId = null; break;
            case "two-owners": address.StationaryStoreId = 1; break;
            case "wrong-role": address.Role = AddressRole.Store; break;
            case "foreign-key": address.CustomerId = 99; break;
            case "duplicate-role":
                test.Db.Addresses.Add(new Address { CustomerId = 2, Role = AddressRole.Billing, Street = "Krótka", BuildingNumber = "2", PostalCode = "00-001", City = "Warszawa", Country = "Polska" });
                test.Db.SaveChanges();
                break;
        }
        test.Db.Addresses.Add(address);
        Assert.Throws<DbUpdateException>(() => test.Db.SaveChanges());
    }

    [SqlServerFact]
    public void ForeignKeysPreventCascadeDeletion()
    {
        using var test = new SqlTestDatabase();
        var supplier = test.Db.Suppliers.Single();
        test.Db.Suppliers.Remove(supplier);
        Assert.Throws<DbUpdateException>(() => test.Db.SaveChanges());
        test.Db.ChangeTracker.Clear();
        Assert.Equal(2, test.Db.Products.Count());
    }

    [SqlServerFact]
    public void UpdatesReplaceOrderItemsAndInvoiceLinksOnSqlServer()
    {
        using var test = new SqlTestDatabase();
        var orders = test.Get<IOrderService>();
        var invoices = test.Get<IInvoiceService>();
        var first = orders.AddOrUpdateOrder(Input());
        var edit = Input();
        edit.Id = first.Id;
        edit.Items = [new() { ProductId = 2, Quantity = 3 }];
        Assert.Equal(1500, orders.AddOrUpdateOrder(edit).TotalAmount);
        var second = orders.AddOrUpdateOrder(Input());
        var invoice = invoices.AddOrUpdateInvoice(new() { Number = "FV/SQL/2", IssueDate = new(2026, 10, 23), OrderIds = [first.Id] });
        invoices.AddOrUpdateInvoice(new() { Id = invoice.Id, Number = "FV/SQL/2", IssueDate = new(2026, 10, 23), OrderIds = [second.Id] });
        test.Db.ChangeTracker.Clear();
        Assert.Null(orders.GetOrder(o => o.Id == first.Id)?.InvoiceId);
        Assert.Equal(invoice.Id, orders.GetOrder(o => o.Id == second.Id)?.InvoiceId);
        Assert.Single(orders.GetOrder(o => o.Id == first.Id)!.Items);
    }

    private static AddOrUpdateOrderVm Input() => new()
    {
        CustomerId = 2, OrderDate = new(2026, 10, 20), DeliveryDate = new(2026, 10, 25),
        Items = [new() { ProductId = 1, Quantity = 2 }, new() { ProductId = 2, Quantity = 1 }]
    };
}

internal sealed class SqlTestDatabase : IDisposable
{
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    private readonly string databaseName = "WebStore_Test_" + Guid.NewGuid().ToString("N");
    public ApplicationDbContext Db { get; }

    public SqlTestDatabase()
    {
        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("WEBSTORE_SQL_TEST_CONNECTION")) { InitialCatalog = databaseName };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(builder.ConnectionString));
        services.AddIdentityCore<User>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddWebStoreServices();
        provider = services.BuildServiceProvider();
        scope = provider.CreateScope();
        Db = Get<ApplicationDbContext>();
        Db.Database.Migrate();
        TestData.Seed(Db);
    }

    public T Get<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    public void Dispose()
    {
        // Usuwana jest wyłącznie baza o losowej nazwie utworzona przez ten test.
        if (Db.Database.GetDbConnection().Database == databaseName && databaseName.StartsWith("WebStore_Test_", StringComparison.Ordinal))
            Db.Database.EnsureDeleted();
        scope.Dispose();
        provider.Dispose();
    }
}
