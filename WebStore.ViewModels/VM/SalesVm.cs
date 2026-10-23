using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM;

public class OrderItemVm
{
    [Range(1, int.MaxValue)] public int ProductId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; }
}

public class AddOrUpdateOrderVm
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    [Range(0, long.MaxValue)] public long TrackingNumber { get; set; }
    [Required, MinLength(1)] public List<OrderItemVm> Items { get; set; } = [];
}

public class OrderVm : AddOrUpdateOrderVm
{
    public decimal TotalAmount { get; set; }
    public int? InvoiceId { get; set; }
}

public class AddOrUpdateInvoiceVm
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Required, MaxLength(100)] public string Number { get; set; } = "";
    public DateTime IssueDate { get; set; }
    [Required, MinLength(1)] public List<int> OrderIds { get; set; } = [];
}

public class InvoiceVm : AddOrUpdateInvoiceVm
{
    public decimal TotalAmount { get; set; }
}
