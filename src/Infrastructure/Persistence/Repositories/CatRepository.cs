using Application.Abstractions;
using Domain.Cats;
using Domain.Tags;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Infrastructure.Persistence.Repositories;

[ExcludeFromCodeCoverage]
internal sealed class CatRepository : ICatRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CatRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IEnumerable<Cat> Cats, int Total)> GetAllAsync(
        string? tag, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken)
    {
        var query = _dbContext.Cats.AsQueryable();

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(c => c.Tags.Any(t => t.Name == tag));
        }

        var total = await query.CountAsync(cancellationToken);

        var cats = await query
            .OrderBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.Tags)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (cats, total);
    }

    public async Task<Cat?> GetAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Cats
            .Include(c => c.Tags)
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<HashSet<string>> GetExistingCatIdsAsync(
        IEnumerable<string> catIds,
        CancellationToken cancellationToken)
    {
        var ids = catIds.ToList();
        var existing = await _dbContext.Cats
            .Where(c => ids.Contains(c.CatId))
            .Select(c => c.CatId)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet();
    }

    public async Task<Dictionary<string, Tag>> GetOrCreateTagsAsync(
        IEnumerable<string> tagNames,
        CancellationToken cancellationToken)
    {
        var normalised = tagNames
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct()
            .ToList();

        var existingTags = await _dbContext.Tags
            .Where(t => normalised.Contains(t.Name))
            .ToListAsync(cancellationToken);

        var result = existingTags.ToDictionary(t => t.Name, t => t);
        var newTagNames = normalised.Where(n => !result.ContainsKey(n)).ToList();

        if (newTagNames.Count == 0)
            return result;

        foreach (var name in newTagNames)
        {
            var tag = new Tag
            {
                Name = name,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Tags.Add(tag);
            result[name] = tag;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A concurrent job already inserted one or more of these tags.
            // Detach unsaved tags, re-query everything from the DB.
            foreach (var name in newTagNames)
            {
                if (result.TryGetValue(name, out var unsaved))
                {
                    _dbContext.Entry(unsaved).State = EntityState.Detached;
                }
            }

            var freshTags = await _dbContext.Tags
                .Where(t => normalised.Contains(t.Name))
                .ToListAsync(cancellationToken);

            result = freshTags.ToDictionary(t => t.Name, t => t);
        }

        return result;
    }

    public async Task AddCatAsync(Cat cat, CancellationToken cancellationToken)
    {
        _dbContext.Cats.Add(cat);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public void DetachEntity(object entity)
    {
        _dbContext.Entry(entity).State = EntityState.Detached;
    }
}
