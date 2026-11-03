using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Customers;
using WedLink.Business.Customers;

namespace WedLink.API.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/customers")]
public sealed class CustomersController(CustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await RunCustomerAction(async () =>
        {
            var result = await customerService.SearchAsync(
                search, page, pageSize, cancellationToken);
            return Ok(CustomerPageResponse.FromData(result));
        });
    }

    [HttpGet("{customerId:int}")]
    public async Task<IActionResult> GetById(
        int customerId,
        CancellationToken cancellationToken)
    {
        return await RunCustomerAction(async () =>
        {
            var customer = await customerService.GetByIdAsync(
                customerId, cancellationToken);
            return Ok(CustomerResponse.FromData(customer));
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CustomerRequest request,
        CancellationToken cancellationToken)
    {
        return await RunCustomerAction(async () =>
        {
            var customer = await customerService.CreateAsync(
                request.ToInput(), cancellationToken);
            var response = CustomerResponse.FromData(customer);

            return CreatedAtAction(
                nameof(GetById),
                new { customerId = response.CustomerId },
                response);
        });
    }

    [HttpPut("{customerId:int}")]
    public async Task<IActionResult> Update(
        int customerId,
        CustomerRequest request,
        CancellationToken cancellationToken)
    {
        return await RunCustomerAction(async () =>
        {
            var customer = await customerService.UpdateAsync(
                customerId, request.ToInput(), cancellationToken);
            return Ok(CustomerResponse.FromData(customer));
        });
    }

    [HttpDelete("{customerId:int}")]
    public async Task<IActionResult> Delete(
        int customerId,
        CancellationToken cancellationToken)
    {
        return await RunCustomerAction(async () =>
        {
            await customerService.DeleteAsync(customerId, cancellationToken);
            return NoContent();
        });
    }

    private async Task<IActionResult> RunCustomerAction(
        Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (CustomerValidationException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "بيانات العميل غير صالحة",
                detail: exception.Message);
        }
        catch (DuplicateCustomerPhoneException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "رقم الهاتف مسجل بالفعل",
                detail: exception.Message);
        }
        catch (CustomerNotFoundException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "العميل غير موجود",
                detail: exception.Message);
        }
    }
}
