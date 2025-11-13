
using Ce.Gateway.Api.Data;
using Ce.Gateway.Api.Middleware;
using Ce.Gateway.Api.Repositories;
using Ce.Gateway.Api.Repositories.Interface;
using Ce.Gateway.Api.Services;
using Ce.Gateway.Api.Services.Interface;
using Microsoft.AspNetCore.Identity;
using Ce.Gateway.Api.Entities;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Ce.Gateway.Api
{
    public class Startup
    {
        private const string CeCorsPolicy = "CeCorsPolicy";

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(cfg =>
            {
                cfg.RequireHttpsMetadata = false;
                cfg.SaveToken = true;

                cfg.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Configuration["Tokens:Issuer"],
                    ValidAudience = Configuration["Tokens:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"])),
                    //Do not check the expiry of token
                    ValidateLifetime = false
                };
            });

            services.AddOcelot(Configuration)
                .AddPolly()
                .AddDelegatingHandler<RequestLoggingDelegatingHandler>(true);

            // Use with SignalR
            services.AddCors(o => o.AddPolicy(CeCorsPolicy, b =>
            {
                b.AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetIsOriginAllowed(_ => true)
                    .AllowCredentials();
            }));

            services.AddHealthChecks();

            services.AddHealthChecksUI(setupSettings: setup =>
            {
                setup.SetHeaderText("CW Health Checks UI");

                //default check every 60 seconds
                if (!int.TryParse(Configuration["HealthChecksUI:EvaluationTimeInSeconds"], out var evalTime))
                {
                    evalTime = 60;
                }
                setup.SetEvaluationTimeInSeconds(evalTime);

                // default SetMinimumSecondsBetweenFailureNotifications = 300 seconds
                if (!int.TryParse(Configuration["HealthChecksUI:MinimumSecondsBetweenFailureNotifications"], out var minimumSecondsBetweenFailureNotifications))
                {
                    minimumSecondsBetweenFailureNotifications = 300;
                }

                setup.SetMinimumSecondsBetweenFailureNotifications(minimumSecondsBetweenFailureNotifications);

            })
                .AddInMemoryStorage();

            var dbName = Environment.IsDevelopment() ? "gateway.development.db" : "gateway.db";
            var dbPath = Path.Combine("data", dbName);
            services.AddDbContextFactory<GatewayDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Configure Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 6;

                // Lockout settings - READ FROM CONFIG
                var lockoutMinutes = Configuration.GetValue<int>("Identity:Lockout:LockoutDurationMinutes", 5);
                var maxAttempts = Configuration.GetValue<int>("Identity:Lockout:MaxFailedAccessAttempts", 5);
                
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(lockoutMinutes);
                options.Lockout.MaxFailedAccessAttempts = maxAttempts;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = false;

                // SignIn settings
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<GatewayDbContext>()
            .AddDefaultTokenProviders();

            // Configure cookie authentication to redirect to login
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/account/login";
                options.AccessDeniedPath = "/account/accessdenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
            });

            services.AddSingleton<ILogWriter, SqlLogWriter>();
            services.AddHttpContextAccessor();

            // Add distributed cache (in-memory for now, can be swapped to Redis later)
            // Add IMemoryCache for in-memory caching
            services.AddMemoryCache();

            services.AddScoped<ILogRepository, LogRepository>();
            services.AddScoped<IRequestLogService, RequestLogService>();
            services.AddScoped<INodePerformanceService, NodePerformanceService>();
            
            // Register consolidated dashboard service with integrated caching
            services.AddScoped<IDashboardService, DashboardService>();

            // Register authentication and user management services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();

            services.AddSingleton<IDownstreamHealthStore, DownstreamHealthStore>();

            #region đăng ký DownstreamHealthMonitorService để dùng cho cả IHostedService và IDownstreamHealthMonitorService
            // Tạo một singleton duy nhất => để đảm bảo có 1 instance duy nhất
            services.AddSingleton<DownstreamHealthMonitorService>();

            // Đăng ký background service dùng chính instance đó
            services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<DownstreamHealthMonitorService>());
            #endregion
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // Map endpoint /health
                endpoints.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });

                // Map GUI /hc-ui và /hc-json và style
                endpoints.MapHealthChecksUI(options =>
                {
                    options.UIPath = "/hc-ui";
                    options.ApiPath = "/hc-json";

                    if (File.Exists("health-ui.css"))
                        options.AddCustomStylesheet("health-ui.css");
                    else
                        Log.Warning("health-ui.css not found, using default styles.");
                });
            });

            // Ensure the data directory exists
            Directory.CreateDirectory("data");

            // Auto-migrate database
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<GatewayDbContext>>();
                using (var dbContext = dbContextFactory.CreateDbContext())
                {
                    dbContext.Database.Migrate();
                }
            }

            // Seed database
            await DatabaseSeeder.SeedAsync(app.ApplicationServices);

            app.UseWebSockets();

            var ocelotConfig = new OcelotPipelineConfiguration
            {
                PreErrorResponderMiddleware = async (ctx, next) =>
                {
                    await next();
                }
            };

            await app.UseOcelot(ocelotConfig);

            // Log after pipeline is configured
            Log.Information("Service is running");
        }
    }
}
