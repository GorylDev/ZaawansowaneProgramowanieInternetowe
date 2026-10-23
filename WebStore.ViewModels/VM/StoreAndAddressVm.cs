using System.ComponentModel.DataAnnotations;
using WebStore.Model.DataModels;

namespace WebStore.ViewModels.VM;

public class AddOrUpdateAddressVm
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Required] public string Street { get; set; } = "";
    [Required] public string BuildingNumber { get; set; } = "";
    public string? ApartmentNumber { get; set; }
    [Required] public string PostalCode { get; set; } = "";
    [Required] public string City { get; set; } = "";
    [Required] public string Country { get; set; } = "";
    [EnumDataType(typeof(AddressRole))] public AddressRole Role { get; set; }
    public int? CustomerId { get; set; }
    public int? StationaryStoreId { get; set; }
}

public class AddressVm : AddOrUpdateAddressVm { }

public class AddOrUpdateStoreVm
{
    [Range(0, int.MaxValue)] public int Id { get; set; }
    [Required] public string Name { get; set; } = "";
}

public class StoreVm : AddOrUpdateStoreVm
{
    public List<AddressVm> Addresses { get; set; } = [];
    public List<EmployeeVm> Employees { get; set; } = [];
}

public class EmployeeVm
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}
