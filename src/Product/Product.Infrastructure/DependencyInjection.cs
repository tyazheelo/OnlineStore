using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Product.Application.Interfaces.Repositories;
using Product.Infrastructure.Data;
using Product.Infrastructure.Repositories;
using Product.Infrastructure.Services;

namespace Product.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddProductInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ProductDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "products")
                ));

            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddHostedService<ExpiredStockReservationService>();

            return services;
        }
    }
}