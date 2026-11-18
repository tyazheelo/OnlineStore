using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 0,
        Paid = 1,
        Processing = 2,
        Shipped = 3,
        Delivered = 4,
        Cancelled = 5,
        PaymentConfirmationPending = 6
    }

    public static class OrderStatusExtensions
    {
        public static string ToDisplayString(this OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Pending",
                OrderStatus.Paid => "Paid",
                OrderStatus.Processing => "Processing",
                OrderStatus.Shipped => "Shipped",
                OrderStatus.Delivered => "Delivered",
                OrderStatus.Cancelled => "Cancelled",
                OrderStatus.PaymentConfirmationPending => "PaymentConfirmationPending",
                _ => status.ToString()
            };
        }
    }
}
