using Order.Domain.Enums;
using Order.Domain.ValueObjects;
using Shop.Contracts.Requests.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Application.Commands
{
    public class CreateOrderCommand
    {
        public Guid CustomerId { get; set; }
        public List<OrderItemCommand> Items { get; set; }
        public Address DeliveryAddress { get; set; }
        public PaymentType PaymentType { get; set; }
    }
}
