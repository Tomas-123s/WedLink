namespace WedLink.DataAccess.Templates;

public class TemplateFeature
{
    public int TemplateVersionId { get; set; }

    public int FeatureId { get; set; }

    public bool IsEnabled { get; set; }

    public int? MaximumCount { get; set; }

    public int? MaximumFileSizeMb { get; set; }
}
