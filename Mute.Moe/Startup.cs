using AspNetCore.RouteAnalyzer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mute.Moe.Auth;

namespace Mute.Moe
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => false;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddMemoryCache();
            services.AddResponseCaching();
            services.AddResponseCompression();
            services.AddRouteAnalyzer();

            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddXmlSerializerFormatters();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("InAnyBotGuild", policy => policy.Requirements.Add(new InBotGuildRequirement()));
                options.AddPolicy("BotOwner", policy => policy.Requirements.Add(new BotOwnerRequirement()));
                options.AddPolicy("DenyAll", policy => policy.RequireAssertion(_ => false));
            });
            services.AddSingleton<IAuthorizationHandler, InBotGuildRequirementHandler>();
            services.AddSingleton<IAuthorizationHandler, BotOwnerRequirementHandler>();

            services.AddLogging(logging => {
                logging.AddConsole();
                logging.AddDebug();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
            {
                app.UseStatusCodePagesWithRedirects("/error/{0}");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseResponseCaching();
            app.UseResponseCompression();

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRouteAnalyzer("/routes");
            });
        }
    }
}
