using System.Linq.Expressions;
using WebStore.Model.DataModels;
using WebStore.ViewModels.VM;

namespace WebStore.Services.Interfaces;

public interface IProductService
{
    ProductVm AddOrUpdateProduct(AddOrUpdateProductVm input);
    ProductVm? GetProduct(Expression<Func<Product, bool>> filterExpression);
    IEnumerable<ProductVm> GetProducts(Expression<Func<Product, bool>>? filterExpression = null);
}

public interface IOrderService
{
    OrderVm AddOrUpdateOrder(AddOrUpdateOrderVm input);
    OrderVm? GetOrder(Expression<Func<Order, bool>> filterExpression);
    IEnumerable<OrderVm> GetOrders(Expression<Func<Order, bool>>? filterExpression = null);
}

public interface IInvoiceService
{
    InvoiceVm AddOrUpdateInvoice(AddOrUpdateInvoiceVm input);
    InvoiceVm? GetInvoice(Expression<Func<Invoice, bool>> filterExpression);
    IEnumerable<InvoiceVm> GetInvoices(Expression<Func<Invoice, bool>>? filterExpression = null);
}

public interface IStoreService
{
    StoreVm AddOrUpdateStore(AddOrUpdateStoreVm input);
    StoreVm? GetStore(Expression<Func<StationaryStore, bool>> filterExpression);
    IEnumerable<StoreVm> GetStores(Expression<Func<StationaryStore, bool>>? filterExpression = null);
}

public interface IAddressService
{
    AddressVm AddOrUpdateAddress(AddOrUpdateAddressVm input);
    AddressVm? GetAddress(Expression<Func<Address, bool>> filterExpression);
    IEnumerable<AddressVm> GetAddresses(Expression<Func<Address, bool>>? filterExpression = null);
}
