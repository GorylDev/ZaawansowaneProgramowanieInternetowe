using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace WebStore.Model.DataModels;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public DateTime RegistrationDate { get; set; }
}

public class Customer : User
{
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    [NotMapped] public Address? BillingAddress => Addresses.SingleOrDefault(a => a.Role == AddressRole.Billing);
    [NotMapped] public Address? ShippingAddress => Addresses.SingleOrDefault(a => a.Role == AddressRole.Shipping);
}

public class Supplier : User
{
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class StationaryStoreEmployee : User
{
    public int StationaryStoreId { get; set; }
    public StationaryStore StationaryStore { get; set; } = null!;
}
