using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices;

public class AddressService(ApplicationDbContext dbContext, IMapper mapper, ILogger<AddressService> logger)
    : BaseService(dbContext, mapper, logger), IAddressService
{
    public AddressVm AddOrUpdateAddress(AddOrUpdateAddressVm input) => Execute(() =>
    {
        Validate(input);
        Require(input.CustomerId.HasValue != input.StationaryStoreId.HasValue, "Adres musi mieć dokładnie jednego właściciela.");
        if (input.CustomerId is int customerId)
        {
            Require(DbContext.Customers.Any(c => c.Id == customerId), "Nie istnieje wskazany klient.");
            Require(input.Role is AddressRole.Billing or AddressRole.Shipping, "Adres klienta wymaga roli Billing lub Shipping.");
            Require(!DbContext.Addresses.Any(a => a.CustomerId == customerId && a.Role == input.Role && a.Id != input.Id), "Klient ma już adres z tą rolą.");
        }
        else
        {
            Require(DbContext.Stores.Any(s => s.Id == input.StationaryStoreId), "Nie istnieje wskazany sklep.");
            Require(input.Role == AddressRole.Store, "Adres sklepu wymaga roli Store.");
        }
        var address = input.Id == 0 ? new Address() : DbContext.Addresses.SingleOrDefault(a => a.Id == input.Id)
            ?? throw new KeyNotFoundException("Nie znaleziono adresu do aktualizacji.");
        Mapper.Map(input, address);
        address.Street = address.Street.Trim();
        address.BuildingNumber = address.BuildingNumber.Trim();
        address.City = address.City.Trim();
        address.Country = address.Country.Trim();
        address.PostalCode = address.PostalCode.Trim();
        if (input.Id == 0) DbContext.Addresses.Add(address);
        DbContext.SaveChanges();
        return Mapper.Map<AddressVm>(address);
    });

    public AddressVm? GetAddress(Expression<Func<Address, bool>> filterExpression)
        => ReadOne<Address, AddressVm>(DbContext.Addresses.AsNoTracking(), filterExpression);
    public IEnumerable<AddressVm> GetAddresses(Expression<Func<Address, bool>>? filterExpression = null)
        => ReadMany<Address, AddressVm>(DbContext.Addresses.AsNoTracking(), filterExpression);
}
