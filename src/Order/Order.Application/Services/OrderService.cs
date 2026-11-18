using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using Order.Application.Commands;
using Order.Application.Interfaces.Repositories;
using Order.Application.Interfaces.Services;
using Order.Application.Validators;
using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Shop.Contracts.DTOs;
using Shop.Contracts.DTOs.Order;
using OrderEntity = Order.Domain.Entities.Order;
using OrderItem = Order.Domain.Entities.OrderItem;

namespace Order.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly CreateOrderValidator _validator;
        private readonly IOrderRepository _repo;
        private readonly ICartClient _cartClient;
        private readonly IProductCatalogClient _productCatalogClient;
        private readonly IInventoryReservationClient _inventoryReservationClient;

        public OrderService(
            IOrderRepository repo,
            CreateOrderValidator validator,
            ICartClient cartClient,
            IProductCatalogClient productCatalogClient,
            IInventoryReservationClient inventoryReservationClient)
        {
            _repo = repo;
            _validator = validator;
            _cartClient = cartClient;
            _productCatalogClient = productCatalogClient;
            _inventoryReservationClient = inventoryReservationClient;
        }

        private decimal CalculateDeliveryCost(Address address, List<OrderItem> items)
        {
            var subtotal = items.Sum(i => i.Price * i.Quantity);

            if (subtotal >= 3000m)
                return 0m;

            return 300m;
        }

        public async Task<OrderDTO> CheckoutAsync(CheckoutOrderCommand command)
        {
            if (command.CustomerId == Guid.Empty)
                throw new ArgumentException("Customer ID is required.", nameof(command.CustomerId));

            var cart = await _cartClient.GetCurrentCartAsync();
            if (cart.Items is null || cart.Items.Count == 0)
                throw new InvalidOperationException("Cannot create an order from an empty cart.");

            if (cart.CustomerId != command.CustomerId)
                throw new InvalidOperationException("The cart does not belong to the current user.");

            var orderId = GetCheckoutOrderId(cart.Id, cart.Revision);
            var existingOrder = await _repo.GetByIdAsync(orderId);
            if (existingOrder is not null)
            {
                EnsureSameCheckout(existingOrder, command, cart.Items);
                await _cartClient.ClearCartAsync(cart.Revision);
                return MapToOrderDTO(existingOrder);
            }

            var products = await _productCatalogClient.GetByIdsAsync(
                cart.Items.Select(item => item.ProductId).Distinct().ToArray());
            var productsById = products.ToDictionary(product => product.Id);
            var orderItems = new List<OrderItemCommand>(cart.Items.Count);

            foreach (var cartItem in cart.Items)
            {
                if (!productsById.TryGetValue(cartItem.ProductId, out var product))
                    throw new InvalidOperationException($"Product {cartItem.ProductId} is no longer available.");

                orderItems.Add(new OrderItemCommand
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Quantity = cartItem.Quantity,
                    Price = product.Price
                });
            }

            var orderEntity = BuildOrder(new CreateOrderCommand
            {
                CustomerId = command.CustomerId,
                Items = orderItems,
                DeliveryAddress = command.DeliveryAddress,
                PaymentType = command.PaymentType
            }, orderId);

            await _inventoryReservationClient.ReserveAsync(orderEntity.Id, orderItems);
            try
            {
                await _repo.AddAsync(orderEntity);
            }
            catch (Exception saveException)
            {
                OrderEntity? persistedOrder;
                try
                {
                    persistedOrder = await _repo.GetByIdAsync(orderEntity.Id);
                }
                catch (Exception lookupException)
                {
                    throw new AggregateException(
                        "Saving the order failed and its persisted state could not be checked. The reservation was kept for safe retry.",
                        saveException,
                        lookupException);
                }

                if (persistedOrder is not null)
                {
                    EnsureSameCheckout(persistedOrder, orderEntity);
                    orderEntity = persistedOrder;
                }
                else
                {
                    try
                    {
                        await _inventoryReservationClient.ReleaseAsync(orderEntity.Id);
                    }
                    catch (Exception releaseException)
                    {
                        throw new AggregateException(
                            "Saving the order and releasing its stock reservation both failed.",
                            saveException,
                            releaseException);
                    }

                    ExceptionDispatchInfo.Capture(saveException).Throw();
                    throw;
                }
            }

            await _cartClient.ClearCartAsync(cart.Revision);
            return MapToOrderDTO(orderEntity);
        }

        private static Guid GetCheckoutOrderId(Guid cartId, long revision)
        {
            var key = Encoding.UTF8.GetBytes($"{cartId:N}:{revision}");
            var hash = SHA256.HashData(key);
            return new Guid(hash.AsSpan(0, 16));
        }

        private static void EnsureSameCheckout(OrderEntity existing, OrderEntity requested)
        {
            var sameItems = existing.Items
                .OrderBy(item => item.ProductId)
                .Select(item => (item.ProductId, item.Quantity))
                .SequenceEqual(requested.Items
                    .OrderBy(item => item.ProductId)
                    .Select(item => (item.ProductId, item.Quantity)));

            if (existing.CustomerId != requested.CustomerId
                || existing.DeliveryAddress != requested.DeliveryAddress
                || existing.PaymentType != requested.PaymentType
                || !sameItems)
            {
                throw new InvalidOperationException(
                    "This cart revision has already been checked out with different order details.");
            }
        }

        private static void EnsureSameCheckout(
            OrderEntity existing,
            CheckoutOrderCommand command,
            IReadOnlyCollection<Shop.Contracts.DTOs.Cart.CartItemDTO> cartItems)
        {
            var sameItems = existing.Items
                .OrderBy(item => item.ProductId)
                .Select(item => (item.ProductId, item.Quantity))
                .SequenceEqual(cartItems
                    .OrderBy(item => item.ProductId)
                    .Select(item => (item.ProductId, item.Quantity)));

            if (existing.CustomerId != command.CustomerId
                || existing.DeliveryAddress != command.DeliveryAddress
                || existing.PaymentType != command.PaymentType
                || !sameItems)
            {
                throw new InvalidOperationException(
                    "This cart revision has already been checked out with different order details.");
            }
        }

        private OrderEntity BuildOrder(CreateOrderCommand command, Guid orderId)
        {
            _validator.Validate(command);

            if (command.DeliveryAddress == null)
                throw new ArgumentNullException(nameof(command.DeliveryAddress));

            var listOfItems = command.Items
                .Select(i => OrderItem.Create(
                    orderId: Guid.Empty,
                    productId: i.ProductId,
                    productName: i.ProductName,
                    quantity: i.Quantity,
                    price: i.Price,
                    isConnectedToOrder: false))
                .ToList();

            var address = command.DeliveryAddress;
            var paymentType = command.PaymentType;

            var deliveryCost = CalculateDeliveryCost(address, listOfItems);

            var newOrder = OrderEntity.Create(
                command.CustomerId,
                listOfItems,
                address,
                paymentType,
                deliveryCost,
                orderId
            );

            return newOrder;
        }
        public async Task<OrderDTO?> GetOrderByIdAsync(Guid id)
        {
            var order = await _repo.GetByIdAsync(id);
            return order == null ? null : MapToOrderDTO(order);
        }

        public async Task<OrderDTO?> GetOrderByIdAsync(Guid id, Guid customerId)
        {
            var order = await _repo.GetByIdAsync(id, customerId);
            return order == null ? null : MapToOrderDTO(order);
        }

        public async Task<List<OrderDTO>> GetOrdersByCustomerIdAsync(Guid customerId)
        {
            var orders = await _repo.GetByCustomerIdAsync(customerId);
            return orders.Select(MapToOrderDTO).ToList();
        }

        public async Task<List<OrderDTO>> GetAllOrdersAsync()
        {
            var orders = await _repo.GetAllAsync();
            return orders.Select(MapToOrderDTO).ToList();
        }

        public async Task<List<OrderDTO>> GetOrdersByStatusAsync(string status)
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var orderStatus)
                || !Enum.IsDefined(orderStatus))
                throw new ArgumentException($"Invalid status: {status}");

            var orders = await _repo.GetByStatusAsync(orderStatus);
            return orders.Select(MapToOrderDTO).ToList();
        }

        public async Task<List<OrderDTO>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            if (startDate > endDate)
                throw new ArgumentException("Start date must be less than end date");

            var orders = await _repo.GetByDateRangeAsync(startDate, endDate);
            return orders.Select(MapToOrderDTO).ToList();
        }

        public async Task<OrderDTO?> UpdateOrderStatusAsync(Guid id, string status)
        {
            if (!Enum.TryParse<OrderStatus>(status, true, out var newStatus)
                || !Enum.IsDefined(newStatus))
            {
                if (await _repo.GetByIdAsync(id) is null)
                    return null;

                throw new ArgumentException($"Invalid status: {status}. Allowed: Pending, Paid, Processing, Shipped, Delivered, Cancelled");
            }

            if (newStatus == OrderStatus.Paid)
            {
                var confirmationStatus = await _repo.QueueInventoryConfirmationAsync(id);
                if (confirmationStatus is null)
                    return null;
                if (confirmationStatus == OrderStatus.Paid)
                {
                    var alreadyPaidOrder = await _repo.GetByIdAsync(id);
                    return alreadyPaidOrder is null ? null : MapToOrderDTO(alreadyPaidOrder);
                }

                try
                {
                    await _inventoryReservationClient.ConfirmAsync(id);
                    await _repo.CompleteInventoryConfirmationAsync(id);
                }
                catch (Exception confirmationException)
                {
                    try
                    {
                        await _repo.RecordInventoryConfirmationFailureAsync(id, DateTime.UtcNow);
                    }
                    catch (Exception recordingException)
                    {
                        throw new AggregateException(
                            "Inventory confirmation failed and its retry could not be scheduled.",
                            confirmationException,
                            recordingException);
                    }

                    ExceptionDispatchInfo.Capture(confirmationException).Throw();
                    throw;
                }

                var queuedOrder = await _repo.GetByIdAsync(id);
                return queuedOrder is null ? null : MapToOrderDTO(queuedOrder);
            }

            var order = await _repo.GetByIdAsync(id);
            if (order == null)
                return null;

            switch (newStatus)
            {
                case OrderStatus.Processing:
                    order.MarkAsProcessing();
                    break;
                case OrderStatus.Shipped:
                    order.MarkAsShipped();
                    break;
                case OrderStatus.Delivered:
                    order.MarkAsDelivered();
                    break;
                case OrderStatus.Cancelled:
                    throw new InvalidOperationException("Orders can only be cancelled through the cancel order action.");
                case OrderStatus.Pending:
                    throw new InvalidOperationException("Cannot set status to Pending manually");
                case OrderStatus.PaymentConfirmationPending:
                    throw new InvalidOperationException("Payment confirmation status is managed automatically.");
                default:
                    throw new ArgumentException($"Invalid status: {status}");
            }

            await _repo.UpdateAsync(order);
            return MapToOrderDTO(order);
        }

        public async Task<OrderDTO?> UpdateDeliveryAddressAsync(Guid id, Address address)
        {
            var order = await _repo.GetByIdAsync(id);
            if (order is null)
                return null;

            order.UpdateDeliveryAddress(address);
            await _repo.UpdateAsync(order);
            return MapToOrderDTO(order);
        }

        public async Task<OrderDTO?> CancelOrderAsync(Guid id, Guid customerId)
        {
            var order = await _repo.GetByIdAsync(id, customerId);
            if (order == null)
                return null;

            order.Cancel();
            await _inventoryReservationClient.ReleaseAsync(order.Id);
            await _repo.UpdateAsync(order);
            return MapToOrderDTO(order);
        }

        public async Task<OrderDTO?> CancelOrderAsync(Guid id)
        {
            var order = await _repo.GetByIdAsync(id);

            if (order == null)
                return null;

            order.Cancel(); 
            await _inventoryReservationClient.ReleaseAsync(order.Id);

            await _repo.UpdateAsync(order);
            return MapToOrderDTO(order);
        }

        public async Task DeleteOrderAsync(Guid id)
        {
            var order = await _repo.GetByIdAsync(id);

            if (order == null)
                throw new KeyNotFoundException($"Order with ID {id} not found");

            if (order.Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.PaymentConfirmationPending)
                throw new InvalidOperationException($"Cannot delete order with status {order.Status}");

            await _inventoryReservationClient.ReleaseAsync(order.Id);
            await _repo.DeleteAsync(id);
        }

        private OrderDTO MapToOrderDTO(OrderEntity order)
        {
            return new OrderDTO
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                OrderNumber = order.OrderNumber,
                CustomerName = string.Empty,
                CustomerEmail = string.Empty,
                Items = order.Items.Select(item => new OrderItemDTO
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    TotalCost = item.TotalCost
                }).ToList(),
                DeliveryAddress = order.DeliveryAddress.FullAddress,
                DeliveryAddressDetails = new DeliveryAddressDTO
                {
                    Country = order.DeliveryAddress.Country,
                    State = order.DeliveryAddress.State,
                    City = order.DeliveryAddress.City,
                    Street = order.DeliveryAddress.Street,
                    PostalCode = order.DeliveryAddress.PostalCode
                },
                Status = order.Status.ToDisplayString(),
                SubTotal = order.SubTotal,
                DeliveryCost = order.DeliveryCost,
                TotalCost = order.TotalCost,
                PaymentType = order.PaymentType.ToDisplayString(),
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }
    }
}