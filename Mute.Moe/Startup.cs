using System;
using System.IO.Abstractions;
using Discord.Addons.Interactive;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Groups;
using Mute.Moe.Services.Images.Cats;
using Mute.Moe.Services.Images.Dogs;
using Mute.Moe.Services.Information.Anime;
using Mute.Moe.Services.Information.Cryptocurrency;
using Mute.Moe.Services.Information.Forex;
using Mute.Moe.Services.Information.SpaceX;
using Mute.Moe.Services.Information.Stocks;
using Mute.Moe.Services.Introspection;
using Mute.Moe.Services.Introspection.Uptime;
using Mute.Moe.Services.Payment;
using Mute.Moe.Services.Randomness;
using Mute.Moe.Services.Sentiment;
using Mute.Moe.Services.Audio;
using Mute.Moe.Services.Audio.Sources.Youtube;
using Mute.Moe.Services.Information.RSS;
using Mute.Moe.Services.Information.UrbanDictionary;
using Mute.Moe.Services.Information.Wikipedia;
using Mute.Moe.Services.Music;
using Mute.Moe.Services.Notifications.RSS;
using Mute.Moe.Services.Notifications.SpaceX;
using Mute.Moe.Services.Reminders;
using Mute.Moe.Services.Sentiment.Training;
using Mute.Moe.Services.SoundEffects;
using Mute.Moe.Services.Speech;
using Mute.Moe.Services.Words;
using System.Net.Http;
using Discord.WebSocket;
using Mute.Moe.Services.Notifications.Cron;
using Mute.Moe.Discord.Services.Avatar;
using Mute.Moe.Discord.Services.Responses.Eliza.Scripts;
using Mute.Moe.Discord.Services.Users;
using Oddity;
using Mute.Moe.Services.Host;

namespace Mute.Moe
{
    public class Startup
    {
        public Configuration Configuration { get; }

        public Startup(Configuration configuration)
        {
            Configuration = configuration;
        }

        private static void ConfigureBaseServices(IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddSingleton(services);
            services.AddSingleton(s => new InteractiveService(s.GetRequiredService<BaseSocketClient>()));

            services.AddTransient<Random>();
            services.AddTransient<IDiceRoller, CryptoDiceRoller>();
            services.AddTransient<ISpacexInfo, OdditySpaceX>();

            services.AddSingleton(new OddityCore());
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<HttpClient, HttpClient>();
            services.AddSingleton<IDatabaseService, SqliteDatabase>();
            services.AddHostedService<ISentimentEvaluator, TensorflowSentiment>();
            services.AddSingleton<ISentimentTrainer, DatabaseSentimentTrainer>();
            services.AddSingleton<ICatPictureProvider, CataasPictures>();
            services.AddSingleton<IArtificialCatPictureProvider, ThisCatDoesNotExist>();
            services.AddSingleton<IDogPictureService, DogceoPictures>();
            services.AddSingleton<IAnimeInfo, MikibotAnilistAnimeSearch>();
            services.AddSingleton<IMangaInfo, MikibotAnilistMangaSearch>();
            services.AddSingleton<ICharacterInfo, MikibotAnilistCharacterSearch>();
            services.AddSingleton<ITransactions, DatabaseTransactions>();
            services.AddSingleton<IPendingTransactions, DatabasePendingTransactions>();
            services.AddSingleton<ICryptocurrencyInfo, ProCoinMarketCapCrypto>();
            services.AddHostedService<IUptime, UtcDifferenceUptime>();
            services.AddSingleton<IStockQuotes, AlphaVantageStocks>();
            services.AddSingleton<IForexInfo, AlphaVantageForex>();
            services.AddSingleton<IStockSearch, AlphaVantageStockSearch>();
            services.AddSingleton<IGroups, DatabaseGroupService>();
            services.AddSingleton<IReminders, DatabaseReminders>();
            services.AddHostedService<IReminderSender, AsyncReminderSender>();
            services.AddSingleton<IWikipedia, WikipediaApi>();
            services.AddSingleton<ISoundEffectLibrary, DatabaseSoundEffectLibrary>();
            services.AddSingleton<ISoundEffectPlayer, SoundEffectPlayer>();
            services.AddSingleton<IWords, HttpWordVectors>();
            services.AddSingleton<ISpacexNotifications, DatabaseSpacexNotifications>();
            services.AddHostedService<ISpacexNotificationsSender, AsyncSpacexNotificationsSender>();
            services.AddSingleton<IUrbanDictionary, UrbanDictionaryApi>();
            services.AddSingleton<IYoutubeDownloader, YoutubeDlDownloader>();
            services.AddSingleton<IWordTraining, DatabaseWordTraining>();
            services.AddSingleton<IMusicLibrary, DatabaseMusicLibrary>();
            services.AddSingleton<IGuildVoiceCollection, InMemoryGuildVoiceCollection>();
            services.AddSingleton<IGuildMusicQueueCollection, InMemoryGuildMusicQueueCollection>();
            services.AddSingleton<IGuildSpeechQueueCollection, InMemoryGuildSpeechQueueCollection>();
            services.AddSingleton<IGuildSoundEffectQueueCollection, InMemoryGuildSoundEffectQueueCollection>();
            services.AddSingleton<IRss, HttpRss>();
            services.AddSingleton<IRssNotifications, DatabaseRssNotifications>();
            services.AddHostedService<IRssNotificationsSender, DatabaseRssNotificationsSender>();
            services.AddSingleton<ICron, InMemoryCron>();
            services.AddSingleton<IUserService, DiscordUserService>();

            //Eventually these should all become interface -> concrete type bindings
            services.AddHostedService<AutoReactionTrainer>();
            services.AddSingleton<Status>();
            services
                .AddSingleton<GameService>()
                .AddSingleton<ConversationalResponseService>()
                .AddSingleton<WordsService>()
                .AddSingleton<SeasonalAvatar>()
                .AddSingleton(Script.Load);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureBaseServices(services);

            HostedDiscordBot.ConfigureServices(services);
            services.AddSingleton<HostedDiscordBot>();

            services.AddSingleton(Configuration);

            if (Configuration.Auth == null)
                throw new InvalidOperationException("Cannot start bot: Config.Auth is null");
        }
    }
}
