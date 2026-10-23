namespace WebStore.Model.DataModels;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public decimal TotalAmount { get; set; }
    public long TrackingNumber { get; set; }
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public ICollection<OrderProduct> Items { get; set; } = new List<OrderProduct>();
}

public class OrderProduct
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public class Invoice
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
