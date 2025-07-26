using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;
using System.IO;
using System.Text;

namespace Ce.Gateway.Api
{
    public class Startup
    {
        private const string CeCorsPolicy = "CeCorsPolicy";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

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
                .AddPolly();

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

            //app.UseAuthorization();
            app.UseAuthentication();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

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

            app.UseWebSockets();

            await app.UseOcelot();

            // Log after pipeline is configured
            Log.Information("Service is running");
        }
    }
}
