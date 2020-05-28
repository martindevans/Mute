﻿using Discord;
using GraphQL.Authorization;
using GraphQL.Types;


namespace Mute.Moe.GQL.Schema
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

        private static string GetAvatarUrl(ResolveFieldContext<IGuildUser> ctx)
        {
            return ctx.Source.GetAvatarUrl(ImageFormat.Jpeg, 512)
                ?? ctx.Source.GetDefaultAvatarUrl();
        }
    }
}
