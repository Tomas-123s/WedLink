namespace WedLink.Business.Customers;

public interface ICustomerRepository
{
    Task<CustomerPage> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<CustomerData?> GetByIdAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    Task<bool> PhoneExistsAsync(
        string phone,
        int? exceptCustomerId = null,
        CancellationToken cancellationToken = default);

    Task<CustomerData> CreateAsync(
        CustomerData customer,
        CancellationToken cancellationToken = default);

    Task<CustomerData?> UpdateAsync(
        int customerId,
        CustomerInput input,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(
        int customerId,
        DateTimeOffset deletedAt,
        CancellationToken cancellationToken = default);
}
