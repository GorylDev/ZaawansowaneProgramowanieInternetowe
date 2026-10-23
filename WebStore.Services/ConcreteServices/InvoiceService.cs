using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices;

public class InvoiceService(ApplicationDbContext dbContext, IMapper mapper, ILogger<InvoiceService> logger)
    : BaseService(dbContext, mapper, logger), IInvoiceService
{
    public InvoiceVm AddOrUpdateInvoice(AddOrUpdateInvoiceVm input) => Execute(() =>
    {
        Validate(input);
        var number = input.Number.Trim();
        Require(!DbContext.Invoices.Any(i => i.Id != input.Id && i.Number.ToUpper() == number.ToUpper()), "Numer faktury jest już używany.");
        Require(input.OrderIds.All(id => id > 0) && input.OrderIds.Distinct().Count() == input.OrderIds.Count, "Niepoprawna lub powtórzona lista zamówień.");
        var orders = DbContext.Orders.Where(o => input.OrderIds.Contains(o.Id)).ToList();
        Require(orders.Count == input.OrderIds.Count, "Nie istnieje wskazane zamówienie.");
        Require(orders.Select(o => o.CustomerId).Distinct().Count() == 1, "Faktura może obejmować zamówienia jednego klienta.");
        Require(orders.All(o => o.InvoiceId is null || (input.Id > 0 && o.InvoiceId == input.Id)), "Zamówienie należy do innej faktury.");
        Require(orders.All(o => input.IssueDate.Date >= o.OrderDate.Date), "Data wystawienia poprzedza zamówienie.");
        var invoice = input.Id == 0 ? new Invoice() : DbContext.Invoices.Include(i => i.Orders)
            .SingleOrDefault(i => i.Id == input.Id) ?? throw new KeyNotFoundException("Nie znaleziono faktury do aktualizacji.");

        invoice.Number = number;
        invoice.IssueDate = input.IssueDate;
        foreach (var removed in invoice.Orders.Where(o => !input.OrderIds.Contains(o.Id)).ToList())
        {
            removed.InvoiceId = null;
            removed.Invoice = null;
            invoice.Orders.Remove(removed);
        }
        foreach (var order in orders)
            if (!invoice.Orders.Contains(order)) invoice.Orders.Add(order);
        if (input.Id == 0) DbContext.Invoices.Add(invoice);
        DbContext.SaveChanges();
        return Mapper.Map<InvoiceVm>(invoice);
    });

    public InvoiceVm? GetInvoice(Expression<Func<Invoice, bool>> filterExpression)
        => ReadOne<Invoice, InvoiceVm>(DbContext.Invoices.Include(i => i.Orders).AsNoTracking(), filterExpression);
    public IEnumerable<InvoiceVm> GetInvoices(Expression<Func<Invoice, bool>>? filterExpression = null)
        => ReadMany<Invoice, InvoiceVm>(DbContext.Invoices.Include(i => i.Orders).AsNoTracking(), filterExpression);
}
