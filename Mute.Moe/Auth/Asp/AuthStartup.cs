using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.Auth.Asp
{
    public static class AuthStartup
    {
        public static void AddAspAuth(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy(AuthPolicies.DiscordUser, policy => policy.Requirements.Add(new DiscordUserRequirement()));
                options.AddPolicy(AuthPolicies.InAnyBotGuild, policy => policy.Requirements.Add(new InBotGuildRequirement()));
                options.AddPolicy(AuthPolicies.BotOwner, policy => policy.Requirements.Add(new BotOwnerRequirement()));
                options.AddPolicy(AuthPolicies.DenyAll, policy => policy.RequireAssertion(_ => false));   
            });

            services.AddSingleton<IAuthorizationHandler, InBotGuildRequirementHandler>();
            services.AddSingleton<IAuthorizationHandler, BotOwnerRequirementHandler>();
        }
    }
}
