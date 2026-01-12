using Discord.Addons.Interactive;
using Discord.WebSocket;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using LlmTornado.Rerank.Models;
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
using Mute.Moe.Services.LLM.Embedding;
using Mute.Moe.Services.LLM.Rerank;
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
using Mute.Moe.Tools.Providers;
using Serpent;
using Serpent.Loading;
using System.IO;
using System.IO.Abstractions;
using System.Net.Http;
using LlmTornado.Chat.Models;
using Wasmtime;

namespace Mute.Moe;

/// <summary>
/// Configures DI container
/// </summary>
/// <param name="Configuration"></param>
public record Startup(Configuration Configuration)
{
    /// <summary>
    /// Add services to the <see cref="IServiceCollection"/>.Does not have access to <see cref="Configuration"/>.
    /// </summary>
    /// <param name="services"></param>
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
        services.AddTransient<IImageUpscaler, ImagesharpUpscaler>();
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
        services.AddSingleton<IUptime>(new UtcDifferenceUptime());
        services.AddSingleton<IStockQuotes, AlphaAdvantageStocks>();
        services.AddSingleton<IForexInfo, AlphaAdvantageForex>();
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

        services.AddTransient<IImageAnalyser, TornadoImageAnalyser>();
        services.AddTransient<IEmbeddings, LLamaServerEmbedding>();
        services.AddTransient<IReranking, LlamaServerReranking>();

        services.AddSingleton<IToolIndex, DatabaseToolIndex>(svc =>
            new DatabaseToolIndex(svc.GetServices<IToolProvider>(), svc.GetRequiredService<IDatabaseService>(), svc.GetRequiredService<IEmbeddings>(), svc.GetRequiredService<IReranking>())
        );

        services.AddSingleton<IConversationStateStorage, ConversationStateStorage>();
    }

    /// <summary>
    /// Add services to the <see cref="IServiceCollection"/>. Has access to <see cref="Configuration"/>.
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureBaseServices(services);

        HostedDiscordBot.ConfigureServices(services);
        services.AddSingleton<HostedDiscordBot>();

        services.AddSingleton(Configuration);

        if (Configuration.Auth == null)
            throw new InvalidOperationException("Cannot start bot: Config.Auth is null");

        // Create LLM stuff
        services.AddSingleton<ChatConversationFactory>();
        if (Configuration.LLM != null)
        {
            var llm = Configuration.LLM;

            // Load system prompt for chat
            services.AddSingleton(new ChatConversationSystemPrompt(File.ReadAllText(llm.ChatSystemPromptPath)));

            // Create endpoints for llama-server
            var endpoints = new List<MultiEndpointProvider<LLamaServerEndpoint>.EndpointConfig>();
            foreach (var endpoint in llm.Endpoints)
            {
                endpoints.Add(new MultiEndpointProvider<LLamaServerEndpoint>.EndpointConfig(
                    new LLamaServerEndpoint(endpoint.ID, endpoint.Endpoint, endpoint.Key, endpoint.ModelsBlacklist.ToHashSet()), endpoint.Slots, new(new(endpoint.Endpoint), endpoint.HealthCheck)
                ));
            }
            services.AddSingleton<MultiEndpointProvider<LLamaServerEndpoint>>(provider => new(
                provider.GetRequiredService<IHttpClientFactory>(),
                new LlamaServerModelCapabilityEndpointFilter(provider.GetRequiredService<IHttpClientFactory>()),
                endpoints.ToArray()
            ));

            // Define models to use
            services.AddSingleton(new LlmChatModel(new ChatModel(llm.ChatLanguageModel.ModelName, LLmProviders.Custom, llm.ChatLanguageModel.ContextSize)));
            services.AddSingleton(new LlmEmbeddingModel(new EmbeddingModel(llm.EmbeddingModel.ModelName, LLmProviders.Custom, llm.EmbeddingModel.ContextSize, llm.EmbeddingModel.EmbeddingDims)));
            services.AddSingleton(new LlmVisionModel(new ChatModel(llm.VisionLanguageModel.ModelName, LLmProviders.Custom, llm.VisionLanguageModel.ContextSize)));
            services.AddSingleton(new LlmRerankModel(new RerankModel(llm.RerankingModel.ModelName, LLmProviders.Custom)));
        }
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
        services.AddSingleton<IToolProvider, ServerStatusToolProvider>();
        services.AddSingleton<IToolProvider, WikipediaToolProvider>();
        services.AddSingleton<IToolProvider, DiceRollToolProvider>();
        services.AddSingleton<IToolProvider, PythonToolProvider>();
        services.AddSingleton<IToolProvider, SubAgentCreationToolProvider>();
        services.AddSingleton<IToolProvider, UserInfoToolProvider>();
        services.AddSingleton<IToolProvider, GuildInfoToolProvider>();
        services.AddSingleton<IToolProvider, ClockProvider>();
    }
}