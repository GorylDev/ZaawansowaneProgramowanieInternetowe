namespace WebStore.Model.DataModels;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Tag { get; set; } = "";
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public byte[] ImageBytes { get; set; } = [];
    public decimal Price { get; set; }
    public float Weight { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public ICollection<ProductStock> Stocks { get; set; } = new List<ProductStock>();
    public ICollection<OrderProduct> OrderProducts { get; set; } = new List<OrderProduct>();
}

public class ProductStock
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}
