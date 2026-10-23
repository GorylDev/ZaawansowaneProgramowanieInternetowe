using Xunit;

namespace WebStore.Tests;

public class InvoiceServiceUnitTests : BaseUnitTests
{
    [Fact]
    public void AddsInvoiceAndAtomicallyReplacesOrderLinks()
    {
        var input = InvoiceInput();
        var firstOrder = input.OrderIds.Single();
        var secondOrder = Orders.AddOrUpdateOrder(OrderInput()).Id;
        input.OrderIds.Add(secondOrder);
        var invoice = Invoices.AddOrUpdateInvoice(input);
        Assert.Equal(5000, invoice.TotalAmount);
        var thirdOrder = Orders.AddOrUpdateOrder(OrderInput()).Id;
        input.Id = invoice.Id;
        input.Number = "FV/2026/2";
        input.OrderIds = [secondOrder, thirdOrder];
        Invoices.AddOrUpdateInvoice(input);
        DbContext.ChangeTracker.Clear();
        Assert.Null(Orders.GetOrder(o => o.Id == firstOrder)?.InvoiceId);
        Assert.Equal(invoice.Id, Orders.GetOrder(o => o.Id == thirdOrder)?.InvoiceId);
        Assert.Equal("FV/2026/2", Invoices.GetInvoice(i => i.Id == invoice.Id)?.Number);
        Assert.Single(Invoices.GetInvoices(i => i.Number == input.Number));
        Assert.Single(Invoices.GetInvoices());
    }

    [Theory]
    [InlineData("empty-number")][InlineData("duplicate-number")][InlineData("empty-orders")]
    [InlineData("duplicate-orders")][InlineData("missing-order")][InlineData("different-customers")]
    [InlineData("assigned-order")][InlineData("issue-date")]
    public void InvalidInvoiceDoesNotWriteOrStealOrderLinks(string reason)
    {
        var valid = Invoices.AddOrUpdateInvoice(InvoiceInput());
        var input = InvoiceInput();
        input.Number = "FV/2026/NEW";
        switch (reason)
        {
            case "empty-number": input.Number = " "; break;
            case "duplicate-number": input.Number = "  FV/2026/1  "; break;
            case "empty-orders": input.OrderIds.Clear(); break;
            case "duplicate-orders": input.OrderIds.Add(input.OrderIds[0]); break;
            case "missing-order": input.OrderIds.Add(99); break;
            case "different-customers": input.OrderIds.Add(Orders.AddOrUpdateOrder(OrderInput(customerId: 3)).Id); break;
            case "assigned-order": input.OrderIds.Add(valid.OrderIds[0]); break;
            case "issue-date": input.IssueDate = new(2026, 10, 19); break;
        }
        Assert.Throws<ArgumentException>(() => Invoices.AddOrUpdateInvoice(input));
        DbContext.ChangeTracker.Clear();
        Assert.Single(Invoices.GetInvoices());
        Assert.Equal(valid.Id, Orders.GetOrder(o => o.Id == valid.OrderIds[0])?.InvoiceId);
        Assert.Single(Invoices.GetInvoice(i => i.Id == valid.Id)!.OrderIds);
    }
}
