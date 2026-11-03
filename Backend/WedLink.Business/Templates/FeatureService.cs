namespace WedLink.Business.Templates;

public sealed class FeatureService(ITemplateCatalogRepository repository)
{
    public Task<IReadOnlyList<FeatureData>> GetFeaturesAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.GetFeaturesAsync(cancellationToken);
    }
}
