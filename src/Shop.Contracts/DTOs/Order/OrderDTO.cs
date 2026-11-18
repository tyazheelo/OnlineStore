using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Contracts.DTOs.Order
{
    public class OrderDTO
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string OrderNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
        public string DeliveryAddress { get; set; }
        public DeliveryAddressDTO DeliveryAddressDetails { get; set; } = new();
        public string Status { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DeliveryCost { get; set; }
        public decimal TotalCost { get; set; }
        public string PaymentType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
