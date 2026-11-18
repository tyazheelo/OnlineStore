using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Product.Infrastructure;
using System.Security.Claims;
using System.Text;

namespace Product.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // API + Razor Pages + swagger
            builder.Services.AddControllers();
            builder.Services.AddRazorPages();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddAuthorization();

            // ����������� ������ Product (DbContext, �����������, �������)
            builder.Services.AddProductApplication();
            builder.Services.AddProductInfrastructure(builder.Configuration);
            builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

            var jwtIssuer = builder.Configuration["Jwt:Issuer"];
            var jwtAudience = builder.Configuration["Jwt:Audience"];
            var jwtKey = builder.Configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtIssuer)
                || string.IsNullOrWhiteSpace(jwtAudience))
                throw new InvalidOperationException(
                    "Configure Jwt:Issuer and Jwt:Audience to match the Identity API.");

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    var validationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30),
                        NameClaimType = ClaimTypes.Name,
                        RoleClaimType = ClaimTypes.Role
                    };

                    if (Encoding.UTF8.GetByteCount(jwtKey ?? string.Empty) >= 32)
                    {
                        validationParameters.IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey!));
                    }
                    else
                    {
                        options.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = context =>
                            {
                                context.Fail("Product API JWT key is not configured.");
                                return Task.CompletedTask;
                            }
                        };
                    }

                    options.TokenValidationParameters = validationParameters;
                });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
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
                var dbContext = scope.ServiceProvider.GetRequiredService<Product.Infrastructure.Data.ProductDbContext>();
                await dbContext.Database.ExecuteSqlRawAsync("""CREATE SCHEMA IF NOT EXISTS "products";""");
                var historyRepository = dbContext.GetService<IHistoryRepository>();
                await historyRepository.CreateIfNotExistsAsync();
                await dbContext.Database.MigrateAsync();
            }

            app.Run();
        }
    }
}