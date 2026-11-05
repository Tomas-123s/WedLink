namespace WedLink.DataAccess.Storage;

public sealed class StoredMediaFile
{
    public string StorageKey { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
}
