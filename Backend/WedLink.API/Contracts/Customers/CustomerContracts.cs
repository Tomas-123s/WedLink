using System.ComponentModel.DataAnnotations;
using WedLink.Business.Customers;

namespace WedLink.API.Contracts.Customers;

public sealed record CustomerRequest(
    [Required, StringLength(150)] string Name,
    [Required, StringLength(30)] string Phone,
    [EmailAddress, StringLength(254)] string? Email,
    [StringLength(1000)] string? Notes)
{
    public CustomerInput ToInput()
    {
        return new CustomerInput(Name, Phone, Email, Notes);
    }
}

public sealed record CustomerResponse(
    int CustomerId,
    string Name,
    string Phone,
    string? Email,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static CustomerResponse FromData(CustomerData customer)
    {
        return new CustomerResponse(
            customer.CustomerId,
            customer.Name,
            customer.Phone,
            customer.Email,
            customer.Notes,
            customer.CreatedAt,
            customer.UpdatedAt);
    }
}

public sealed record CustomerPageResponse(
    IReadOnlyList<CustomerResponse> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static CustomerPageResponse FromData(CustomerPage page)
    {
        var totalPages = (int)Math.Ceiling((double)page.TotalItems / page.PageSize);

        return new CustomerPageResponse(
            page.Items.Select(CustomerResponse.FromData).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems,
            totalPages);
    }
}
