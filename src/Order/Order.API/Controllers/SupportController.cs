using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Order.Application.Interfaces.Services;
using Shop.Contracts.Requests.Order;

namespace Order.API.Controllers;

[ApiController]
[Authorize]
[Route("api/support")]
public sealed class SupportController : ControllerBase
{
    private readonly ISupportService _supportService;

    public SupportController(ISupportService supportService)
    {
        _supportService = supportService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportTicketRequest request)
    {
        if (!TryGetUserId(out var customerId))
            return Unauthorized();

        try
        {
            var ticket = await _supportService.CreateAsync(
                customerId,
                GetUserName(customerId),
                request.OrderId,
                request.Message);
            return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Order not found." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMine()
    {
        if (!TryGetUserId(out var customerId))
            return Unauthorized();
        return Ok(await _supportService.GetByCustomerIdAsync(customerId));
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _supportService.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var ticket = User.IsInRole("Admin")
            ? await _supportService.GetByIdAsync(id)
            : await _supportService.GetByIdAsync(id, userId);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] AddSupportMessageRequest request)
    {
        if (!TryGetUserId(out var authorId))
            return Unauthorized();

        var isAdmin = User.IsInRole("Admin");
        try
        {
            var ticket = await _supportService.AddMessageAsync(
                id,
                authorId,
                GetUserName(authorId),
                isAdmin,
                request.Message);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost("{id:guid}/claim")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Claim(Guid id)
    {
        if (!TryGetUserId(out var adminId))
            return Unauthorized();

        try
        {
            var ticket = await _supportService.AssignAsync(id, adminId);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Resolve(Guid id)
    {
        if (!TryGetUserId(out var adminId))
            return Unauthorized();

        try
        {
            var ticket = await _supportService.ResolveAsync(id, adminId);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId)
            && userId != Guid.Empty;
    }

    private string GetUserName(Guid userId)
    {
        var userName = User.FindFirstValue(ClaimTypes.Name)?.Trim();
        return string.IsNullOrEmpty(userName)
            || userName.Equals("undefined", StringComparison.OrdinalIgnoreCase)
            || userName.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? userId.ToString()
                : userName;
    }
}
