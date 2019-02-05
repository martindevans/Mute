using Discord;
using GraphQL.Authorization;
using GraphQL.Types;
using JetBrains.Annotations;

namespace Mute.Moe.GraphQL.Schema
{
    // ReSharper disable once InconsistentNaming
    public class IGuildUserSchema
        : ObjectGraphType<IGuildUser>
    {
        public IGuildUserSchema()
        {
            this.AuthorizeWith("DiscordUser");

            Field("id", ctx => ctx.Id.ToString());
            Field(a => a.Nickname, nullable: true);
            Field(a => a.Username);
            Field(a => a.Discriminator);
            Field(a => a.AvatarId);
            Field(a => a.IsBot);
            Field(typeof(StringGraphType), "avatarUrl", resolve: GetAvatarUrl);
        }

        private object GetAvatarUrl([NotNull] ResolveFieldContext<IGuildUser> ctx)
        {
            return ctx.Source.GetAvatarUrl(ImageFormat.Jpeg, 512)
                ?? ctx.Source.GetDefaultAvatarUrl();
        }
    }
}
