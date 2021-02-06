using System;
using System.Xml.Linq;

using Newtonsoft.Json.Linq;

namespace Mute.Moe.Sigil
{
    internal class SymbolGraph
    {
        private static readonly JObject SylGraph = BigJsonBlob.SylGraph;

        public static XElement LookupId(string id, string bg, string fg)
        {
            var symbols = (JObject)SylGraph.GetValue("symbols")!;
            return ToSvgEl((JObject)symbols.GetValue(id)!, bg, fg);
        }

        private static XElement ToSvgEl(JObject obj, string bg, string fg)
        {
            var tag = (string)obj["tag"]!;

            XElement svg = tag switch {
                "g" => ToSvgGroup(obj, bg, fg),
                "path" => ToSvgPath(),
                _ => throw new NotSupportedException($"Unknown tag `{tag}`")
            };

            //Attach attributes
            var attrs = (JObject?)obj.GetValue("attr");
            if (attrs != null)
            {
                foreach (var (key, value) in attrs)
                    svg.SetAttributeValue(key, (string)value!);
            }

            var meta = (JObject?)obj.GetValue("meta");

            //Apply style
            var style = (JObject?)meta?.GetValue("style");
            if (style != null)
            {
                var fill = (string)style.GetValue("fill")!;
                if (fill == "FG")
                    svg.SetAttributeValue("fill", fg);
                else if (fill == "BG")
                    svg.SetAttributeValue("fill", bg);
                else
                {
                    //ncrunch: no coverage start
                    throw new NotSupportedException($"Unsupported fill mode `{fill}`");
                    //ncrunch: no coverage end
                }
            }

            return svg;
        }

        private static XElement ToSvgPath()
        {
            return new(Sigil.SvgNamespace + "path");
        }

        private static XElement ToSvgGroup(JObject jObject, string bg, string fg)
        {
            var g = new XElement(Sigil.SvgNamespace + "g");

            var children = (JArray)jObject.GetValue("children")!;
            foreach (var child in children)
                g.Add(ToSvgEl((JObject)child, bg, fg));

            return g;
        }
    }
}
