namespace Mute.Moe.Services.Telemetry;

/// <summary>
/// Keys to use in telemetry
/// </summary>
public static class Keys
{
    /// <summary>
    /// Keys to use in tags
    /// </summary>
    public static class Tag
    {
        /// <summary>
        /// Standard messaging keys: https://opentelemetry.io/docs/specs/semconv/registry/attributes/messaging/
        /// </summary>
        public static class Messaging
        {
            /// <summary>
            /// A value used by the messaging system as an identifier for the message, represented as a string.
            /// </summary>
            public const string MessageId = "messaging.message.id";

            /// <summary>
            /// The messaging system as identified by the client instrumentation.
            /// </summary>
            public const string MessagingSystem = "messaging.system";

            /// <summary>
            /// The size of the message body in bytes.
            /// </summary>
            public const string MessageBodySize = "messaging.message.body.size";
        }

        /// <summary>
        /// Non standard keys
        /// </summary>
        public static class Discord
        {
            /// <summary>
            /// Transcription from audio message
            /// </summary>
            public const string Transcription = "discord.message.transcription";

            /// <summary>
            /// Discord Guild ID, not set if it's a private message
            /// </summary>
            public const string GuildId = "discord.guild.id";
            
            /// <summary>
            /// Discord channel ID
            /// </summary>
            public const string ChannelId = "discord.channel.id";
            
            /// <summary>
            /// Discord user ID of sender.
            /// </summary>
            public const string UserId = "discord.user.id";
        }

        /// <summary>
        /// Non standard keys
        /// </summary>
        public static class Mute
        {
            /// <summary>
            /// Type of the message context processor interface
            /// </summary>
            public const string ContextProcessorInterfaceType = "mute.context_processor.interface_type";

            /// <summary>
            /// Type of the message context processor (the actual concrete type)
            /// </summary>
            public const string ContextProcessorConcreteType = "mute.context_processor.concrete_type";
        }
    }
}