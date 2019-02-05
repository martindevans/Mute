using System;
using GraphQL.Authorization;
using GraphQL.Types;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Auth;

namespace Mute.Moe.GraphQL.Schema
{
    public class InjectedSchema
        : global::GraphQL.Types.Schema
    {
        // ReSharper disable SuggestBaseTypeForParameter (specific types are important for dependency injection)
        public InjectedSchema(IServiceProvider services)
        // ReSharper restore SuggestBaseTypeForParameter
        {
            Query = new MuteQuery(services);
        }

        private class MuteQuery
            : ObjectGraphType
        {
            public MuteQuery(IServiceProvider services)
            {
                var roots = services.GetServices<IRootQuery>();
                foreach (var root in roots)
                    root.Add(services, this);

                this.AuthorizeWith(AuthPolicies.InAnyBotGuild);
            }
        }

        public interface IRootQuery
        {
            void Add([NotNull] IServiceProvider services, [NotNull] ObjectGraphType ogt);
        }
    }
}
