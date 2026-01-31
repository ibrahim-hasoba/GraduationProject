using Graduation.API.Errors;
using Graduation.API.Middlewares;
using Graduation.BLL.JwtFeatures;
using Graduation.BLL.Services.Implementations;
using Graduation.BLL.Services.Interfaces;
using Graduation.DAL.Data;
using Graduation.DAL.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

namespace Graduation.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog FIRST (before creating builder)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "EgyptianMarketplace")
                .WriteTo.Console()
                .WriteTo.File(
                    path: "logs/log-.txt",
                    rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Starting Egyptian Marketplace API");

                var builder = WebApplication.CreateBuilder(args);

                // Use Serilog
                builder.Host.UseSerilog();

                //// Rate Limiting Configuration (BEFORE AddControllers)
                //builder.Services.AddRateLimiter(options =>
                //{
                //    // Global rate limit: 100 requests per minute per user/IP
                //    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                //    {
                //        var username = context.User.Identity?.Name ?? context.Request.Headers.Host.ToString();

                //        return RateLimitPartition.GetFixedWindowLimiter(
                //            partitionKey: username,
                //            factory: partition => new FixedWindowRateLimiterOptions
                //            {
                //                AutoReplenishment = true,
                //                PermitLimit = 100,
                //                Window = TimeSpan.FromMinutes(1)
                //            });
                //    });

                //    // Strict rate limit for authentication endpoints: 5 requests per minute
                //    options.AddFixedWindowLimiter("auth", limiterOptions =>
                //    {
                //        limiterOptions.PermitLimit = 5;
                //        limiterOptions.Window = TimeSpan.FromMinutes(1);
                //        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                //        limiterOptions.QueueLimit = 0;
                //    });

                //    // Rate limit for file uploads: 10 per minute
                //    options.AddFixedWindowLimiter("upload", limiterOptions =>
                //    {
                //        limiterOptions.PermitLimit = 10;
                //        limiterOptions.Window = TimeSpan.FromMinutes(1);
                //        limiterOptions.QueueLimit = 0;
                //    });

                //    options.OnRejected = async (context, cancellationToken) =>
                //    {
                //        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                //        await context.HttpContext.Response.WriteAsJsonAsync(new
                //        {
                //            success = false,
                //            message = "Too many requests. Please try again later.",
                //            retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                //                ? retryAfter.TotalSeconds
                //                : null
                //        }, cancellationToken: cancellationToken);
                //    };
                //});

                // Add services
                builder.Services.AddControllers();
                builder.Services.AddOpenApi();
                builder.Services.AddSwaggerGen();
                builder.Services.AddEndpointsApiExplorer();

                // Database Configuration
                builder.Services.AddDbContext<DatabaseContext>(options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                });

                // Identity Configuration
                builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 8;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.User.RequireUniqueEmail = true;

                    // Email verification
                    options.SignIn.RequireConfirmedEmail = true;
                    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;

                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                })
                .AddEntityFrameworkStores<DatabaseContext>()
                .AddDefaultTokenProviders();

                // JWT Configuration
                var jwtSettings = builder.Configuration.GetSection("JWTSettings");
                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings["validIssuer"],
                        ValidAudience = jwtSettings["validAudience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings["securityKey"]!))
                    };
                })
                .AddFacebook(facebookOptions =>
                {
                    facebookOptions.AppId = builder.Configuration["FacebookAuth:AppId"]!;
                    facebookOptions.AppSecret = builder.Configuration["FacebookAuth:AppSecret"]!;
                    facebookOptions.SaveTokens = true;
                    facebookOptions.Scope.Add("email");
                    facebookOptions.Scope.Add("public_profile");
                    facebookOptions.Fields.Add("name");
                    facebookOptions.Fields.Add("email");
                    facebookOptions.Fields.Add("picture");
                });

                // Register Services
                builder.Services.AddScoped<JwtHandler>();
                builder.Services.AddScoped<IVendorService, VendorService>();
                builder.Services.AddScoped<IEmailService, EmailService>();
                builder.Services.AddScoped<IProductService, ProductService>();
                builder.Services.AddScoped<ICartService, CartService>();
                builder.Services.AddScoped<IOrderService, OrderService>();
                builder.Services.AddScoped<IReviewService, ReviewService>();
                builder.Services.AddScoped<IAdminService, AdminService>();
                builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
                builder.Services.AddEndpointsApiExplorer();
                //builder.Services.AddSwaggerDocumentation();
                // Register Facebook Auth Service with HttpClient
                builder.Services.AddHttpClient<IFacebookAuthService, FacebookAuthService>();

                // CORS Configuration
                var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:3000", "http://localhost:4200" };

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowSpecificOrigins", policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials()
                              .SetIsOriginAllowedToAllowWildcardSubdomains();
                    });
                });

                // Model Validation Configuration
                builder.Services.Configure<ApiBehaviorOptions>(options =>
                {
                    options.InvalidModelStateResponseFactory = actionContext =>
                    {
                        var errors = actionContext.ModelState
                            .Where(m => m.Value.Errors.Count > 0)
                            .SelectMany(m => m.Value.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToArray();

                        var errorResponse = new ApiValidationErrorResponse
                        {
                            Errors = errors
                        };

                        return new BadRequestObjectResult(errorResponse);
                    };
                });

                var app = builder.Build();

                // Seed roles and admin user
                using (var scope = app.Services.CreateScope())
                {
                    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
                    await SeedRolesAndAdmin(roleManager, userManager);
                }

                // Configure middleware pipeline
                app.UseMiddleware<ExceptionMiddleware>();

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Graduation.API v1");
                    });
                }

                app.UseHttpsRedirection();
                app.UseCors("AllowSpecificOrigins");  // Changed from "AllowAll"
                //app.UseRateLimiter();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task SeedRolesAndAdmin(RoleManager<IdentityRole> roleManager, UserManager<AppUser> userManager)
        {
            string[] roles = { "Admin", "Vendor", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@graduationapp.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                var admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }
        }
    }
}