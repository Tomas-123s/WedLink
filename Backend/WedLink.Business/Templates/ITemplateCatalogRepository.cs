namespace WedLink.Business.Templates;

public interface ITemplateCatalogRepository
{
    Task<IReadOnlyList<PublicTemplateData>> GetPublicTemplatesAsync(CancellationToken cancellationToken = default);

    Task<TemplatePage> SearchTemplatesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<TemplateData?> GetTemplateAsync(
        int templateId,
        CancellationToken cancellationToken = default);

    Task<TemplateData> CreateTemplateAsync(
        TemplateInput input,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<TemplateData?> UpdateTemplateAsync(
        int templateId,
        TemplateInput input,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);

    Task<bool> SetTemplateActiveAsync(
        int templateId,
        bool isActive,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeatureData>> GetFeaturesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> VersionNumberExistsAsync(
        int templateId,
        string versionNumber,
        CancellationToken cancellationToken = default);

    Task<TemplateVersionData> CreateVersionAsync(
        int templateId,
        TemplateVersionInput input,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken = default);

    Task<TemplateVersionData?> GetVersionAsync(
        int templateVersionId,
        CancellationToken cancellationToken = default);

    Task<bool> SetVersionFeaturesAsync(
        int templateVersionId,
        IReadOnlyList<TemplateFeatureInput> features,
        CancellationToken cancellationToken = default);

    Task<bool> SetVersionAddOnsAsync(
        int templateVersionId,
        IReadOnlyList<int> addOnIds,
        CancellationToken cancellationToken = default);

    Task<bool> SetVersionActiveAsync(
        int templateVersionId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AddOnData>> GetAddOnsAsync(
        CancellationToken cancellationToken = default);

    Task<AddOnData> CreateAddOnAsync(
        AddOnInput input,
        CancellationToken cancellationToken = default);

    Task<AddOnData?> UpdateAddOnAsync(
        int addOnId,
        AddOnInput input,
        CancellationToken cancellationToken = default);
}
