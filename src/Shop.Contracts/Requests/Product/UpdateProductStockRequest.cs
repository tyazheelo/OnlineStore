using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shop.Contracts.Requests.Product
{
    public class UpdateProductStockRequest
    {
        public int StockChange {  get; set; }
    }
}
