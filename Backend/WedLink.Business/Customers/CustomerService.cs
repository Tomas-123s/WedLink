namespace WedLink.Business.Customers;

public sealed class CustomerService(ICustomerRepository repository)
{
    public async Task<CustomerPage> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new CustomerValidationException("رقم الصفحة يجب أن يكون 1 أو أكبر.");
        }

        if (pageSize is < 1 or > 100)
        {
            throw new CustomerValidationException("حجم الصفحة يجب أن يكون بين 1 و100.");
        }

        return await repository.SearchAsync(
            search?.Trim(), page, pageSize, cancellationToken);
    }

    public async Task<CustomerData> GetByIdAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        return await repository.GetByIdAsync(customerId, cancellationToken)
            ?? throw new CustomerNotFoundException();
    }

    public async Task<CustomerData> CreateAsync(
        CustomerInput input,
        CancellationToken cancellationToken = default)
    {
        var cleanInput = ValidateAndClean(input);

        if (await repository.PhoneExistsAsync(
                cleanInput.Phone, cancellationToken: cancellationToken))
        {
            throw new DuplicateCustomerPhoneException();
        }

        var customer = new CustomerData(
            0,
            cleanInput.Name,
            cleanInput.Phone,
            cleanInput.Email,
            cleanInput.Notes,
            DateTimeOffset.UtcNow,
            null);

        return await repository.CreateAsync(customer, cancellationToken);
    }

    public async Task<CustomerData> UpdateAsync(
        int customerId,
        CustomerInput input,
        CancellationToken cancellationToken = default)
    {
        var cleanInput = ValidateAndClean(input);

        if (await repository.PhoneExistsAsync(
                cleanInput.Phone, customerId, cancellationToken))
        {
            throw new DuplicateCustomerPhoneException();
        }

        return await repository.UpdateAsync(
                   customerId,
                   cleanInput,
                   DateTimeOffset.UtcNow,
                   cancellationToken)
               ?? throw new CustomerNotFoundException();
    }

    public async Task DeleteAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        if (!await repository.SoftDeleteAsync(
                customerId, DateTimeOffset.UtcNow, cancellationToken))
        {
            throw new CustomerNotFoundException();
        }
    }

    private static CustomerInput ValidateAndClean(CustomerInput input)
    {
        var name = input.Name.Trim();
        var phone = input.Phone.Trim().Replace(" ", string.Empty);
        var email = CleanOptional(input.Email);
        var notes = CleanOptional(input.Notes);

        if (name.Length == 0 || name.Length > 150)
        {
            throw new CustomerValidationException("الاسم مطلوب ويجب ألا يتجاوز 150 حرفًا.");
        }

        if (phone.Length == 0 || phone.Length > 30)
        {
            throw new CustomerValidationException("رقم الهاتف مطلوب ويجب ألا يتجاوز 30 حرفًا.");
        }

        if (email?.Length > 254)
        {
            throw new CustomerValidationException("البريد الإلكتروني طويل جدًا.");
        }

        if (notes?.Length > 1000)
        {
            throw new CustomerValidationException("الملاحظات يجب ألا تتجاوز 1000 حرف.");
        }

        return new CustomerInput(name, phone, email, notes);
    }

    private static string? CleanOptional(string? value)
    {
        var cleaned = value?.Trim();
        return string.IsNullOrEmpty(cleaned) ? null : cleaned;
    }
}
