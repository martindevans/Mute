using System;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.DiceLang.AST;
using Mute.Moe.Utilities;

namespace Mute.Tests;

[TestClass]
public class Playground
{
    [TestMethod]
    public void DateTimeRange()
    {
        var r1 = DateTimeRecognizer.RecognizeDateTime("next week", "en-gb");
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(r1));

        var r2 = FuzzyParsing.MomentRange("next week");
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(r2));

        var r3 = FuzzyParsing.Moment("monday");
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(r3));
    }

    [TestMethod]
    public void JsonRecord()
    {
        var add = new Add(
            new MacroInvoke("ns", "name", [ new ConstantValue(1) ]),
            new Negate(new ConstantValue(2))
        );

        var j1 = JsonSerializer.Serialize(add);
        Console.WriteLine(j1);
    }
}