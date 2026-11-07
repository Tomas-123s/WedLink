using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using WedLink.Business.CustomerSubmissions;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Storage;

public sealed class DatabaseSubmissionFileStorage(WedLinkDbContext db) : ISubmissionFileStorage
{
    // Stored rows use this marker followed by the original bytes, or by Brotli bytes
    // when Brotli produces a smaller payload. Rows without the marker are legacy data.
    private static readonly byte[] CompressedMarker = "WLB1"u8.ToArray();

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var original = buffer.ToArray();
        await using var compressedBuffer = new MemoryStream();
        await using (var compressor = new BrotliStream(compressedBuffer, CompressionLevel.Optimal, leaveOpen: true))
            await compressor.WriteAsync(original, cancellationToken);

        byte[] stored;
        if (compressedBuffer.Length < original.Length)
        {
            stored = new byte[CompressedMarker.Length + (int)compressedBuffer.Length];
            CompressedMarker.CopyTo(stored, 0);
            compressedBuffer.GetBuffer().AsSpan(0, (int)compressedBuffer.Length).CopyTo(stored.AsSpan(CompressedMarker.Length));
        }
        else
        {
            stored = original;
        }

        var key = $"{Guid.NewGuid():N}.bin";
        db.StoredMediaFiles.Add(new StoredMediaFile { StorageKey = key, Content = stored });
        await db.SaveChangesAsync(cancellationToken);
        return key;
    }

    public async Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        var content = await db.StoredMediaFiles.AsNoTracking()
            .Where(item => item.StorageKey == key)
            .Select(item => item.Content)
            .FirstOrDefaultAsync(cancellationToken);
        if (content is null) return null;
        if (!content.AsSpan().StartsWith(CompressedMarker)) return new MemoryStream(content, writable: false);

        await using var compressed = new MemoryStream(content, CompressedMarker.Length,
            content.Length - CompressedMarker.Length, writable: false);
        await using var decompressor = new BrotliStream(compressed, CompressionMode.Decompress);
        var restored = new MemoryStream();
        await decompressor.CopyToAsync(restored, cancellationToken);
        restored.Position = 0;
        return restored;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await db.StoredMediaFiles.Where(item => item.StorageKey == key).ExecuteDeleteAsync(cancellationToken);
    }
}
