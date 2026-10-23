using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices;

public class OrderService(ApplicationDbContext dbContext, IMapper mapper, ILogger<OrderService> logger)
    : BaseService(dbContext, mapper, logger), IOrderService
{
    public OrderVm AddOrUpdateOrder(AddOrUpdateOrderVm input) => Execute(() =>
    {
        Validate(input);
        foreach (var item in input.Items) Validate(item);
        Require(input.Items.Select(i => i.ProductId).Distinct().Count() == input.Items.Count, "Produkt powtarza się w zamówieniu.");
        Require(input.DeliveryDate >= input.OrderDate, "Dostawa nie może poprzedzać zamówienia.");
        Require(DbContext.Customers.Any(c => c.Id == input.CustomerId), "Nie istnieje wskazany klient.");
        var ids = input.Items.Select(i => i.ProductId).ToList();
        var products = DbContext.Products.Where(p => ids.Contains(p.Id)).ToDictionary(p => p.Id);
        Require(products.Count == ids.Count, "Nie istnieje wskazany produkt.");
        var total = input.Items.Sum(i => products[i.ProductId].Price * i.Quantity);
        Require(total <= 9999999999999999.99m, "Kwota zamówienia przekracza dopuszczalny zakres.");
        var order = input.Id == 0 ? new Order() : DbContext.Orders.Include(o => o.Items)
            .SingleOrDefault(o => o.Id == input.Id) ?? throw new KeyNotFoundException("Nie znaleziono zamówienia do aktualizacji.");
        Require(order.InvoiceId is null, "Nie można edytować zamówienia objętego fakturą.");

        order.CustomerId = input.CustomerId;
        order.OrderDate = input.OrderDate;
        order.DeliveryDate = input.DeliveryDate;
        order.TrackingNumber = input.TrackingNumber;
        order.TotalAmount = total;
        foreach (var removed in order.Items.Where(i => !ids.Contains(i.ProductId)).ToList())
        {
            DbContext.OrderProducts.Remove(removed);
            order.Items.Remove(removed);
        }
        foreach (var requested in input.Items)
        {
            var item = order.Items.SingleOrDefault(i => i.ProductId == requested.ProductId);
            if (item is null)
            {
                item = new OrderProduct { ProductId = requested.ProductId };
                order.Items.Add(item);
            }
            item.Quantity = requested.Quantity;
        }
        if (input.Id == 0) DbContext.Orders.Add(order);
        DbContext.SaveChanges();
        return Mapper.Map<OrderVm>(order);
    });

    public OrderVm? GetOrder(Expression<Func<Order, bool>> filterExpression)
        => ReadOne<Order, OrderVm>(DbContext.Orders.Include(o => o.Items).AsNoTracking(), filterExpression);
    public IEnumerable<OrderVm> GetOrders(Expression<Func<Order, bool>>? filterExpression = null)
        => ReadMany<Order, OrderVm>(DbContext.Orders.Include(o => o.Items).AsNoTracking(), filterExpression);
}
