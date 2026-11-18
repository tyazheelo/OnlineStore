using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Domain.Enums
{
    public enum PaymentType
    {
        ByCard
    }
    public static class PaymentTypeExtensions
    {
        public static string ToDisplayString(this PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.ByCard => "By card",
                _ => paymentType.ToString()
            };
        }
    }
}
