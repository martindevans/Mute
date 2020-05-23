using System;
using GraphQL.Authorization;
using GraphQL.Types;

using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.Auth;

namespace Mute.Moe.GQL.Schema
{
    public class InjectedSchema
        : GraphQL.Types.Schema
    {
        // ReSharper disable SuggestBaseTypeForParameter (specific types are important for dependency injection)
        public InjectedSchema(IServiceProvider services)
        // ReSharper restore SuggestBaseTypeForParameter
        {
            Query = new MuteQuery(services);
            Mutation = new MuteMutation(services);
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
            void Add( IServiceProvider services,  ObjectGraphType ogt);
        }

        private class MuteMutation
            : ObjectGraphType
        {
            public MuteMutation(IServiceProvider services)
            {
                var roots = services.GetServices<IRootMutation>();
                foreach (var root in roots)
                    root.Add(services, this);

                this.AuthorizeWith(AuthPolicies.InAnyBotGuild);
            }
        }

        public interface IRootMutation
        {
            void Add( IServiceProvider services,  ObjectGraphType ogt);
        }
    }
}
