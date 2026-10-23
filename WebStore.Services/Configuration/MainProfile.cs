using AutoMapper;
using WebStore.Model.DataModels;
using WebStore.ViewModels.VM;

namespace WebStore.Services.Configuration;

public class MainProfile : Profile
{
    public MainProfile()
    {
        CreateMap<AddOrUpdateProductVm, Product>(MemberList.None);
        CreateMap<Product, ProductVm>().ForMember(v => v.Quantity, c => c.MapFrom(p => p.Stocks.Sum(s => s.Quantity)));
        CreateMap<OrderProduct, OrderItemVm>();
        CreateMap<Order, OrderVm>();
        CreateMap<Invoice, InvoiceVm>()
            .ForMember(v => v.OrderIds, c => c.MapFrom(i => i.Orders.Select(o => o.Id).OrderBy(id => id)))
            .ForMember(v => v.TotalAmount, c => c.MapFrom(i => i.Orders.Sum(o => o.TotalAmount)));
        CreateMap<AddOrUpdateAddressVm, Address>(MemberList.None);
        CreateMap<Address, AddressVm>();
        CreateMap<StationaryStoreEmployee, EmployeeVm>();
        CreateMap<StationaryStore, StoreVm>();
    }
}
