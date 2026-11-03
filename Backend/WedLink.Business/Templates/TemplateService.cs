namespace WedLink.Business.Templates;

public sealed class TemplateService(ITemplateCatalogRepository repository)
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

    private static string? CleanOptional(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }
}
