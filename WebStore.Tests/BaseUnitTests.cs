using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Configuration;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Tests;

public abstract class BaseUnitTests : IDisposable
{
    private readonly ServiceProvider provider;
    private readonly IServiceScope scope;
    protected readonly ApplicationDbContext DbContext;
    protected readonly IProductService Products;
    protected readonly IOrderService Orders;
    protected readonly IInvoiceService Invoices;
    protected readonly IStoreService Stores;
    protected readonly IAddressService Addresses;
    protected readonly CapturedLog Logs = new();

    protected BaseUnitTests()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddProvider(Logs));
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentityCore<User>().AddRoles<IdentityRole<int>>().AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddWebStoreServices();
        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        scope = provider.CreateScope();
        DbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Products = scope.ServiceProvider.GetRequiredService<IProductService>();
        Orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
        Invoices = scope.ServiceProvider.GetRequiredService<IInvoiceService>();
        Stores = scope.ServiceProvider.GetRequiredService<IStoreService>();
        Addresses = scope.ServiceProvider.GetRequiredService<IAddressService>();
        TestData.Seed(DbContext);
    }

    protected static AddOrUpdateProductVm ProductInput(int id = 0) => new()
    {
        Id = id, Name = "Macbook Pro", Description = "Komputer do pracy", Price = 6000,
        Weight = 1.1f, CategoryId = 1, SupplierId = 1, ImageBytes = [255, 128]
    };

    protected static AddOrUpdateOrderVm OrderInput(int id = 0, int customerId = 2) => new()
    {
        Id = id, CustomerId = customerId, OrderDate = new(2026, 10, 20), DeliveryDate = new(2026, 10, 25), TrackingNumber = 123,
        Items = [new() { ProductId = 1, Quantity = 2 }, new() { ProductId = 2, Quantity = 1 }]
    };

    protected static AddOrUpdateAddressVm AddressInput(int id = 0) => new()
    {
        Id = id, Street = "Długa", BuildingNumber = "10", PostalCode = "00-001", City = "Warszawa", Country = "Polska",
        CustomerId = 2, Role = AddressRole.Billing
    };

    protected AddOrUpdateInvoiceVm InvoiceInput(int id = 0) => new()
    {
        Id = id, Number = "FV/2026/1", IssueDate = new(2026, 10, 23), OrderIds = [Orders.AddOrUpdateOrder(OrderInput()).Id]
    };

    protected void CheckMapper() => scope.ServiceProvider.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    protected UserManager<User> UserManager => scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    public void Dispose()
    {
        scope.Dispose();
        provider.Dispose();
        GC.SuppressFinalize(this);
    }
}

internal static class TestData
{
    public static void Seed(ApplicationDbContext db)
    {
        object[] data = [
            new Supplier { Id = 1, FirstName = "Adam", LastName = "Bednarski", UserName = "supplier@example.test", RegistrationDate = new(2010, 1, 1) },
            new Customer { Id = 2, FirstName = "Anna", LastName = "Nowak", UserName = "anna@example.test" },
            new Customer { Id = 3, FirstName = "Jan", LastName = "Kowalski", UserName = "jan@example.test" },
            new Category { Id = 1, Name = "Computers", Tag = "#computer" },
            new StationaryStore { Id = 1, Name = "Sklep Warszawa" },
            new StationaryStoreEmployee { Id = 4, FirstName = "Ewa", LastName = "Lis", UserName = "ewa@example.test", StationaryStoreId = 1 },
            new Product { Id = 1, Name = "Monitor Dell 32", Description = "Monitor 32 cale", Price = 1000, Weight = 20, CategoryId = 1, SupplierId = 1, ImageBytes = [255, 128] },
            new Product { Id = 2, Name = "Mysz Logitech", Description = "Mysz do pracy", Price = 500, Weight = .5f, CategoryId = 1, SupplierId = 1, ImageBytes = [255, 112] }];
        if (db.Database.IsSqlServer())
        {
            db.Database.OpenConnection();
            using var transaction = db.Database.BeginTransaction();
            SaveGroup(data.OfType<Category>(), "SET IDENTITY_INSERT [Categories] ON", "SET IDENTITY_INSERT [Categories] OFF");
            SaveGroup(data.OfType<StationaryStore>(), "SET IDENTITY_INSERT [Stores] ON", "SET IDENTITY_INSERT [Stores] OFF");
            SaveGroup(data.OfType<User>(), "SET IDENTITY_INSERT [AspNetUsers] ON", "SET IDENTITY_INSERT [AspNetUsers] OFF");
            SaveGroup(data.OfType<Product>(), "SET IDENTITY_INSERT [Products] ON", "SET IDENTITY_INSERT [Products] OFF");
            transaction.Commit();
            db.Database.CloseConnection();
        }
        else
        {
            db.AddRange(data);
            db.SaveChanges();
        }
        db.ChangeTracker.Clear();

        void SaveGroup<T>(IEnumerable<T> entities, string enableSql, string disableSql) where T : class
        {
            db.Database.ExecuteSqlRaw(enableSql);
            db.AddRange(entities);
            db.SaveChanges();
            db.Database.ExecuteSqlRaw(disableSql);
        }
    }
}

public sealed class CapturedLog : ILoggerProvider
{
    public List<Exception> Errors { get; } = [];
    public ILogger CreateLogger(string categoryName) => new Sink(this);
    public void Dispose() { }
    private sealed class Sink(CapturedLog owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error && exception is not null) owner.Errors.Add(exception);
        }
    }
}
