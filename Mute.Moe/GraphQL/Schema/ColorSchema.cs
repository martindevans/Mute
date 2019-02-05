using Discord;
using GraphQL.Types;

namespace Mute.Moe.GraphQL.Schema
{
    public class ColorSchema
        : ObjectGraphType<Color>
    {
        public ColorSchema()
        {
            Field(typeof(IntGraphType), "R", resolve: a => a.Source.R);
            Field(typeof(IntGraphType), "G", resolve: a => a.Source.G);
            Field(typeof(IntGraphType), "B", resolve: a => a.Source.B);

            Field(typeof(StringGraphType), "hex", resolve: a => $"#{a.Source.R:X2}{a.Source.G:X2}{a.Source.B:X2}");
        }
    }
}
