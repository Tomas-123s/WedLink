using System.ComponentModel.DataAnnotations;
using WedLink.Business.Templates;

namespace WedLink.API.Contracts.Templates;

public sealed record TemplateRequest(
    [Required, StringLength(150)] string Name,
    [StringLength(1000)] string? Description,
    [StringLength(500)] string? PreviewImageReference)
{
    public TemplateInput ToInput() => new(Name, Description, PreviewImageReference);
}

public sealed record TemplateStatusRequest(bool IsActive);

public sealed record TemplateVersionRequest(
    [Required, StringLength(30)] string VersionNumber,
    [Required, StringLength(150)] string PackageKey)
{
    public TemplateVersionInput ToInput() => new(VersionNumber, PackageKey);
}

public sealed record TemplateFeatureRequest(
    int FeatureId,
    bool IsEnabled,
    int? MaximumCount,
    int? MaximumFileSizeMb)
{
    public TemplateFeatureInput ToInput() => new(
        FeatureId, IsEnabled, MaximumCount, MaximumFileSizeMb);
}

public sealed record TemplateAddOnsRequest(IReadOnlyList<int> AddOnIds);

public sealed record AddOnRequest(
    [Required, StringLength(150)] string Name,
    [StringLength(1000)] string? Description,
    [Range(typeof(decimal), "0", "9999999999999999.99")] decimal CurrentPrice,
    bool IsActive)
{
    public AddOnInput ToInput() => new(Name, Description, CurrentPrice, IsActive);
}

public sealed record AddOnResponse(
    int AddOnId,
    string Name,
    string? Description,
    decimal CurrentPrice,
    string Currency,
    bool IsActive)
{
    public static AddOnResponse FromData(AddOnData item) => new(
        item.AddOnId,
        item.Name,
        item.Description,
        item.CurrentPrice,
        item.Currency,
        item.IsActive);
}

public sealed record FeatureResponse(
    int FeatureId,
    string Key,
    string Name,
    string? Description)
{
    public static FeatureResponse FromData(FeatureData item) => new(
        item.FeatureId,
        item.Key,
        item.Name,
        item.Description);
}

public sealed record TemplateFeatureResponse(
    int FeatureId,
    string Key,
    string Name,
    bool IsEnabled,
    int? MaximumCount,
    int? MaximumFileSizeMb)
{
    public static TemplateFeatureResponse FromData(TemplateFeatureData item) => new(
        item.FeatureId,
        item.Key,
        item.Name,
        item.IsEnabled,
        item.MaximumCount,
        item.MaximumFileSizeMb);
}

public sealed record TemplateVersionResponse(
    int TemplateVersionId,
    int TemplateId,
    string VersionNumber,
    string PackageKey,
    bool IsActive,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TemplateFeatureResponse> Features,
    IReadOnlyList<AddOnResponse> AddOns)
{
    public static TemplateVersionResponse FromData(TemplateVersionData item) => new(
        item.TemplateVersionId,
        item.TemplateId,
        item.VersionNumber,
        item.PackageKey,
        item.IsActive,
        item.CreatedAt,
        item.Features.Select(TemplateFeatureResponse.FromData).ToList(),
        item.AddOns.Select(AddOnResponse.FromData).ToList());
}

public sealed record TemplateResponse(
    int TemplateId,
    string Name,
    string? Description,
    string? PreviewImageReference,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<TemplateVersionResponse> Versions)
{
    public static TemplateResponse FromData(TemplateData item) => new(
        item.TemplateId,
        item.Name,
        item.Description,
        item.PreviewImageReference,
        item.IsActive,
        item.CreatedAt,
        item.UpdatedAt,
        item.Versions.Select(TemplateVersionResponse.FromData).ToList());
}

public sealed record TemplatePageResponse(
    IReadOnlyList<TemplateResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static TemplatePageResponse FromData(TemplatePage page) => new(
        page.Items.Select(TemplateResponse.FromData).ToList(),
        page.Page,
        page.PageSize,
        page.TotalItems,
        (int)Math.Ceiling((double)page.TotalItems / page.PageSize));
}
