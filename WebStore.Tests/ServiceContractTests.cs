using Xunit;

namespace WebStore.Tests;

public class ServiceContractTests : BaseUnitTests
{
    [Fact] public void MapperConfigurationAndIdentityAreAvailable() { CheckMapper(); Assert.NotNull(UserManager); }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void NullWriteIsLoggedAndRethrown(int service)
    {
        var error = Assert.Throws<ArgumentNullException>(() =>
        {
            switch (service)
            {
                case 0: Products.AddOrUpdateProduct(null!); break;
                case 1: Orders.AddOrUpdateOrder(null!); break;
                case 2: Invoices.AddOrUpdateInvoice(null!); break;
                case 3: Stores.AddOrUpdateStore(null!); break;
                case 4: Addresses.AddOrUpdateAddress(null!); break;
            }
        });
        Assert.Contains(error, Logs.Errors);
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void NullSinglePredicateIsRejected(int service)
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            switch (service)
            {
                case 0: Products.GetProduct(null!); break;
                case 1: Orders.GetOrder(null!); break;
                case 2: Invoices.GetInvoice(null!); break;
                case 3: Stores.GetStore(null!); break;
                case 4: Addresses.GetAddress(null!); break;
            }
        });
    }

    [Fact]
    public void MissingSingleReturnsNullAndUnmatchedListsAreEmpty()
    {
        Assert.Null(Products.GetProduct(_ => false)); Assert.Empty(Products.GetProducts(_ => false));
        Assert.Null(Orders.GetOrder(_ => false)); Assert.Empty(Orders.GetOrders(_ => false));
        Assert.Null(Invoices.GetInvoice(_ => false)); Assert.Empty(Invoices.GetInvoices(_ => false));
        Assert.Null(Stores.GetStore(_ => false)); Assert.Empty(Stores.GetStores(_ => false));
        Assert.Null(Addresses.GetAddress(_ => false)); Assert.Empty(Addresses.GetAddresses(_ => false));
    }

    [Theory][InlineData(0)][InlineData(1)][InlineData(2)][InlineData(3)][InlineData(4)]
    public void UpdatingMissingIdDoesNotCreateAnEntity(int service)
    {
        Assert.Throws<KeyNotFoundException>(() =>
        {
            switch (service)
            {
                case 0: Products.AddOrUpdateProduct(ProductInput(99)); break;
                case 1: Orders.AddOrUpdateOrder(OrderInput(99)); break;
                case 2: Invoices.AddOrUpdateInvoice(InvoiceInput(99)); break;
                case 3: Stores.AddOrUpdateStore(new() { Id = 99, Name = "Nieistniejący" }); break;
                case 4: Addresses.AddOrUpdateAddress(AddressInput(99)); break;
            }
        });
        DbContext.ChangeTracker.Clear();
        Assert.Equal(2, Products.GetProducts().Count());
        Assert.Empty(Invoices.GetInvoices());
        Assert.Single(Stores.GetStores());
        Assert.Empty(Addresses.GetAddresses());
    }
}
