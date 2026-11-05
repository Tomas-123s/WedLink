using Microsoft.EntityFrameworkCore;
using WedLink.Business.CustomerSubmissions;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Storage;

public sealed class DatabaseSubmissionFileStorage(WedLinkDbContext db) : ISubmissionFileStorage
{
    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var key = $"{Guid.NewGuid():N}.bin";
        db.StoredMediaFiles.Add(new StoredMediaFile { StorageKey = key, Content = buffer.ToArray() });
        await db.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var content = await db.StoredMediaFiles.AsNoTracking()
            .Where(item => item.StorageKey == key)
            .Select(item => item.Content)
            .FirstOrDefaultAsync(cancellationToken);
        return content is null ? null : new MemoryStream(content, writable: false);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await db.StoredMediaFiles.Where(item => item.StorageKey == key).ExecuteDeleteAsync(cancellationToken);
    }
}
