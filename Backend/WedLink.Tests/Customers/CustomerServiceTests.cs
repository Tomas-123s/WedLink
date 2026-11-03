using WedLink.Business.Customers;

namespace WedLink.Tests.Customers;

public sealed class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_RemovesSpacesFromPhone()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository);

        var customer = await service.CreateAsync(
            new CustomerInput("  Sara  ", "010 12 34 56", "sara@example.com", null));

        Assert.Equal("Sara", customer.Name);
        Assert.Equal("010123456", customer.Phone);
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicatePhone()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository);

        await service.CreateAsync(new CustomerInput("Sara", "010123", null, null));

        await Assert.ThrowsAsync<DuplicateCustomerPhoneException>(() =>
            service.CreateAsync(new CustomerInput("Mona", "010 123", null, null)));
    }

    [Fact]
    public async Task UpdateAsync_AllowsKeepingSamePhone()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository);
        var created = await service.CreateAsync(
            new CustomerInput("Sara", "010123", null, null));

        var updated = await service.UpdateAsync(
            created.CustomerId,
            new CustomerInput("Sara Ali", "010 123", null, null));

        Assert.Equal("Sara Ali", updated.Name);
        Assert.Equal("010123", updated.Phone);
    }

    [Fact]
    public async Task SearchAsync_RejectsPageSizeOverOneHundred()
    {
        var service = new CustomerService(new FakeCustomerRepository());

        await Assert.ThrowsAsync<CustomerValidationException>(() =>
            service.SearchAsync(null, page: 1, pageSize: 101));
    }

    [Fact]
    public async Task DeleteAsync_HidesCustomerFromSearch()
    {
        var repository = new FakeCustomerRepository();
        var service = new CustomerService(repository);
        var created = await service.CreateAsync(
            new CustomerInput("Sara", "010123", null, null));

        await service.DeleteAsync(created.CustomerId);
        var page = await service.SearchAsync(null, page: 1, pageSize: 10);

        Assert.Empty(page.Items);
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly List<CustomerData> _customers = [];
        private int _nextId = 1;

        public Task<CustomerPage> SearchAsync(
            string? search,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var results = _customers.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(search))
            {
                results = results.Where(customer =>
                    customer.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || customer.Phone.Contains(search));
            }

            var allCustomers = results.ToList();
            var items = allCustomers
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult(new CustomerPage(
                items, page, pageSize, allCustomers.Count));
        }

        public Task<CustomerData?> GetByIdAsync(
            int customerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_customers.FirstOrDefault(
                customer => customer.CustomerId == customerId));
        }

        public Task<bool> PhoneExistsAsync(
            string phone,
            int? exceptCustomerId = null,
            CancellationToken cancellationToken = default)
        {
            var exists = _customers.Any(customer =>
                customer.Phone == phone && customer.CustomerId != exceptCustomerId);
            return Task.FromResult(exists);
        }

        public Task<CustomerData> CreateAsync(
            CustomerData customer,
            CancellationToken cancellationToken = default)
        {
            var saved = customer with { CustomerId = _nextId++ };
            _customers.Add(saved);
            return Task.FromResult(saved);
        }

        public Task<CustomerData?> UpdateAsync(
            int customerId,
            CustomerInput input,
            DateTimeOffset updatedAt,
            CancellationToken cancellationToken = default)
        {
            var index = _customers.FindIndex(customer => customer.CustomerId == customerId);
            if (index < 0)
            {
                return Task.FromResult<CustomerData?>(null);
            }

            var customer = _customers[index] with
            {
                Name = input.Name,
                Phone = input.Phone,
                Email = input.Email,
                Notes = input.Notes,
                UpdatedAt = updatedAt
            };
            _customers[index] = customer;
            return Task.FromResult<CustomerData?>(customer);
        }

        public Task<bool> SoftDeleteAsync(
            int customerId,
            DateTimeOffset deletedAt,
            CancellationToken cancellationToken = default)
        {
            var index = _customers.FindIndex(customer => customer.CustomerId == customerId);
            if (index < 0)
            {
                return Task.FromResult(false);
            }

            _customers.RemoveAt(index);
            return Task.FromResult(true);
        }
    }
}
