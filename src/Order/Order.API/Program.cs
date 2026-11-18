using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Order.Infrastructure;

namespace Order.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddRazorPages();
            builder.Services.AddAuthorization();
            builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()));
            var jwt = builder.Configuration.GetSection("Jwt").Get<JwtConfiguration>()
                ?? throw new InvalidOperationException("JWT configuration is missing.");
            if (string.IsNullOrWhiteSpace(jwt.Issuer) || string.IsNullOrWhiteSpace(jwt.Audience) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
                throw new InvalidOperationException("Configure Jwt:Issuer, Jwt:Audience and a Jwt:Key of at least 32 bytes.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = ClaimTypes.NameIdentifier,
                        RoleClaimType = ClaimTypes.Role
                    };
                });
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // ����������� ������ Order
            builder.Services.AddOrderApplication();
            builder.Services.AddOrderInfrastructure(builder.Configuration);

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<Order.Infrastructure.Data.OrderDbContext>();
                await dbContext.Database.ExecuteSqlRawAsync("""CREATE SCHEMA IF NOT EXISTS "orders";""");
                var historyRepository = dbContext.GetService<IHistoryRepository>();
                await historyRepository.CreateIfNotExistsAsync();
                await dbContext.Database.MigrateAsync();
            }

            app.Run();
        }

        private sealed class JwtConfiguration
        {
            public string Issuer { get; set; } = string.Empty;
            public string Audience { get; set; } = string.Empty;
            public string Key { get; set; } = string.Empty;
        }
    }
}