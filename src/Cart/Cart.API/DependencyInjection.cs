using AutoMapper;
using Cart.Application.Commands;
using Cart.Application.Interfaces.Services;
using Cart.Application.Mappings;
using Cart.Application.Services;
using Cart.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cart.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCartApplication(this IServiceCollection services)
        {
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IValidator<AddCartItemCommand>, AddCartItemValidator>();
            services.AddScoped<IValidator<UpdateCartItemQuantityCommand>, UpdateCartItemQuantityValidator>();
            services.AddAutoMapper(_ => { }, typeof(CartMappingProfile).Assembly);

            return services;
        }
    }
}
