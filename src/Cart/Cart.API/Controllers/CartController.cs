using System.Security.Claims;
using System.Security.Claims;
using AutoMapper;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cart.Application.Interfaces.Services;
using Cart.Application.Exceptions;
using Cart.Application.Commands;
using Shop.Contracts.DTOs.Cart;
using Shop.Contracts.Requests.Cart;

namespace Cart.API.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;
        private readonly IMapper _mapper;

        public CartController(ICartService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            if (!TryGetCustomerId(out var customerId))
                return Unauthorized();

            try
            {
                var cart = await _service.GetCurrentCartAsync(customerId);
                return Ok(_mapper.Map<CartDTO>(cart));
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Product catalog is unavailable." });
            }
            catch (CartConcurrencyException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] CartItemRequest request)
        {
            if (request == null) return BadRequest();
            if (!TryGetCustomerId(out var customerId))
                return Unauthorized();

            var command = new AddCartItemCommand
            {
                CustomerId = customerId,
                ProductId = request.ProductId,
                Quantity = request.Quantity
            };

            try
            {
                await _service.AddItemAsync(command);
                return NoContent();
            }
            catch (ValidationException exception)
            {
                return BadRequest(new { errors = exception.Errors.Select(error => error.ErrorMessage) });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Product was not found." });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Product catalog is unavailable." });
            }
            catch (CartConcurrencyException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        [HttpDelete("items/{productId:guid}")]
        public async Task<IActionResult> RemoveItem(Guid productId)
        {
            if (!TryGetCustomerId(out var customerId))
                return Unauthorized();

            try
            {
                return await _service.RemoveItemAsync(customerId, productId) ? NoContent() : NotFound();
            }
            catch (CartConcurrencyException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        [HttpPut("items/{productId:guid}")]
        public async Task<IActionResult> UpdateItemQuantity(
            Guid productId,
            [FromBody] UpdateCartItemQuantityRequest request)
        {
            if (!TryGetCustomerId(out var customerId))
                return Unauthorized();

            var command = new UpdateCartItemQuantityCommand
            {
                CustomerId = customerId,
                ProductId = productId,
                Quantity = request.Quantity
            };

            try
            {
                return await _service.UpdateItemQuantityAsync(command) ? NoContent() : NotFound();
            }
            catch (ValidationException exception)
            {
                return BadRequest(new { errors = exception.Errors.Select(error => error.ErrorMessage) });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { error = "Product was not found." });
            }
            catch (InvalidOperationException exception)
            {
                return Conflict(new { error = exception.Message });
            }
            catch (HttpRequestException)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Product catalog is unavailable." });
            }
            catch (CartConcurrencyException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> ClearCart([FromQuery] long? revision = null)
        {
            if (!TryGetCustomerId(out var customerId))
                return Unauthorized();

            try
            {
                var cleared = await _service.ClearCartAsync(customerId, revision);
                if (!cleared)
                    return Conflict(new { error = "The cart changed after checkout started; its newer contents were kept." });

                return NoContent();
            }
            catch (CartConcurrencyException exception)
            {
                return Conflict(new { error = exception.Message });
            }
        }

        private bool TryGetCustomerId(out Guid customerId)
        {
            return Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out customerId);
        }
    }
}