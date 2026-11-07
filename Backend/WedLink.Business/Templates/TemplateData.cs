namespace WedLink.Business.Templates;

public sealed record TemplateData(
    int TemplateId,
    string Name,
    string? Description,
    string? PreviewImageReference,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<TemplateVersionData> Versions,
    decimal CurrentPrice = 0);

public sealed record TemplateVersionData(
    int TemplateVersionId,
    int TemplateId,
    string VersionNumber,
    string PackageKey,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TemplateFeatureData> Features,
    IReadOnlyList<AddOnData> AddOns);

public sealed record FeatureData(int FeatureId, string Key, string Name, string? Description);

public sealed record TemplateFeatureData(
    int FeatureId,
    string Key,
    string Name,
    bool IsEnabled,
    int? MaximumCount,
    int? MaximumFileSizeMb);

public sealed record AddOnData(
    int AddOnId,
    string Name,
    string? Description,
    decimal CurrentPrice,
    string Currency,
    bool IsActive);

public sealed record TemplateInput(
    string Name,
    string? Description,
    string? PreviewImageReference,
    decimal CurrentPrice = 0);

public sealed record TemplateVersionInput(string VersionNumber, string PackageKey);

public sealed record TemplateFeatureInput(
    int FeatureId,
    bool IsEnabled,
    int? MaximumCount,
    int? MaximumFileSizeMb);

public sealed record AddOnInput(
    string Name,
    string? Description,
    decimal CurrentPrice,
    bool IsActive);

public sealed record TemplatePage(
    IReadOnlyList<TemplateData> Items,
    int Page,
    int PageSize,
    int TotalItems);

public sealed record PublicTemplateData(int TemplateId, string Name, string? Description,
    string? PreviewImageReference, string VersionNumber, IReadOnlyList<PublicTemplateFeatureData> Features,
    decimal CurrentPrice = 0);
public sealed record PublicTemplateFeatureData(string Key, string Name, string? Description, int? MaximumCount, int? MaximumFileSizeMb);

public sealed class TemplateValidationException(string message) : Exception(message)
{
}

public sealed class TemplateNotFoundException() : Exception("القالب أو الإصدار غير موجود.")
{
}

public sealed class TemplateConflictException(string message) : Exception(message)
{
}
