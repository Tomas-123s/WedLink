using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WedLink.API.Contracts.Orders;
using WedLink.Business.Orders;

namespace WedLink.API.Controllers;

[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/v1/admin/orders")]
public sealed class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await RunOrderAction(async () =>
        {
            var result = await orderService.SearchAsync(
                search, status, customerId, page, pageSize, cancellationToken);
            return Ok(OrderPageResponse.FromData(result));
        });
    }

    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> GetById(
        int orderId,
        CancellationToken cancellationToken)
    {
        return await RunOrderAction(async () =>
        {
            var order = await orderService.GetByIdAsync(orderId, cancellationToken);
            return Ok(OrderDetailResponse.FromData(order));
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        return await RunOrderAction(async () =>
        {
            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = await orderService.CreateAsync(
                request.ToInput(), actorId, cancellationToken);
            var response = OrderDetailResponse.FromData(order);
            return CreatedAtAction(nameof(GetById), new { orderId = order.OrderId }, response);
        });
    }

    [HttpPatch("{orderId:int}/status")]
    public async Task<IActionResult> ChangeStatus(
        int orderId,
        ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        return await RunOrderAction(async () =>
        {
            var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var order = await orderService.ChangeStatusAsync(
                orderId, request.ToInput(), actorId, cancellationToken);
            return Ok(OrderDetailResponse.FromData(order));
        });
    }

    private async Task<IActionResult> RunOrderAction(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (OrderValidationException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid order request",
                detail: exception.Message);
        }
        catch (OrderNotFoundException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Order not found",
                detail: exception.Message);
        }
        catch (OrderStatusConflictException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Order status conflict",
                detail: exception.Message);
        }
    }
}
