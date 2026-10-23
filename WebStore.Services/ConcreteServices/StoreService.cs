using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;
using WebStore.Model.DataModels;
using WebStore.Services.Interfaces;
using WebStore.ViewModels.VM;

namespace WebStore.Services.ConcreteServices;

public class StoreService(ApplicationDbContext dbContext, IMapper mapper, ILogger<StoreService> logger)
    : BaseService(dbContext, mapper, logger), IStoreService
{
    public StoreVm AddOrUpdateStore(AddOrUpdateStoreVm input) => Execute(() =>
    {
        Validate(input);
        var store = input.Id == 0 ? new StationaryStore() : DbContext.Stores.Include(s => s.Addresses).Include(s => s.Employees)
            .SingleOrDefault(s => s.Id == input.Id) ?? throw new KeyNotFoundException("Nie znaleziono sklepu do aktualizacji.");
        store.Name = input.Name.Trim();
        if (input.Id == 0) DbContext.Stores.Add(store);
        DbContext.SaveChanges();
        return Mapper.Map<StoreVm>(store);
    });

    public StoreVm? GetStore(Expression<Func<StationaryStore, bool>> filterExpression)
        => ReadOne<StationaryStore, StoreVm>(Query(), filterExpression);
    public IEnumerable<StoreVm> GetStores(Expression<Func<StationaryStore, bool>>? filterExpression = null)
        => ReadMany<StationaryStore, StoreVm>(Query(), filterExpression);
    private IQueryable<StationaryStore> Query() => DbContext.Stores.Include(s => s.Addresses).Include(s => s.Employees).AsNoTracking();
}
