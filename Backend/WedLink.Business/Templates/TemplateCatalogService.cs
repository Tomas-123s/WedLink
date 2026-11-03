namespace WedLink.Business.Templates;

public sealed class TemplateCatalogService(ITemplateCatalogRepository repository)
{
    public Task<TemplatePage> SearchTemplatesAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new TemplateValidationException("بيانات تقسيم الصفحات غير صالحة.");
        }

        return repository.SearchTemplatesAsync(
            search?.Trim(), page, pageSize, cancellationToken);
    }

    public async Task<TemplateData> GetTemplateAsync(
        int templateId,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetTemplateAsync(templateId, cancellationToken)
            ?? throw new TemplateNotFoundException();
    }

    public async Task<TemplateData> CreateTemplateAsync(
        TemplateInput input,
        CancellationToken cancellationToken = default)
    {
        var cleanInput = CleanTemplate(input);
        return await repository.CreateTemplateAsync(
            cleanInput, DateTimeOffset.UtcNow, cancellationToken);
    }

    public async Task<TemplateData> UpdateTemplateAsync(
        int templateId,
        TemplateInput input,
        CancellationToken cancellationToken = default)
    {
        var cleanInput = CleanTemplate(input);
        return await repository.UpdateTemplateAsync(
                   templateId,
                   cleanInput,
                   DateTimeOffset.UtcNow,
                   cancellationToken)
               ?? throw new TemplateNotFoundException();
    }

    public async Task SetTemplateActiveAsync(
        int templateId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.SetTemplateActiveAsync(
                templateId, isActive, DateTimeOffset.UtcNow, cancellationToken))
        {
            throw new TemplateNotFoundException();
        }
    }

    public Task<IReadOnlyList<FeatureData>> GetFeaturesAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.GetFeaturesAsync(cancellationToken);
    }

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

    public Task<IReadOnlyList<AddOnData>> GetAddOnsAsync(
        CancellationToken cancellationToken = default)
    {
        return repository.GetAddOnsAsync(cancellationToken);
    }

    public Task<AddOnData> CreateAddOnAsync(
        AddOnInput input,
        CancellationToken cancellationToken = default)
    {
        return repository.CreateAddOnAsync(CleanAddOn(input), cancellationToken);
    }

    public async Task<AddOnData> UpdateAddOnAsync(
        int addOnId,
        AddOnInput input,
        CancellationToken cancellationToken = default)
    {
        return await repository.UpdateAddOnAsync(
                   addOnId, CleanAddOn(input), cancellationToken)
               ?? throw new TemplateNotFoundException();
    }

    private static TemplateInput CleanTemplate(TemplateInput input)
    {
        var name = input.Name.Trim();
        var description = CleanOptional(input.Description);
        var previewImage = CleanOptional(input.PreviewImageReference);

        if (name.Length == 0 || name.Length > 150)
        {
            throw new TemplateValidationException("اسم القالب مطلوب وبحد أقصى 150 حرفًا.");
        }

        if (description?.Length > 1000 || previewImage?.Length > 500)
        {
            throw new TemplateValidationException("أحد حقول القالب أطول من الحد المسموح.");
        }

        return new TemplateInput(name, description, previewImage);
    }

    private static AddOnInput CleanAddOn(AddOnInput input)
    {
        var name = input.Name.Trim();
        var description = CleanOptional(input.Description);

        if (name.Length == 0 || name.Length > 150)
        {
            throw new TemplateValidationException("اسم الإضافة مطلوب وبحد أقصى 150 حرفًا.");
        }

        if (description?.Length > 1000)
        {
            throw new TemplateValidationException("وصف الإضافة يجب ألا يتجاوز 1000 حرف.");
        }

        if (input.CurrentPrice < 0 || decimal.Round(input.CurrentPrice, 2) != input.CurrentPrice)
        {
            throw new TemplateValidationException("السعر يجب أن يكون موجبًا وبحد أقصى منزلتين عشريتين.");
        }

        return input with { Name = name, Description = description };
    }

    private static string? CleanOptional(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
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
