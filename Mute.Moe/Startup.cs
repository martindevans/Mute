using Discord.Addons.Interactive;
using Discord.WebSocket;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Discord;
using Mute.Moe.Discord.Commands;
using Mute.Moe.Discord.Context.Preprocessing;
using Mute.Moe.Discord.Modules;
using Mute.Moe.Discord.Services.Avatar;
using Mute.Moe.Discord.Services.ComponentActions;
using Mute.Moe.Discord.Services.Games;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Discord.Services.Users;
using Mute.Moe.Services.Audio;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.DiceLang.AST;
using Mute.Moe.Services.DiceLang.Macros;
using Mute.Moe.Services.Groups;
using Mute.Moe.Services.Host;
using Mute.Moe.Services.ImageGen;
using Mute.Moe.Services.Information.Anime;
using Mute.Moe.Services.Information.Cryptocurrency;
using Mute.Moe.Services.Information.Forex;
using Mute.Moe.Services.Information.Geocoding;
using Mute.Moe.Services.Information.RSS;
using Mute.Moe.Services.Information.Stocks;
using Mute.Moe.Services.Information.UrbanDictionary;
using Mute.Moe.Services.Information.Weather;
using Mute.Moe.Services.Information.Wikipedia;
using Mute.Moe.Services.Introspection;
using Mute.Moe.Services.Introspection.Uptime;
using Mute.Moe.Services.LLM;
using Mute.Moe.Services.Notifications.Cron;
using Mute.Moe.Services.Notifications.RSS;
using Mute.Moe.Services.Payment;
using Mute.Moe.Services.Randomness;
using Mute.Moe.Services.RateLimit;
using Mute.Moe.Services.Reminders;
using Mute.Moe.Services.Speech;
using Mute.Moe.Services.Speech.STT;
using Mute.Moe.Services.Speech.TTS;
using Mute.Moe.Tools;
using Serpent;
using Serpent.Loading;
using System.IO.Abstractions;
using System.Net.Http;
using LlmTornado.Embedding.Models;
using Wasmtime;

namespace Mute.Moe;

