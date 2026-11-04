using WedLink.Business.Templates;

namespace WedLink.API.Contracts.Templates;

public sealed record PublicTemplateFeatureResponse(string Key, string Name, string? Description,
    int? MaximumCount, int? MaximumFileSizeMb);
public sealed record PublicTemplateResponse(int TemplateId, string Name, string? Description,
    string? PreviewImageUrl, string VersionNumber, IReadOnlyList<PublicTemplateFeatureResponse> Features)
{
    public static PublicTemplateResponse FromData(PublicTemplateData data) => new(data.TemplateId, data.Name,
        data.Description, SafeImageUrl(data.PreviewImageReference), data.VersionNumber,
        data.Features.Select(f => new PublicTemplateFeatureResponse(f.Key, f.Name, f.Description, f.MaximumCount, f.MaximumFileSizeMb)).ToArray());

    private static string? SafeImageUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.StartsWith('/') && !value.StartsWith("//", StringComparison.Ordinal) && !value.Contains('\\')
            && !value.Contains("..", StringComparison.Ordinal) && !value.Contains('?') && !value.Contains('#')) return value;
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
                ? uri.ToString() : null;
    }
}
