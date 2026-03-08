using Domain.Cats;
using Domain.Tags;

namespace Application.Abstractions;

public interface ICatRepository
{
    Task<Cat?> GetAsync(int id, CancellationToken cancellationToken);
    Task<(IEnumerable<Cat> Cats, int Total)> GetAllAsync(string? tag, int page, int pageSize, CancellationToken cancellationToken);
    Task<HashSet<string>> GetExistingCatIdsAsync(IEnumerable<string> catIds, CancellationToken cancellationToken);
    Task<Dictionary<string, Tag>> GetOrCreateTagsAsync(IEnumerable<string> tagNames, CancellationToken cancellationToken);
    Task AddCatAsync(Cat cat, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    void DetachEntity(object entity);
}
