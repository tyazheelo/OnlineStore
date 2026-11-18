using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Order.Application.Interfaces.Services;
using Order.API.Mappers;
using Shop.Contracts.Requests.Order;
using System;
using System.Threading.Tasks;

namespace Order.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICustomerDirectoryClient _customerDirectory;
        public OrderController(IOrderService orderService, ICustomerDirectoryClient customerDirectory)
        {
            _orderService = orderService;
            _customerDirectory = customerDirectory;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CreateOrderRequest request)
        {
            if (!TryGetCurrentUserId(out var customerId))
                return Unauthorized();

            try
            {
                var command = CreateOrderMapper.ToCommand(request, customerId);
                var order = await _orderService.CheckoutAsync(command);
                return Ok(order);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Cart or product catalog is unavailable." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            if (!TryGetCurrentUserId(out var customerId))
                return Unauthorized();

            var order = await _orderService.GetOrderByIdAsync(id, customerId);
            if (order == null)
                return NotFound();
            return Ok(order);
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            if (!TryGetCurrentUserId(out var customerId))
                return Unauthorized();

            return Ok(await _orderService.GetOrdersByCustomerIdAsync(customerId));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return await IncludeCustomerDetailsAsync(orders);
        }

        [HttpGet("status/{status}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByStatus(string status)
        {
            var orders = await _orderService.GetOrdersByStatusAsync(status);
            return await IncludeCustomerDetailsAsync(orders);
        }

        [HttpGet("date-range")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetByDateRange([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var orders = await _orderService.GetOrdersByDateRangeAsync(startDate, endDate);
            return await IncludeCustomerDetailsAsync(orders);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _orderService.UpdateOrderStatusAsync(id, request.Status);
                if (order == null)
                    return NotFound();
                return Ok(order);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (ArgumentException exception)
            {
                return BadRequest(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Inventory service is unavailable." });
            }
        }

        [HttpPut("{id}/address")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateOrderAddressRequest request)
        {
            try
            {
                var address = new Order.Domain.ValueObjects.Address(
                    request.Country.Trim(),
                    string.IsNullOrWhiteSpace(request.State) ? string.Empty : request.State.Trim(),
                    request.City.Trim(),
                    request.Street.Trim(),
                    request.PostalCode.Trim());
                var order = await _orderService.UpdateDeliveryAddressAsync(id, address);
                return order is null ? NotFound() : Ok(order);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var isAdmin = User.IsInRole("Admin");
            var customerId = Guid.Empty;
            if (!isAdmin && !TryGetCurrentUserId(out customerId))
                return Unauthorized();

            try
            {
                var order = isAdmin
                    ? await _orderService.CancelOrderAsync(id)
                    : await _orderService.CancelOrderAsync(id, customerId);
                if (order == null)
                    return NotFound();
                return Ok(order);
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Inventory service is unavailable." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _orderService.DeleteOrderAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Inventory service is unavailable." });
            }
        }

        private bool TryGetCurrentUserId(out Guid userId)
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
        }

        private async Task<IActionResult> IncludeCustomerDetailsAsync(List<Shop.Contracts.DTOs.Order.OrderDTO> orders)
        {
            try
            {
                var customers = await _customerDirectory.GetByIdsAsync(orders.Select(order => order.CustomerId).Distinct().ToArray());
                var byId = customers.ToDictionary(customer => customer.UserId);
                foreach (var order in orders)
                {
                    if (byId.TryGetValue(order.CustomerId, out var customer))
                    {
                        order.CustomerName = $"{customer.FirstName} {customer.LastName}".Trim();
                        order.CustomerEmail = customer.Email;
                    }
                }

                return Ok(orders);
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Identity service is unavailable." });
            }
        }
    }
}
