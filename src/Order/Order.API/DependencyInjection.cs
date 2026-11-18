using Microsoft.Extensions.DependencyInjection;
using Order.Application.Interfaces.Services;
using Order.Application.Services;
using Order.Application.Validators;

namespace Order.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOrderApplication(this IServiceCollection services)
        {
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ISupportService, SupportService>();
            services.AddScoped<CreateOrderValidator>();

            return services;
        }
    }
}
