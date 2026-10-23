using Microsoft.Extensions.DependencyInjection;
using WebStore.Services.ConcreteServices;
using WebStore.Services.Interfaces;

namespace WebStore.Services.Configuration;

public static class ServiceRegistration
{
    public static IServiceCollection AddWebStoreServices(this IServiceCollection services, string? mapperLicenseKey = null)
    {
        services.AddAutoMapper(configuration =>
        {
            if (!string.IsNullOrWhiteSpace(mapperLicenseKey)) configuration.LicenseKey = mapperLicenseKey;
        }, typeof(MainProfile));
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IStoreService, StoreService>();
        services.AddScoped<IAddressService, AddressService>();
        return services;
    }
}
