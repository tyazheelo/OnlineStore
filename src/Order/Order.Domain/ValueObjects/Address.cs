using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Order.Domain.ValueObjects
{
    public record Address(
    string Country,
    string State,
    string City,
    string Street,
    string PostalCode
)
    {
        public string FullAddress => $"{Street}, {City}, {State}, {Country}, {PostalCode}";
    }
}
