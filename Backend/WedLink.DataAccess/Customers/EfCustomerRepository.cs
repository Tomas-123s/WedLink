using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WedLink.Business.Customers;
using WedLink.DataAccess.Persistence;

namespace WedLink.DataAccess.Customers;

public sealed class EfCustomerRepository(WedLinkDbContext dbContext)
    : ICustomerRepository
{
    public async Task<CustomerPage> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(customer =>
                customer.Name.Contains(search) || customer.Phone.Contains(search));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var customers = await query
            .OrderBy(customer => customer.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(customer => ToData(customer))
            .ToListAsync(cancellationToken);

        return new CustomerPage(customers, page, pageSize, totalItems);
    }

    public async Task<CustomerData?> GetByIdAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                customer => customer.CustomerId == customerId,
                cancellationToken);

        return customer is null ? null : ToData(customer);
    }

    public Task<bool> PhoneExistsAsync(
        string phone,
        int? exceptCustomerId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Customers.AnyAsync(
            customer => customer.Phone == phone
                && (!exceptCustomerId.HasValue
                    || customer.CustomerId != exceptCustomerId.Value),
            cancellationToken);
    }

    public async Task<CustomerData> CreateAsync(
        CustomerData data,
        CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Name = data.Name,
            Phone = data.Phone,
            Email = data.Email,
            Notes = data.Notes,
            CreatedAt = data.CreatedAt
        };

        dbContext.Customers.Add(customer);
        await SaveChangesAsync(cancellationToken);
        return ToData(customer);
    }

    public async Task<CustomerData?> UpdateAsync(
        int customerId,
        CustomerInput input,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(
            customer => customer.CustomerId == customerId,
            cancellationToken);

        if (customer is null)
        {
            return null;
        }

        customer.Name = input.Name;
        customer.Phone = input.Phone;
        customer.Email = input.Email;
        customer.Notes = input.Notes;
        customer.UpdatedAt = updatedAt;

        await SaveChangesAsync(cancellationToken);
        return ToData(customer);
    }

    public async Task<bool> SoftDeleteAsync(
        int customerId,
        DateTimeOffset deletedAt,
        CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers.FirstOrDefaultAsync(
            customer => customer.CustomerId == customerId,
            cancellationToken);

        if (customer is null)
        {
            return false;
        }

        customer.DeletedAt = deletedAt;
        customer.UpdatedAt = deletedAt;
        await SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is SqlException sqlException
                && sqlException.Number is 2601 or 2627)
        {
            throw new DuplicateCustomerPhoneException();
        }
    }

    private static CustomerData ToData(Customer customer)
    {
        return new CustomerData(
            customer.CustomerId,
            customer.Name,
            customer.Phone,
            customer.Email,
            customer.Notes,
            customer.CreatedAt,
            customer.UpdatedAt);
    }
}
