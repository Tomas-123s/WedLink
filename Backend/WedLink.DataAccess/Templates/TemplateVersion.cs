namespace WedLink.DataAccess.Templates;

public class TemplateVersion
{
    public int TemplateVersionId { get; set; }

    public int TemplateId { get; set; }

    public string VersionNumber { get; set; } = string.Empty;

    public string PackageKey { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
