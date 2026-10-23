using System.ComponentModel.DataAnnotations;

namespace WebStore.ViewModels.VM;

public class AddOrUpdateProductVm
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Required] public string Description { get; set; } = "";
    [Required] public byte[] ImageBytes { get; set; } = [];
    [Required] public string Name { get; set; } = "";
    [Required, Range(typeof(decimal), "0", "9999999999999999.99", ParseLimitsInInvariantCulture = true)] public decimal Price { get; set; }
    [Required, Range(0, float.MaxValue)] public float Weight { get; set; }
    [Required, Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Required, Range(1, int.MaxValue)] public int SupplierId { get; set; }
}

public class ProductVm
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public byte[] ImageBytes { get; set; } = [];
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public float Weight { get; set; }
    public int Quantity { get; set; }
}
