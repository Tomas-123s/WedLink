namespace WedLink.DataAccess.Guests;

public class MusicTrack
{
    public int MusicTrackId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Artist { get; set; }
    public string RightsReference { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
