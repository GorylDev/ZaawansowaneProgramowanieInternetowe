using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WebStore.DAL.EF;

namespace WebStore.Services.ConcreteServices;

public abstract class BaseService(ApplicationDbContext dbContext, IMapper mapper, ILogger logger)
{
    protected readonly ApplicationDbContext DbContext = dbContext;
    protected readonly IMapper Mapper = mapper;
    protected readonly ILogger Logger = logger;

    protected T Execute<T>(Func<T> operation)
    {
        try { return operation(); }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Błąd operacji usługi {Service}", GetType().Name);
            throw;
        }
    }

    protected static void Validate(object input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(input, new ValidationContext(input), results, true))
            throw new ArgumentException(string.Join("; ", results.Select(r => r.ErrorMessage)), nameof(input));
    }

    protected static void Require(bool condition, string message)
    {
        if (!condition) throw new ArgumentException(message);
    }

    protected TVm? ReadOne<TEntity, TVm>(IQueryable<TEntity> query, Expression<Func<TEntity, bool>> filter)
        where TEntity : class where TVm : class => Execute(() =>
        {
            ArgumentNullException.ThrowIfNull(filter);
            var entity = query.FirstOrDefault(filter);
            return entity is null ? null : Mapper.Map<TVm>(entity);
        });

    protected IEnumerable<TVm> ReadMany<TEntity, TVm>(IQueryable<TEntity> query, Expression<Func<TEntity, bool>>? filter)
        where TEntity : class => Execute(() =>
        {
            if (filter is not null) query = query.Where(filter);
            return (IEnumerable<TVm>)Mapper.Map<List<TVm>>(query.ToList());
        });
}
