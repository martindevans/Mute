using System;
using System.IO;
using System.IO.Abstractions;
using AspNetCore.RouteAnalyzer;
using Discord.Addons.Interactive;
using GraphQL.Types;
using GraphQL.Utilities;
using GraphQL.Validation.Complexity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mute.Moe.Discord;
using Mute.Moe.Discord.Services;
using Mute.Moe.Discord.Services.Audio;
using Mute.Moe.Discord.Services.Audio.Playback;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Groups;
using Mute.Moe.Services.Images.Cats;
using Mute.Moe.Services.Images.Dogs;
using Mute.Moe.Services.Information.Anime;
using Mute.Moe.Services.Information.Cryptocurrency;
using Mute.Moe.Services.Information.Forex;
using Mute.Moe.Services.Information.SpaceX;
using Mute.Moe.Services.Information.Steam;
using Mute.Moe.Services.Information.Stocks;
using Mute.Moe.Services.Introspection;
using Mute.Moe.Services.Introspection.Uptime;
using Mute.Moe.Services.Payment;
using Mute.Moe.Services.Randomness;
using Mute.Moe.Services.Sentiment;
using Newtonsoft.Json;
using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Ui.GraphiQL;
using Mute.Moe.Auth.Asp;
using Mute.Moe.Auth.GraphQL;
using Mute.Moe.GQL;
using Mute.Moe.GQL.Schema;
using Mute.Moe.Services.Information.UrbanDictionary;
using Mute.Moe.Services.Information.Wikipedia;
using Mute.Moe.Services.Notifications.SpaceX;
using Mute.Moe.Services.Reminders;
using Mute.Moe.Services.Sentiment.Training;
using Mute.Moe.Services.SoundEffects;
using Mute.Moe.Services.Words;
using Mute.Moe.Utilities;

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
            services.AddSingleton(services);
            services.AddSingleton<InteractiveService>();

            services.AddTransient<Random>();
            services.AddTransient<IDiceRoller, CryptoDiceRoller>();

            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IHttpClient, SimpleHttpClient>();
            services.AddSingleton<IDatabaseService, SqliteDatabase>();
            services.AddSingleton<ISentimentEvaluator, TensorflowSentiment>();
            services.AddSingleton<ISentimentTrainer, DatabaseSentimentTrainer>();
            services.AddSingleton<ICatPictureProvider, CataasPictures>();
            services.AddSingleton<IArtificialCatPictureProvider, ThisCatDoesNotExist>();
            services.AddSingleton<IDogPictureService, DogceoPictures>();
            services.AddSingleton<IAnimeInfo, MikibotAnilistAnimeSearch>();
            services.AddSingleton<IMangaInfo, MikibotAnilistMangaSearch>();
            services.AddSingleton<ICharacterInfo, MikibotAnilistCharacterSearch>();
            services.AddSingleton<ITransactions, DatabaseTransactions>();
            services.AddSingleton<IPendingTransactions, DatabasePendingTransactions>();
            services.AddSingleton<ISpacexInfo, OdditySpaceX>();
            services.AddSingleton<ICryptocurrencyInfo, ProCoinMarketCapCrypto>();
            services.AddSingleton<ISteamInfo, SteamApi>();
            services.AddSingleton<IUptime, UtcDifferenceUptime>();
            services.AddSingleton<IStockInfo, AlphaVantageStocks>();
            services.AddSingleton<IForexInfo, AlphaVantageForex>();
            services.AddSingleton<IGroups, DatabaseGroupService>();
            services.AddSingleton<IReminders, DatabaseReminders>();
            services.AddSingleton<IReminderSender, AsyncReminderSender>();
            services.AddSingleton<IWikipedia, WikipediaApi>();
            services.AddSingleton<ISoundEffectLibrary, DatabaseSoundEffectLibrary>();
            services.AddSingleton<ISoundEffectPlayer, SoundEffectPlayer>();
            services.AddSingleton<IWords, HttpWordVectors>();
            services.AddSingleton<ISpacexNotifications, DatabaseSpacexNotifications>();
            services.AddSingleton<ISpacexNotificationsSender, AsyncSpacexNotificationsSender>();
            services.AddSingleton<IUrbanDictionary, UrbanDictionaryApi>();

            services.AddSingleton<AutoReactionTrainer>();
            services.AddSingleton<Status>();

            //Eventually these should all become interface -> concrete type bindings
            services
                .AddSingleton<MusicPlayerService>()
                .AddSingleton<YoutubeService>()
                .AddSingleton<MusicRatingService>()
                .AddSingleton<GameService>()
                .AddSingleton<ConversationalResponseService>()
                .AddSingleton<WordsService>()
                .AddSingleton<WordTrainingService>()
                .AddSingleton<MultichannelAudioService>();
        }

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
            services.AddRouteAnalyzer();

            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddXmlSerializerFormatters();

            services.AddAspAuth();
            
            services.AddLogging(logging => {
                //logging.AddConsole();
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

            services.AddSingleton<IUserContextBuilder, GraphQLUserContextBuilder>();

            //GraphQL setup
            GraphTypeTypeRegistry.Register<TimeSpan, TimeSpanMillisecondsGraphType>();
            services.AddSingleton<InjectedSchema>();
            services.AddSingleton<InjectedSchema.IRootQuery, StatusSchema>();
            services.AddSingleton<InjectedSchema.IRootQuery, RemindersQuerySchema>();
            services.AddSingleton<InjectedSchema.IRootMutation, RemindersMutationSchema>();

            services.AddGraphQLAuth();
            services.AddGraphQL(options => {
                options.EnableMetrics = true;
                options.ExposeExceptions = true;
                options.ComplexityConfiguration = new ComplexityConfiguration { MaxDepth = 15 };
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
            {
                app.UseStatusCodePagesWithRedirects("/error/{0}");
                //app.UseHttpsRedirection();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseResponseCaching();

            app.UseGraphQL<InjectedSchema>();
            app.UseGraphiQLServer(new GraphiQLOptions
            {
                GraphiQLPath = "/graphiql",
                GraphQLEndPoint = "/graphql"
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRouteAnalyzer("/routes");
            });
        }
    }
}
