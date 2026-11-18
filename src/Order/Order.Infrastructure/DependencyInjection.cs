using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Order.Application.Interfaces.Repositories;
using Order.Application.Interfaces.Services;
using Order.Infrastructure.Clients;
using Order.Infrastructure.Data;
using Order.Infrastructure.Repositories;
using Order.Infrastructure.Services;

namespace Order.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddOrderInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<OrderDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "orders")
                ));

            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<ISupportTicketRepository, SupportTicketRepository>();
            services.AddHostedService<PendingOrderExpirationService>();
            services.AddHostedService<InventoryConfirmationOutboxService>();

            var cartApiBaseUrl = configuration["Services:CartApiBaseUrl"]
                ?? throw new InvalidOperationException("Cart API base URL is missing.");
            var productApiBaseUrl = configuration["Services:ProductApiBaseUrl"]
                ?? throw new InvalidOperationException("Product API base URL is missing.");
            var identityApiBaseUrl = configuration["Services:IdentityApiBaseUrl"]
                ?? throw new InvalidOperationException("Identity API base URL is missing.");
            var productServiceApiKey = configuration["ServiceAuthentication:ApiKey"]
                ?? throw new InvalidOperationException("Product service API key is missing.");

            services.AddHttpContextAccessor();
            services.AddTransient<UserAuthorizationForwardingHandler>();
            services.AddHttpClient<ICartClient, CartApiClient>(client =>
                {
                    client.BaseAddress = new Uri(cartApiBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .AddHttpMessageHandler<UserAuthorizationForwardingHandler>();
            services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>(client =>
            {
                client.BaseAddress = new Uri(productApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });
            services.AddHttpClient<ICustomerDirectoryClient, CustomerDirectoryClient>(client =>
                {
                    client.BaseAddress = new Uri(identityApiBaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(10);
                })
                .AddHttpMessageHandler<UserAuthorizationForwardingHandler>();
            services.AddHttpClient<IInventoryReservationClient, InventoryReservationClient>(client =>
            {
                client.BaseAddress = new Uri(productApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("X-Service-Api-Key", productServiceApiKey);
            });
            services.AddHttpClient<IProductCatalogClient, ProductCatalogClient>(client =>
            {
                client.BaseAddress = new Uri(productApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            return services;
        }
    }
}