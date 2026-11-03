namespace WedLink.DataAccess.Templates;

public class Template
{
    public int TemplateId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? PreviewImageReference { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}
