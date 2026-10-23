using Microsoft.EntityFrameworkCore;
using WebStore.Model.DataModels;
using WebStore.ViewModels.VM;
using Xunit;

namespace WebStore.Tests;

public class AddressAndStoreUnitTests : BaseUnitTests
{
    [Fact]
    public void CustomerAddressesHaveSeparateRolesAndCanBeUpdated()
    {
        var billing = Addresses.AddOrUpdateAddress(AddressInput());
        var input = AddressInput();
        input.Role = AddressRole.Shipping;
        var shipping = Addresses.AddOrUpdateAddress(input);
        input.Id = shipping.Id;
        input.City = "Kraków";
        Addresses.AddOrUpdateAddress(input);
        DbContext.ChangeTracker.Clear();
        var customer = DbContext.Customers.Include(c => c.Addresses).Single(c => c.Id == 2);
        Assert.Equal(billing.Id, customer.BillingAddress?.Id);
        Assert.Equal("Kraków", customer.ShippingAddress?.City);
        Assert.Equal(2, Addresses.GetAddresses().Count());
        Assert.Single(Addresses.GetAddresses(a => a.Role == AddressRole.Shipping));
        Assert.Equal("Kraków", Addresses.GetAddress(a => a.Id == shipping.Id)?.City);
    }

    [Fact]
    public void StoreCanBeAddedAndRenamedWithoutLosingEmployeesOrAddresses()
    {
        var input = AddressInput();
        input.CustomerId = null;
        input.StationaryStoreId = 1;
        input.Role = AddressRole.Store;
        var address = Addresses.AddOrUpdateAddress(input);
        input.Id = address.Id;
        input.City = "Gdańsk";
        Addresses.AddOrUpdateAddress(input);
        var added = Stores.AddOrUpdateStore(new() { Name = "Nowy sklep" });
        Assert.True(added.Id > 1);
        Stores.AddOrUpdateStore(new() { Id = 1, Name = "Sklep po zmianie" });
        DbContext.ChangeTracker.Clear();
        var store = Stores.GetStore(s => s.Id == 1)!;
        Assert.Equal("Sklep po zmianie", store.Name);
        Assert.Equal("Gdańsk", Assert.Single(store.Addresses).City);
        Assert.Equal("Ewa", Assert.Single(store.Employees).FirstName);
        Assert.Equal(2, Stores.GetStores().Count());
        Assert.Single(Stores.GetStores(s => s.Name == "Nowy sklep"));
        Assert.Throws<ArgumentException>(() => Stores.AddOrUpdateStore(new() { Id = 1, Name = " " }));
        Assert.Equal("Sklep po zmianie", Stores.GetStore(s => s.Id == 1)?.Name);
    }

    [Theory]
    [InlineData("no-owner")][InlineData("both-owners")][InlineData("missing-customer")][InlineData("missing-store")]
    [InlineData("customer-role")][InlineData("store-role")][InlineData("duplicate-role")]
    [InlineData("street")][InlineData("number")][InlineData("postal")][InlineData("city")][InlineData("country")][InlineData("enum")]
    public void InvalidAddressIsRejected(string reason)
    {
        var input = AddressInput();
        switch (reason)
        {
            case "no-owner": input.CustomerId = null; break;
            case "both-owners": input.StationaryStoreId = 1; break;
            case "missing-customer": input.CustomerId = 99; break;
            case "missing-store": input.CustomerId = null; input.StationaryStoreId = 99; input.Role = AddressRole.Store; break;
            case "customer-role": input.Role = AddressRole.Store; break;
            case "store-role": input.CustomerId = null; input.StationaryStoreId = 1; break;
            case "duplicate-role": Addresses.AddOrUpdateAddress(AddressInput()); break;
            case "street": input.Street = " "; break;
            case "number": input.BuildingNumber = ""; break;
            case "postal": input.PostalCode = ""; break;
            case "city": input.City = ""; break;
            case "country": input.Country = ""; break;
            case "enum": input.Role = (AddressRole)99; break;
        }
        var before = DbContext.Addresses.Count();
        Assert.Throws<ArgumentException>(() => Addresses.AddOrUpdateAddress(input));
        DbContext.ChangeTracker.Clear();
        Assert.Equal(before, DbContext.Addresses.Count());
    }
}
