using Discord.WebSocket;
using GraphQL.Authorization;
using GraphQL.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IAuthorizationEvaluator = GraphQL.Authorization.IAuthorizationEvaluator;

namespace Mute.Moe.Auth.GraphQL
{
    public static class AuthStartup
    {
        public static void AddGraphQLAuth(this IServiceCollection services)
        {
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.TryAddSingleton<IAuthorizationEvaluator, AuthorizationEvaluator>();
            services.AddTransient<IValidationRule, AuthorizationValidationRule>();

            services.TryAddSingleton(s =>
            {
                var authSettings = new AuthorizationSettings();
                var discord = s.GetService<DiscordSocketClient>();

                authSettings.AddPolicy(AuthPolicies.DiscordUser, a => a.AddRequirement(new DiscordUser(discord)));
                authSettings.AddPolicy(AuthPolicies.InAnyBotGuild, a => a.AddRequirement(new InBotGuild(discord)));
                authSettings.AddPolicy(AuthPolicies.BotOwner, a => a.AddRequirement(new BotOwner(discord)));
                authSettings.AddPolicy(AuthPolicies.DenyAll, a => a.RequireClaim("DF6594A8-4AFA-4BF8-9C85-AB67AC34197A"));

                return authSettings;
            });
        }
    }

    
}
