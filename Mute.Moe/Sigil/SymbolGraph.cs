using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;

namespace Mute.Moe.Sigil
{
    internal class SymbolGraph
    {
        private static readonly JObject SylGraph = BigJsonBlob.SylGraph;

        [NotNull] public static XElement LookupId(string id, string bg, string fg)
        {
            var symbols = (JObject)SylGraph.GetValue("symbols");
            return ToSvgEl((JObject)symbols.GetValue(id), bg, fg);
        }

        [NotNull] private static XElement ToSvgEl([NotNull] JObject obj, string bg, string fg)
        {
            var tag = (string)obj["tag"];

            XElement svg;
            switch (tag)
            {
                case "g":
                    svg = ToSvgGroup(obj, bg, fg);
                    break;
                case "path":
                    svg = ToSvgPath(obj);
                    break;
                default:
                    throw new NotSupportedException($"Unknown tag `{tag}`");
            }

            //Attach attributes
            var attrs = (JObject)obj.GetValue("attr");
            if (attrs != null)
            {
                foreach (var (key, value) in attrs)
                    svg.SetAttributeValue(key, (string)value);
            }

            var meta = (JObject)obj.GetValue("meta");

            //Apply style
            var style = (JObject)meta?.GetValue("style");
            if (style != null)
            {
                var fill = (string)style.GetValue("fill");
                if (fill == "FG")
                    svg.SetAttributeValue("fill", fg);
                else if (fill == "BG")
                    svg.SetAttributeValue("fill", bg);
                else
                    throw new NotSupportedException($"Unsupported fill mode `{fill}`");
            }

            return svg;
        }

        [NotNull] private static XElement ToSvgPath([NotNull] JObject jObject)
        {
            return new XElement(Sigil.SvgNamespace + "path");
        }

        [NotNull] private static XElement ToSvgGroup([NotNull] JObject jObject, string bg, string fg)
        {
            var g = new XElement(Sigil.SvgNamespace + "g");

            var children = (JArray)jObject.GetValue("children");
            foreach (var child in children)
                g.Add(ToSvgEl((JObject)child, bg, fg));

            return g;
        }
    }
}
