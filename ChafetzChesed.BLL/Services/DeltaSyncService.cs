using Microsoft.EntityFrameworkCore;

namespace ChafetzChesed.BLL.Services;

public interface IDeltaSyncService
{
    Task<DeltaResult<TDto>> GetDeltaAsync<TEntity, TDto>(
        IQueryable<TEntity> baseQuery,
        int institutionId,
        int lastId,
        int limit,
        Func<TEntity, TDto> map,
        string idPropertyName = "Id") where TEntity : class;
}

public record DeltaResult<TDto>(
    string Resource, int InstitutionId, int FromLastId, int Count, int NextLastId, bool HasMore, IEnumerable<TDto> Items);

public class DeltaSyncService : IDeltaSyncService
{
    public async Task<DeltaResult<TDto>> GetDeltaAsync<TEntity, TDto>(
        IQueryable<TEntity> baseQuery,
        int institutionId,
        int lastId,
        int limit,
        Func<TEntity, TDto> map,
        string idPropertyName = "Id")
        where TEntity : class
    {
        var query = baseQuery
            .Where(e => EF.Property<int>(e, "InstitutionId") == institutionId &&
                        EF.Property<int>(e, idPropertyName) > lastId)
            .OrderBy(e => EF.Property<int>(e, idPropertyName))
            .Take(Math.Clamp(limit, 1, 1000));

        var list = await query.ToListAsync();

        int GetId(TEntity e)
        {
            var prop = e!.GetType().GetProperty(idPropertyName)!;
            var val = prop.GetValue(e);
            return Convert.ToInt32(val);
        }

        var items = list.Select(map).ToList();
        var next = items.Count > 0 ? list.Max(GetId) : lastId;

        bool hasMore = await baseQuery.AnyAsync(e =>
            EF.Property<int>(e, "InstitutionId") == institutionId &&
            EF.Property<int>(e, idPropertyName) > next);

        return new DeltaResult<TDto>("", institutionId, lastId, items.Count, next, hasMore, items);
    }
}
