using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Granit.ReferenceData.EntityFrameworkCore.Internal;

/// <summary>
/// EF Core implementation of <see cref="IReferenceDataStore{TEntity}"/>.
/// Persists reference data in the host application's DbContext with built-in
/// <see cref="IMemoryCache"/> for read-heavy workloads.
/// </summary>
/// <typeparam name="TEntity">The concrete reference data entity type.</typeparam>
/// <typeparam name="TDbContext">The host application's DbContext.</typeparam>
/// <remarks>
/// Registered as Scoped. Uses <see cref="IServiceScopeFactory"/> to create
/// a dedicated scope per database operation, ensuring correct EF Core lifetime.
/// </remarks>
internal sealed class EfCoreReferenceDataStore<TEntity, TDbContext>(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache,
    IOptions<ReferenceDataOptions> options) : IReferenceDataStore<TEntity>
    where TEntity : ReferenceDataEntity
    where TDbContext : DbContext
{
    private static readonly string EntityName = typeof(TEntity).Name;
    private static string AllCacheKey => $"refdata:{EntityName}:all";
    private static string CodeCacheKey(string code) => $"refdata:{EntityName}:{code}";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IMemoryCache _cache = cache;
    private readonly ReferenceDataOptions _options = options.Value;

    /// <inheritdoc/>
    public async Task<ReferenceDataResult<TEntity>> GetAllAsync(
        ReferenceDataQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        query ??= new ReferenceDataQuery();

        IQueryable<TEntity> queryable = context.Set<TEntity>().AsNoTracking();

        // Active filter
        if (query.ActiveOnly)
        {
            queryable = queryable.Where(e => e.IsActive);
        }

        // Search filter (Code or any label, case-insensitive)
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm;
            queryable = queryable.Where(e =>
                EF.Functions.Like(e.Code, $"%{term}%") ||
                EF.Functions.Like(e.LabelEn, $"%{term}%") ||
                EF.Functions.Like(e.LabelFr, $"%{term}%") ||
                EF.Functions.Like(e.LabelNl, $"%{term}%") ||
                EF.Functions.Like(e.LabelDe, $"%{term}%") ||
                EF.Functions.Like(e.LabelEs, $"%{term}%") ||
                EF.Functions.Like(e.LabelIt, $"%{term}%") ||
                EF.Functions.Like(e.LabelPt, $"%{term}%"));
        }

        // Total count before pagination
        int totalCount = await queryable.CountAsync(cancellationToken);

        // Sorting
        queryable = query.SortBy?.ToUpperInvariant() switch
        {
            "CODE" => query.Descending
                ? queryable.OrderByDescending(e => e.Code)
                : queryable.OrderBy(e => e.Code),
            "LABEL" => query.Descending
                ? queryable.OrderByDescending(e => e.LabelEn)
                : queryable.OrderBy(e => e.LabelEn),
            _ => query.Descending
                ? queryable.OrderByDescending(e => e.SortOrder).ThenBy(e => e.Code)
                : queryable.OrderBy(e => e.SortOrder).ThenBy(e => e.Code),
        };

        // Pagination
        if (query.Skip.HasValue)
        {
            queryable = queryable.Skip(query.Skip.Value);
        }

        if (query.Take.HasValue)
        {
            queryable = queryable.Take(query.Take.Value);
        }

        List<TEntity> items = await queryable.ToListAsync(cancellationToken);

        return new ReferenceDataResult<TEntity>(items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        string cacheKey = CodeCacheKey(code);

        if (_cache.TryGetValue(cacheKey, out TEntity? cached))
        {
            return cached;
        }

        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        TEntity? entity = await context.Set<TEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Code == code, cancellationToken);

        if (entity is not null)
        {
            _cache.Set(cacheKey, entity, _options.CacheTimeToLive);
        }

        return entity;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        context.Set<TEntity>().Add(entity);
        await context.SaveChangesAsync(cancellationToken);

        InvalidateCache(entity.Code);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        context.Set<TEntity>().Update(entity);
        await context.SaveChangesAsync(cancellationToken);

        InvalidateCache(entity.Code);
    }

    /// <inheritdoc/>
    public async Task SetActiveAsync(
        string code,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        TDbContext context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        TEntity? entity = await context.Set<TEntity>()
            .FirstOrDefaultAsync(e => e.Code == code, cancellationToken);

        if (entity is not null)
        {
            entity.IsActive = isActive;
            await context.SaveChangesAsync(cancellationToken);
        }

        InvalidateCache(code);
    }

    private void InvalidateCache(string code)
    {
        _cache.Remove(CodeCacheKey(code));
        _cache.Remove(AllCacheKey);
    }
}
