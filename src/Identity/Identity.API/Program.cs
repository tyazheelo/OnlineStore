using System.Security.Claims;
using System.Text;
using Identity.Infrastructure;
using Identity.Infrastructure.Authentication;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.IdentityModel.Tokens;

namespace Identity.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddRazorPages();
            builder.Services.AddAuthorization();
            builder.Services.AddIdentityInfrastructure(builder.Configuration);
            builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
                policy.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()));
            var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
                var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                await dbContext.Database.ExecuteSqlRawAsync("""CREATE SCHEMA IF NOT EXISTS "identity";""");
                var historyRepository = dbContext.GetService<IHistoryRepository>();
                await historyRepository.CreateIfNotExistsAsync();
                await dbContext.Database.MigrateAsync();

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                if (!await roleManager.RoleExistsAsync("Admin"))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
                    if (!roleResult.Succeeded)
                        throw new InvalidOperationException(
                            $"Could not create Admin role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
                }

                var configuredAdminId = builder.Configuration["Admin:UserId"];
                if (!string.IsNullOrWhiteSpace(configuredAdminId))
                {
                    if (!Guid.TryParse(configuredAdminId, out var adminId) || adminId == Guid.Empty)
                        throw new InvalidOperationException("Admin:UserId must be a valid user ID.");

                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                    var adminUser = await userManager.FindByIdAsync(adminId.ToString());
                    if (adminUser is null)
                        throw new InvalidOperationException(
                            $"Configured Admin:UserId '{adminId}' does not match an existing account.");

                    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                    {
                        var assignmentResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                        if (!assignmentResult.Succeeded)
                            throw new InvalidOperationException(
                                $"Could not assign Admin role: {string.Join("; ", assignmentResult.Errors.Select(error => error.Description))}");
                    }
                }
            }

            await app.RunAsync();
        }
    }
}
