using Xunit;

namespace WebStore.Tests;

public class OrderServiceUnitTests : BaseUnitTests
{
    [Fact]
    public void AddsOrderAndRecalculatesOnEditButPreservesTotalAfterPriceChange()
    {
        var result = Orders.AddOrUpdateOrder(OrderInput());
        Assert.Equal(2500, result.TotalAmount);
        Assert.Equal(2, result.Items.Count);
        var product = ProductInput(1);
        product.Price = 2000;
        Products.AddOrUpdateProduct(product);
        Assert.Equal(2500, Orders.GetOrder(o => o.Id == result.Id)?.TotalAmount);
        var edit = OrderInput(result.Id);
        edit.Items[0].Quantity = 3;
        Orders.AddOrUpdateOrder(edit);
        DbContext.ChangeTracker.Clear();
        Assert.Equal(6500, Orders.GetOrder(o => o.Id == result.Id)?.TotalAmount);
        Assert.Single(Orders.GetOrders(o => o.TotalAmount == 6500));
        Assert.Single(Orders.GetOrders());
        Assert.Empty(DbContext.ProductStocks);
    }

    [Fact]
    public void UpdatingItemsRemovesAndAddsRelations()
    {
        var order = Orders.AddOrUpdateOrder(OrderInput());
        var edit = OrderInput(order.Id);
        edit.Items.RemoveAt(0);
        Orders.AddOrUpdateOrder(edit);
        DbContext.ChangeTracker.Clear();
        Assert.Single(Orders.GetOrder(o => o.Id == order.Id)!.Items);
        Assert.Equal(500, Orders.GetOrder(o => o.Id == order.Id)!.TotalAmount);
    }

    [Theory]
    [InlineData("customer")][InlineData("product")][InlineData("empty")][InlineData("duplicate")]
    [InlineData("quantity")][InlineData("date")][InlineData("tracking")]
    public void InvalidOrderIsRejectedWithoutPartialChanges(string reason)
    {
        var original = Orders.AddOrUpdateOrder(OrderInput());
        var edit = OrderInput(original.Id);
        edit.TrackingNumber = 456;
        switch (reason)
        {
            case "customer": edit.CustomerId = 99; break;
            case "product": edit.Items[0].ProductId = 99; break;
            case "empty": edit.Items.Clear(); break;
            case "duplicate": edit.Items[1].ProductId = 1; break;
            case "quantity": edit.Items[0].Quantity = 0; break;
            case "date": edit.DeliveryDate = edit.OrderDate.AddDays(-1); break;
            case "tracking": edit.TrackingNumber = -1; break;
        }
        Assert.Throws<ArgumentException>(() => Orders.AddOrUpdateOrder(edit));
        DbContext.ChangeTracker.Clear();
        var unchanged = Orders.GetOrder(o => o.Id == original.Id)!;
        Assert.Equal(123, unchanged.TrackingNumber);
        Assert.Equal(2500, unchanged.TotalAmount);
        Assert.Equal(2, unchanged.Items.Count);
    }

    [Fact]
    public void InvoicedOrderCannotBeEdited()
    {
        var invoice = Invoices.AddOrUpdateInvoice(InvoiceInput());
        Assert.Throws<ArgumentException>(() => Orders.AddOrUpdateOrder(OrderInput(invoice.OrderIds.Single())));
        DbContext.ChangeTracker.Clear();
        Assert.Equal(2500, Orders.GetOrder(o => o.Id == invoice.OrderIds.Single())?.TotalAmount);
    }
}
