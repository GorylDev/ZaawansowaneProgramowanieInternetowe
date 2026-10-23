using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices;

public class ProductService(ApplicationDbContext dbContext, IMapper mapper, ILogger<ProductService> logger)
    : BaseService(dbContext, mapper, logger), IProductService
{
    public ProductVm AddOrUpdateProduct(AddOrUpdateProductVm input) => Execute(() =>
    {
        Validate(input);
        Require(float.IsFinite(input.Weight), "Masa musi być liczbą skończoną.");
        Require(DbContext.Categories.Any(c => c.Id == input.CategoryId), "Nie istnieje wskazana kategoria.");
        Require(DbContext.Suppliers.Any(s => s.Id == input.SupplierId), "Nie istnieje wskazany dostawca.");
        var product = input.Id == 0 ? new Product() : DbContext.Products.Include(p => p.Stocks)
            .SingleOrDefault(p => p.Id == input.Id) ?? throw new KeyNotFoundException("Nie znaleziono produktu do aktualizacji.");
        Mapper.Map(input, product);
        product.Name = product.Name.Trim();
        product.Description = product.Description.Trim();
        product.Price = decimal.Round(product.Price, 2, MidpointRounding.AwayFromZero);
        if (input.Id == 0) DbContext.Products.Add(product);
        DbContext.SaveChanges();
        return Mapper.Map<ProductVm>(product);
    });

    public ProductVm? GetProduct(Expression<Func<Product, bool>> filterExpression)
        => ReadOne<Product, ProductVm>(DbContext.Products.Include(p => p.Stocks).AsNoTracking(), filterExpression);
    public IEnumerable<ProductVm> GetProducts(Expression<Func<Product, bool>>? filterExpression = null)
        => ReadMany<Product, ProductVm>(DbContext.Products.Include(p => p.Stocks).AsNoTracking(), filterExpression);
}
