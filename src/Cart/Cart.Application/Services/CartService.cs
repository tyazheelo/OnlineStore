using System;
using System.Threading;
using System.Threading.Tasks;
using Cart.Application.Commands;
using Cart.Application.Exceptions;
using Cart.Application.Interfaces.Repositories;
using Cart.Application.Interfaces.Services;
using Cart.Application.Validators;
using Cart.Domain.Entities;

using CartEntity = Cart.Domain.Entities.Cart;
namespace Cart.Application.Services
{
    public class CartService : ICartService
    {
        private const int MaxConcurrencyAttempts = 3;
        private readonly ICartRepository _repo;
        private readonly IProductCatalogClient _productCatalogClient;
        private readonly FluentValidation.IValidator<AddCartItemCommand> _validator;
        private readonly FluentValidation.IValidator<UpdateCartItemQuantityCommand> _updateQuantityValidator;

        public CartService(
            ICartRepository repo,
            IProductCatalogClient productCatalogClient,
            FluentValidation.IValidator<AddCartItemCommand> validator,
            FluentValidation.IValidator<UpdateCartItemQuantityCommand> updateQuantityValidator)
        {
            _repo = repo;
            _productCatalogClient = productCatalogClient;
            _validator = validator;
            _updateQuantityValidator = updateQuantityValidator;
        }

        public async Task<CartEntity?> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            return await _repo.GetByCustomerIdAsync(customerId, cancellationToken);
        }

        public async Task<CartEntity> GetOrCreateCartForCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var cart = await _repo.GetByCustomerIdAsync(customerId, cancellationToken);
            if (cart != null) return cart;

            var newCart = CartEntity.Create(customerId);
            await _repo.AddAsync(newCart, cancellationToken);
            return newCart;
        }

        public async Task<CartEntity> GetCurrentCartAsync(Guid customerId, CancellationToken cancellationToken = default)
        {
            var cart = await GetOrCreateCartForCustomerAsync(customerId, cancellationToken);
            if (cart.Items.Count == 0)
                return cart;

            var products = await _productCatalogClient.GetByIdsAsync(
                cart.Items.Select(item => item.ProductId).ToArray(),
                cancellationToken);
            var productsById = products.ToDictionary(product => product.Id);

            foreach (var item in cart.Items)
            {
                if (!productsById.TryGetValue(item.ProductId, out var product))
                {
                    item.SetAvailability(false);
                    continue;
                }

                var isAvailable = product.IsActive && product.StockQuantity >= item.Quantity;
                item.UpdateProductSnapshot(product.Name, product.Price, isAvailable);
            }

            return cart;
        }

        public async Task AddItemAsync(AddCartItemCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _validator.Validate(command);
            if (!validationResult.IsValid)
                throw new FluentValidation.ValidationException(validationResult.Errors);

            var product = await _productCatalogClient.GetByIdAsync(command.ProductId, cancellationToken);
            if (product is null)
                throw new KeyNotFoundException($"Product {command.ProductId} was not found.");

            if (!product.IsActive)
                throw new InvalidOperationException("The product is not available.");

            for (var attempt = 0; ; attempt++)
            {
                var cart = await _repo.GetByCustomerIdAsync(command.CustomerId, cancellationToken);
                var existingQuantity = cart?.Items
                    .FirstOrDefault(item => item.ProductId == command.ProductId)?.Quantity ?? 0;
                if (product.StockQuantity < existingQuantity + command.Quantity)
                    throw new InvalidOperationException("There is not enough stock for the requested quantity.");

                cart ??= await GetOrCreateCartForCustomerAsync(command.CustomerId, cancellationToken);
                cart.AddItem(product.Id, product.Name, product.Price, command.Quantity);
                try
                {
                    await _repo.UpdateAsync(cart, cancellationToken);
                    return;
                }
                catch (CartConcurrencyException) when (attempt < MaxConcurrencyAttempts - 1)
                {
                }
            }
        }

        public async Task<bool> UpdateItemQuantityAsync(
            UpdateCartItemQuantityCommand command,
            CancellationToken cancellationToken = default)
        {
            var validationResult = _updateQuantityValidator.Validate(command);
            if (!validationResult.IsValid)
                throw new FluentValidation.ValidationException(validationResult.Errors);

            for (var attempt = 0; ; attempt++)
            {
                var cart = await _repo.GetByCustomerIdAsync(command.CustomerId, cancellationToken);
                var item = cart?.Items.FirstOrDefault(cartItem => cartItem.ProductId == command.ProductId);
                if (cart is null || item is null)
                    return false;

                var product = await _productCatalogClient.GetByIdAsync(command.ProductId, cancellationToken);
                if (product is null)
                    throw new KeyNotFoundException($"Product {command.ProductId} was not found.");

                if (!product.IsActive)
                    throw new InvalidOperationException("The product is not available.");

                if (product.StockQuantity < command.Quantity)
                    throw new InvalidOperationException("There is not enough stock for the requested quantity.");

                cart.SetItemQuantity(product.Id, product.Name, product.Price, command.Quantity);
                try
                {
                    await _repo.UpdateAsync(cart, cancellationToken);
                    return true;
                }
                catch (CartConcurrencyException) when (attempt < MaxConcurrencyAttempts - 1)
                {
                }
            }
        }

        public async Task<bool> RemoveItemAsync(Guid customerId, Guid productId, CancellationToken cancellationToken = default)
        {
            for (var attempt = 0; ; attempt++)
            {
                var cart = await _repo.GetByCustomerIdAsync(customerId, cancellationToken);
                if (cart == null || !cart.RemoveItem(productId))
                    return false;

                try
                {
                    await _repo.UpdateAsync(cart, cancellationToken);
                    return true;
                }
                catch (CartConcurrencyException) when (attempt < MaxConcurrencyAttempts - 1)
                {
                }
            }
        }

        public async Task<bool> ClearCartAsync(
            Guid customerId,
            long? expectedRevision = null,
            CancellationToken cancellationToken = default)
        {
            for (var attempt = 0; ; attempt++)
            {
                var cart = await _repo.GetByCustomerIdAsync(customerId, cancellationToken);
                if (cart == null)
                    return true;

                if (expectedRevision.HasValue && cart.Revision != expectedRevision.Value)
                    return false;

                cart.Clear();
                try
                {
                    await _repo.UpdateAsync(cart, cancellationToken);
                    return true;
                }
                catch (CartConcurrencyException) when (attempt < MaxConcurrencyAttempts - 1)
                {
                }
            }
        }

    }
}