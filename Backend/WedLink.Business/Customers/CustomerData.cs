namespace WedLink.Business.Customers;

public sealed record CustomerData(
    int CustomerId,
    string Name,
    string Phone,
    string? Email,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record CustomerPage(
    IReadOnlyList<CustomerData> Items,
    int Page,
    int PageSize,
    int TotalItems);

public sealed record CustomerInput(
    string Name,
    string Phone,
    string? Email,
    string? Notes);

public sealed class CustomerValidationException(string message) : Exception(message)
{
}

public sealed class DuplicateCustomerPhoneException()
    : Exception("رقم الهاتف مستخدم لعميل آخر.")
{
}

public sealed class CustomerNotFoundException()
    : Exception("العميل غير موجود.")
{
}
