namespace WedLink.Business.Templates;

public sealed class AddOnService(ITemplateCatalogRepository repository)
{
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
}
