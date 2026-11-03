namespace WedLink.Business.Templates;

public sealed class TemplateVersionService(ITemplateCatalogRepository repository)
{
    public async Task<TemplateVersionData> CreateVersionAsync(
        int templateId,
        TemplateVersionInput input,
        CancellationToken cancellationToken = default)
    {
        var versionNumber = input.VersionNumber.Trim();
        var packageKey = input.PackageKey.Trim();

        if (versionNumber.Length == 0 || versionNumber.Length > 30)
        {
            throw new TemplateValidationException("رقم إصدار القالب مطلوب وبحد أقصى 30 حرفًا.");
        }

        if (packageKey.Length == 0 || packageKey.Length > 150)
        {
            throw new TemplateValidationException("مفتاح حزمة القالب مطلوب وبحد أقصى 150 حرفًا.");
        }

        var template = await repository.GetTemplateAsync(templateId, cancellationToken);
        if (template is null)
        {
            throw new TemplateNotFoundException();
        }

        if (await repository.VersionNumberExistsAsync(
                templateId, versionNumber, cancellationToken))
        {
            throw new TemplateConflictException("رقم الإصدار مستخدم لهذا القالب بالفعل.");
        }

        return await repository.CreateVersionAsync(
            templateId,
            new TemplateVersionInput(versionNumber, packageKey),
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    public async Task<TemplateVersionData> GetVersionAsync(
        int templateVersionId,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetVersionAsync(templateVersionId, cancellationToken)
            ?? throw new TemplateNotFoundException();
    }

    public async Task SetVersionFeaturesAsync(
        int templateVersionId,
        IReadOnlyList<TemplateFeatureInput> features,
        CancellationToken cancellationToken = default)
    {
        ValidateUniqueIds(features.Select(feature => feature.FeatureId));

        foreach (var feature in features)
        {
            if (feature.FeatureId < 1
                || feature.MaximumCount < 0
                || feature.MaximumFileSizeMb < 1)
            {
                throw new TemplateValidationException("حدود الميزة يجب أن تكون أرقامًا موجبة.");
            }
        }

        var knownFeatures = await repository.GetFeaturesAsync(cancellationToken);
        var knownIds = knownFeatures.Select(feature => feature.FeatureId).ToHashSet();
        if (features.Any(feature => !knownIds.Contains(feature.FeatureId)))
        {
            throw new TemplateValidationException("تتضمن القائمة ميزة غير معروفة.");
        }

        if (!await repository.SetVersionFeaturesAsync(
                templateVersionId, features, cancellationToken))
        {
            throw new TemplateNotFoundException();
        }
    }

    public async Task SetVersionAddOnsAsync(
        int templateVersionId,
        IReadOnlyList<int> addOnIds,
        CancellationToken cancellationToken = default)
    {
        ValidateUniqueIds(addOnIds);

        var activeIds = (await repository.GetAddOnsAsync(cancellationToken))
            .Where(addOn => addOn.IsActive)
            .Select(addOn => addOn.AddOnId)
            .ToHashSet();

        if (addOnIds.Any(addOnId => !activeIds.Contains(addOnId)))
        {
            throw new TemplateValidationException("اختر إضافات موجودة ونشطة فقط.");
        }

        if (!await repository.SetVersionAddOnsAsync(
                templateVersionId, addOnIds, cancellationToken))
        {
            throw new TemplateNotFoundException();
        }
    }

    public async Task SetVersionActiveAsync(
        int templateVersionId,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.SetVersionActiveAsync(templateVersionId, cancellationToken))
        {
            throw new TemplateNotFoundException();
        }
    }

    private static void ValidateUniqueIds(IEnumerable<int> ids)
    {
        var items = ids.ToList();
        if (items.Any(id => id < 1) || items.Distinct().Count() != items.Count)
        {
            throw new TemplateValidationException("القائمة تحتوي معرفات مكررة أو غير صالحة.");
        }
    }
}
