using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Product.Application.Commands;
using Product.Application.Interfaces.Services;
using Shop.Contracts.Requests.Product;

namespace Product.API.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryReservationsController : ControllerBase
{
    private const string ApiKeyHeader = "X-Service-Api-Key";
    private readonly IInventoryReservationService _reservationService;
    private readonly IConfiguration _configuration;

    public InventoryReservationsController(
        IInventoryReservationService reservationService,
        IConfiguration configuration)
    {
        _reservationService = reservationService;
        _configuration = configuration;
    }

    [HttpPost("reservations")]
    public async Task<IActionResult> Reserve([FromBody] ReserveStockRequest request, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            return Unauthorized();

        try
        {
            await _reservationService.ReserveAsync(new ReserveStockCommand
            {
                OrderId = request.OrderId,
                Items = request.Items.Select(item => new ReserveStockItemCommand
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList()
            }, cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPost("reservations/{orderId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid orderId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            return Unauthorized();

        try
        {
            await _reservationService.ConfirmAsync(orderId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost("reservations/{orderId:guid}/release")]
    public async Task<IActionResult> Release(Guid orderId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            return Unauthorized();

        await _reservationService.ReleaseAsync(orderId, cancellationToken);
        return NoContent();
    }

    [HttpPost("reservations/{orderId:guid}/expire")]
    public async Task<IActionResult> Expire(
        Guid orderId,
        [FromQuery] DateTime cutoffUtc,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            return Unauthorized();

        try
        {
            var expired = await _reservationService.ExpireAsync(orderId, cutoffUtc, cancellationToken);
            return Ok(expired);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("reservations/{orderId:guid}/confirmed")]
    public async Task<IActionResult> IsConfirmed(Guid orderId, CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
            return Unauthorized();

        try
        {
            return Ok(await _reservationService.IsConfirmedAsync(orderId, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private bool IsAuthorized()
    {
        var expected = _configuration["ServiceAuthentication:ApiKey"];
        var provided = Request.Headers[ApiKeyHeader].ToString();
        if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(provided))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(provided));
    }
}
