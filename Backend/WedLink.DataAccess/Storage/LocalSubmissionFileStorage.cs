using WedLink.Business.CustomerSubmissions;

namespace WedLink.DataAccess.Storage;

public sealed class LocalSubmissionFileStorage : ISubmissionFileStorage
{
    private readonly string _root;

    public LocalSubmissionFileStorage(string contentRoot, string? configuredPath)
    {
        var path = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(contentRoot, "PrivateMedia")
            : Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(contentRoot, configuredPath);
        _root = Path.GetFullPath(path);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var key = $"{Guid.NewGuid():N}.bin";
        var path = Resolve(key);
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(output, cancellationToken);
        return key;
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(key);
        Stream? stream = File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        if (Path.GetFileName(key) != key || !key.EndsWith(".bin", StringComparison.Ordinal)) throw new InvalidOperationException("Invalid storage key.");
        var path = Path.GetFullPath(Path.Combine(_root, key));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid storage path.");
        return path;
    }
}
