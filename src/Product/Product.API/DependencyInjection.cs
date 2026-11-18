using Microsoft.Extensions.DependencyInjection;
using Product.Application.Interfaces.Services;
using Product.Application.Services;

namespace Product.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProductApplication(this IServiceCollection services)
        {
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<IInventoryReservationService, InventoryReservationService>();

            return services;
        }
    }
}
