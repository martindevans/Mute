using Discord;
using GraphQL.Authorization;
using GraphQL.Types;

namespace Mute.Moe.GraphQL.Schema
{
    // ReSharper disable once InconsistentNaming
    public class IRoleSchema
        : ObjectGraphType<IRole>
    {
        public IRoleSchema()
        {
            this.AuthorizeWith("DiscordUser");

            Field("id", ctx => ctx.Id.ToString());

            Field(ctx => ctx.Name);
            Field(ctx => ctx.Position);
            Field(ctx => ctx.IsHoisted);
            Field(ctx => ctx.IsManaged);
            Field(ctx => ctx.IsMentionable);

            Field(typeof(ColorSchema), "color", resolve: ctx => ctx.Source.Color);
        }
    }
}
