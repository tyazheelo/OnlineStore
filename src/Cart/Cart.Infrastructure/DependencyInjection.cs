using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Cart.Application.Interfaces.Repositories;
using Cart.Application.Interfaces.Services;
using Cart.Infrastructure.Clients;
using Cart.Infrastructure.Data;
using Cart.Infrastructure.Repositories;

namespace Cart.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCartInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<CartDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "cart")
                ));

            var productApiBaseUrl = configuration["Services:ProductApiBaseUrl"]
                ?? throw new InvalidOperationException("Product API base URL is missing.");
            services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>(client =>
            {
                client.BaseAddress = new Uri(productApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            services.AddScoped<ICartRepository, CartRepository>();

            return services;
        }
    }
}