/// <summary>
/// Configures DI container
/// </summary>
/// <param name="Configuration"></param>
public record Startup(Configuration Configuration)
{
    private static void ConfigureBaseServices(IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddSingleton(services);
        services.AddSingleton(s => new InteractiveService(s.GetRequiredService<BaseSocketClient>()));

        services.AddTransient<Random>();
        services.AddTransient<IDiceRoller, CryptoDiceRoller>();
        services.AddTransient<ITextToSpeech, NullTextToSpeech>();
        services.AddTransient<ISpeechToText, WhisperSpeechToText>();

        services.AddTransient<IImageGeneratorBannedWords, HardcodedBannedWords>();
        services.AddTransient<IImageGenerator, Automatic1111>();
        services.AddTransient<IImageAnalyser, Automatic1111>();
        services.AddTransient<IImageUpscaler, Automatic1111>();
        services.AddTransient<IImageOutpainter, Automatic1111>();
        services.AddSingleton<StableDiffusionBackendCache>();
        services.AddSingleton<IImageGenerationConfigStorage, DatabaseImageGenerationStorage>();

        services.AddSingleton<IRateLimit, InMemoryRateLimits>();
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<HttpClient, HttpClient>();
        services.AddSingleton<IDatabaseService, SqliteDatabase>();
        services.AddSingleton<IAnimeInfo, MikibotAnilistAnimeSearch>();
        services.AddSingleton<IMangaInfo, MikibotAnilistMangaSearch>();
        services.AddSingleton<ICharacterInfo, MikibotAnilistCharacterSearch>();
        services.AddSingleton<ITransactions, DatabaseTransactions>();
        services.AddSingleton<IPendingTransactions, DatabasePendingTransactions>();
        services.AddSingleton<ICryptocurrencyInfo, ProCoinMarketCapCrypto>();
        services.AddSingleton<IUptime, UtcDifferenceUptime>();
        services.AddSingleton<IStockQuotes, AlphaVantageStocks>();
        services.AddSingleton<IForexInfo, AlphaVantageForex>();
        services.AddSingleton<IStockSearch, AlphaVantageStockSearch>();
        services.AddSingleton<IGroups, DatabaseGroupService>();
        services.AddSingleton<IReminders, DatabaseReminders>();
        services.AddHostedService<IReminderSender, AsyncReminderSender>();
        services.AddSingleton<IWikipedia, WikipediaApi>();
        services.AddSingleton<IUrbanDictionary, UrbanDictionaryApi>();
        services.AddSingleton<IGuildVoiceCollection, InMemoryGuildVoiceCollection>();
        services.AddSingleton<IGuildSpeechQueueCollection, InMemoryGuildSpeechQueueCollection>();
        services.AddSingleton<IRss, HttpRss>();
        services.AddSingleton<IRssNotifications, DatabaseRssNotifications>();
        services.AddHostedService<IRssNotificationsSender, DatabaseRssNotificationsSender>();
        services.AddSingleton<ICron, InMemoryCron>();
        services.AddSingleton<IUserService, DiscordUserService>();
        services.AddHostedService<IAvatarPicker, SeasonalAvatar>();
        services.AddSingleton<IMacroResolver>(x => x.GetRequiredService<IMacroStorage>());
        services.AddSingleton<IMacroStorage, DatabaseMacroStorage>();
        services.AddSingleton<IWeather, OpenWeatherMapService>();
        services.AddSingleton<IGeocoding, OpenWeatherMapGeocoding>();

        services.AddSingleton(new Engine(new Config().WithFuelConsumption(true)));
        services.AddSingleton(services => PythonBuilder.Load(services.GetRequiredService<Engine>(), new DefaultPythonModuleLoader()));

        services.AddSingleton<IMessagePreprocessor, MobileAudioMessageTranscriptionPreprocessor>();

        services.AddSingleton<Status>();
        services.AddSingleton<ConversationalResponseService>();
        services.AddHostedService<GameService>();
        services.AddSingleton<ComponentActionService>();

        services.AddTransient<IConversationPreprocessor, CommandWordService>();
        services.AddTransient<ICommandWordHandler, RemindersCommandWord>();

        services.AddSingleton<ToolExecutionEngineFactory>();
        AddToolProviders(services);

        services.AddSingleton<IEmbeddings, TornadoEmbeddings>();
        services.AddSingleton<IToolIndex, DatabaseToolIndex>(svc =>
            new DatabaseToolIndex(svc.GetServices<IToolProvider>(), svc.GetRequiredService<IDatabaseService>(), svc.GetRequiredService<IEmbeddings>())
        );
    }

    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureBaseServices(services);

        HostedDiscordBot.ConfigureServices(services);
        services.AddSingleton<HostedDiscordBot>();

        services.AddSingleton(Configuration);

        if (Configuration.Auth == null)
            throw new InvalidOperationException("Cannot start bot: Config.Auth is null");

        var providers = new List<ProviderAuthentication>();
        if (Configuration.RemoteLLM?.Google?.Key != null)
            providers.Add(new ProviderAuthentication(LLmProviders.Google, Configuration.RemoteLLM.Google.Key));
        if (Configuration.RemoteLLM?.OpenAI?.Key != null)
            providers.Add(new ProviderAuthentication(LLmProviders.OpenAi, Configuration.RemoteLLM.OpenAI.Key));
        var api = new TornadoApi(providers);
        services.AddSingleton(api);
        services.AddSingleton(ChatModel.Google.Gemini.Gemini25Flash);
        services.AddSingleton(EmbeddingModel.Google.Gemini.GeminiEmbedding001);
    }

    private static void AddToolProviders(IServiceCollection services)
    {
        services.AddSingleton<IToolProvider, GeocodingToolProvider>();
        services.AddSingleton<IToolProvider, WeatherToolProvider>();
        services.AddSingleton<IToolProvider, MangaToolProvider>();
        services.AddSingleton<IToolProvider, AnimeToolProvider>();
        services.AddSingleton<IToolProvider, CryptocurrencyInfoToolProvider>();
        services.AddSingleton<IToolProvider, ForexToolProvider>();
        services.AddSingleton<IToolProvider, StockToolProvider>();
    }
}