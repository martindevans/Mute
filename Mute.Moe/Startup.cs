using System;
using System.IO;
using AspNetCore.RouteAnalyzer;
using Discord.Addons.Interactive;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mute.Moe.Auth;
using Mute.Moe.Discord;
using Mute.Moe.Discord.Services;
using Mute.Moe.Discord.Services.Audio;
using Mute.Moe.Discord.Services.Audio.Playback;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Images;
using Mute.Moe.Services.Payment;
using Mute.Moe.Services.Search;
using Mute.Moe.Services.Sentiment;
using Newtonsoft.Json;

namespace Mute.Moe
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private static void ConfigureBaseServices(IServiceCollection services)
        {
            services.AddTransient<Random>();

            services.AddSingleton(services);
            services.AddSingleton<InteractiveService>();

            services.AddSingleton<IHttpClient, SimpleHttpClient>();
            services.AddSingleton<IDatabaseService, SqliteDatabase>();
            services.AddSingleton<ISentimentService, TensorflowSentiment>();
            services.AddSingleton<ICatPictureService, CataasPictures>();
            services.AddSingleton<IDogPictureService, DogceoPictures>();
            services.AddSingleton<IAnimeSearch, NadekobotAnimeSearch>();
            services.AddSingleton<ITransactions, DatabaseTransactionService>();

            //Eventually these should all become interface -> concrete type bindings
            services
                .AddSingleton<CryptoCurrencyService>()
                .AddSingleton<AlphaAdvantageService>()
                .AddSingleton<IouDatabaseService>()
                .AddSingleton<MusicPlayerService>()
                .AddSingleton<YoutubeService>()
                .AddSingleton<MusicRatingService>()
                .AddSingleton<GameService>()
                .AddSingleton<ReminderService>()
                .AddSingleton<SentimentTrainingService>()
                .AddSingleton<HistoryLoggingService>()
                .AddSingleton<ReactionSentimentTrainer>()
                .AddSingleton<ConversationalResponseService>()
                .AddSingleton<WikipediaService>()
                .AddSingleton<TimeService>()
                .AddSingleton<SteamApi>()
                .AddSingleton<SoundEffectService>()
                .AddSingleton<WordsService>()
                .AddSingleton<SpacexService>()
                .AddSingleton<WordVectorsService>()
                .AddSingleton<UptimeService>()
                .AddSingleton<WordTrainingService>()
                .AddSingleton<RoleService>()
                .AddSingleton<MultichannelAudioService>();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureBaseServices(services);
            HostedDiscordBot.ConfigureServices(services);
            services.AddHostedService<HostedDiscordBot>();
            services.AddHostedService<ServicePreloader>();

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

            var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(Configuration["BotConfigPath"]));
            services.AddSingleton(config);

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie("Cookies").AddDiscord(d => {
                d.AppId = config.Auth.ClientId;
                d.AppSecret = config.Auth.ClientSecret;
                d.Scope.Add("identify");
                d.SaveTokens = true;
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
