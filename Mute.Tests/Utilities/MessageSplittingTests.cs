using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Utilities;

namespace Mute.Tests.Utilities;

[TestClass]
public class MessageSplittingTests
{
    #region Simple paragraphs

    [TestMethod]
    public void SplitIntoParagraphs_SingleLine_ReturnsSingleParagraph()
    {
        var result = MessageSplitting.SplitIntoParagraphs("Hello world");
        CollectionAssert.AreEqual(new[] { "Hello world" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_SeparatedByDoubleNewline_Splits()
    {
        var result = MessageSplitting.SplitIntoParagraphs("First paragraph\n\nSecond paragraph");
        CollectionAssert.AreEqual(new[] { "First paragraph", "Second paragraph" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_SeparatedByDoubleCarriageReturnNewline_Splits()
    {
        var result = MessageSplitting.SplitIntoParagraphs("First paragraph\r\n\r\nSecond paragraph");
        CollectionAssert.AreEqual(new[] { "First paragraph", "Second paragraph" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_MultipleParagraphs_SplitsAll()
    {
        var result = MessageSplitting.SplitIntoParagraphs("One\n\nTwo\n\nThree");
        CollectionAssert.AreEqual(new[] { "One", "Two", "Three" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_MultiLineParagraph_KeepsLinesTogether()
    {
        var result = MessageSplitting.SplitIntoParagraphs("Line one\nLine two\n\nLine three");
        CollectionAssert.AreEqual(new[] { "Line one\nLine two", "Line three" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_EmptyString_ReturnsNoParagraphs()
    {
        var result = MessageSplitting.SplitIntoParagraphs(string.Empty);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void SplitIntoParagraphs_OnlyBlankLines_ReturnsNoParagraphs()
    {
        var result = MessageSplitting.SplitIntoParagraphs("\n\n\n   \n\n");
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void SplitIntoParagraphs_LeadingBlankLines_IgnoresThem()
    {
        var result = MessageSplitting.SplitIntoParagraphs("\n\nHello");
        CollectionAssert.AreEqual(new[] { "Hello" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_TrailingBlankLines_IgnoresThem()
    {
        var result = MessageSplitting.SplitIntoParagraphs("Hello\n\n\n");
        CollectionAssert.AreEqual(new[] { "Hello" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_MoreThanTwoNewlines_TreatsAsOneDelimiter()
    {
        var result = MessageSplitting.SplitIntoParagraphs("One\n\n\n\nTwo");
        CollectionAssert.AreEqual(new[] { "One", "Two" }, result);
    }

    #endregion

    #region Code blocks

    [TestMethod]
    public void SplitIntoParagraphs_CodeBlock_KeptAsSingleString()
    {
        var input = "```csharp\nvar x = 1;\nvar y = 2;\n```";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_CodeBlockWithBlankLineInside_KeptAsSingleString()
    {
        var input = "```csharp\nvar x = 1;\n\nvar y = 2;\n```";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_CodeBlockAttachedToPrecedingText()
    {
        var input = "Here is some code:\n```csharp\nvar x = 1;\n```";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_CodeBlockBetweenParagraphs_SurroundingParagraphsSplit()
    {
        var input = "Before\n\n```csharp\nvar x = 1;\n```\n\nAfter";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "Before", "```csharp\nvar x = 1;\n```", "After" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_TildeCodeBlock_KeptAsSingleString()
    {
        var input = "~~~\ncode line\n\nmore code\n~~~";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_ListLikeLinesInsideCodeBlock_AreNotTreatedAsListItems()
    {
        var input = "```\n- not a list item\n\n1. not a list item either\n```\n\nReal text";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "```\n- not a list item\n\n1. not a list item either\n```", "Real text" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_UnterminatedCodeBlock_KeepsRestOfMessage()
    {
        var input = "```\nvar x = 1;\nmore text\nno closing fence";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_FenceOfDifferentCharacter_DoesNotCloseCodeBlock()
    {
        var input = "```\ncode\n~~~\nstill code\n```";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    #endregion

    #region Bullet lists

    [TestMethod]
    public void SplitIntoParagraphs_BulletList_KeptAsSingleString()
    {
        var input = "- First item\n- Second item\n- Third item";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_AsteriskBulletList_KeptAsSingleString()
    {
        var input = "* First item\n* Second item";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_BulletListAttachedToPrecedingText()
    {
        var input = "Here are the steps:\n- First step\n- Second step";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_BulletListWithBlankLinesBetweenItems_KeptAsSingleString()
    {
        var input = "- First item\n\n- Second item\n\n- Third item";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_BulletListFollowedByNonListLine_SplitsAfterList()
    {
        var input = "- First item\n- Second item\n\nSome other text";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "- First item\n- Second item", "Some other text" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_BulletListAfterBlankLine_IsSeparateParagraph()
    {
        var input = "Some text\n\n- First item\n- Second item";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "Some text", "- First item\n- Second item" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_IndentedBulletList_KeptAsSingleString()
    {
        var input = "Here are the steps:\n   - First step\n   - Second step";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_IndentedMoreThanThreeSpaces_NotTreatedAsListItem()
    {
        var input = "- First item\n    indented continuation\n\nNext paragraph";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "- First item\n    indented continuation", "Next paragraph" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_LineStartingWithAsteriskEmphasis_NotTreatedAsListItem()
    {
        var input = "*emphasis* at the start\n\nSecond paragraph";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "*emphasis* at the start", "Second paragraph" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_LineStartingWithHyphenatedWord_NotTreatedAsListItem()
    {
        var input = "well-known fact\n\nSecond paragraph";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "well-known fact", "Second paragraph" }, result);
    }

    #endregion

    #region Numbered lists

    [TestMethod]
    public void SplitIntoParagraphs_NumberedList_KeptAsSingleString()
    {
        var input = "1. First step\n2. Second step\n3. Third step";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_NumberedListWithParentheses_KeptAsSingleString()
    {
        var input = "1) First step\n2) Second step";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_NumberedListAttachedToPrecedingText()
    {
        var input = "Here are the steps:\n1. First step\n2. Second step";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_NumberedListWithBlankLinesBetweenItems_KeptAsSingleString()
    {
        var input = "1. First step\n\n2. Second step\n\n3. Third step";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { input }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_NumberedListFollowedByNonListLine_SplitsAfterList()
    {
        var input = "1. First step\n2. Second step\n\nSome other text";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "1. First step\n2. Second step", "Some other text" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_LineStartingWithYear_NotTreatedAsListItem()
    {
        var input = "1985 was a great year for games\n\nSecond paragraph";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "1985 was a great year for games", "Second paragraph" }, result);
    }

    #endregion

    #region Mixed content

    [TestMethod]
    public void SplitIntoParagraphs_MixedContent_SplitsCorrectly()
    {
        var input = "Intro text.\n\n1. First step\n2. Second step\n\nHere is some code:\n```csharp\nvar x = 1;\n```\n\n- A list\n- Another item\n\nOutro text.";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[]
        {
            "Intro text.",
            "1. First step\n2. Second step",
            "Here is some code:\n```csharp\nvar x = 1;\n```",
            "- A list\n- Another item",
            "Outro text."
        }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_ListThenParagraphThenList_SplitsBetween()
    {
        var input = "- a\n- b\n\nSome text\n\n- c\n- d";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "- a\n- b", "Some text", "- c\n- d" }, result);
    }

    [TestMethod]
    public void SplitIntoParagraphs_CarriageReturnsInAllConstructs_SplitsCorrectly()
    {
        var input = "Intro.\r\n\r\n1. First\r\n2. Second\r\n\r\nOutro.";
        var result = MessageSplitting.SplitIntoParagraphs(input);
        CollectionAssert.AreEqual(new[] { "Intro.", "1. First\n2. Second", "Outro." }, result);
    }

    #endregion

    #region MergeParagraphs

    [TestMethod]
    public void MergeParagraphs_AllFit_MergesIntoSingleString()
    {
        var result = MessageSplitting.MergeParagraphs(new List<string> { "One", "Two", "Three" }, 100);
        CollectionAssert.AreEqual(new[] { "One\n\nTwo\n\nThree" }, result);
    }

    [TestMethod]
    public void MergeParagraphs_SingleParagraph_ReturnsSingleString()
    {
        var result = MessageSplitting.MergeParagraphs(new List<string> { "Hello" }, 100);
        CollectionAssert.AreEqual(new[] { "Hello" }, result);
    }

    [TestMethod]
    public void MergeParagraphs_EmptyList_ReturnsEmptyList()
    {
        var result = MessageSplitting.MergeParagraphs(new List<string>(), 100);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void MergeParagraphs_ExceedsMaxLength_SplitsIntoMultipleStrings()
    {
        // "One" + 2 + "Two" = 8 fits, but adding "Three" would be 15 which exceeds 10
        var result = MessageSplitting.MergeParagraphs(new List<string> { "One", "Two", "Three" }, 10);
        CollectionAssert.AreEqual(new[] { "One\n\nTwo", "Three" }, result);
    }

    [TestMethod]
    public void MergeParagraphs_ExactFit_Merges()
    {
        // "One" + 2 + "Two" = 8, which exactly fits
        var result = MessageSplitting.MergeParagraphs(new List<string> { "One", "Two" }, 8);
        CollectionAssert.AreEqual(new[] { "One\n\nTwo" }, result);
    }

    [TestMethod]
    public void MergeParagraphs_ParagraphExceedsMaxLength_KeptOnItsOwn()
    {
        var longParagraph = new string('a', 100);
        var result = MessageSplitting.MergeParagraphs(new List<string> { longParagraph, "Small" }, 50);
        CollectionAssert.AreEqual(new[] { longParagraph, "Small" }, result);
    }

    [TestMethod]
    public void MergeParagraphs_FirstParagraphExceedsMaxLength_DoesNotEmitEmptyString()
    {
        var longParagraph = new string('a', 100);
        var result = MessageSplitting.MergeParagraphs(new List<string> { longParagraph }, 50);
        CollectionAssert.AreEqual(new[] { longParagraph }, result);
    }

    [TestMethod]
    public void MergeParagraphs_EachResultIsWithinMaxLength()
    {
        var paragraphs = new List<string> { "aaa", "bb", "c", "dddd", "e" };
        var result = MessageSplitting.MergeParagraphs(paragraphs, 10);
        CollectionAssert.AreEqual(new[] { "aaa\n\nbb\n\nc", "dddd\n\ne" }, result);
        Assert.IsTrue(result.All(p => p.Length <= 10));
    }

    [TestMethod]
    public void MergeParagraphs_RoundTrip_ReturnsOriginalMessage()
    {
        var message = "First paragraph.\n\nSecond paragraph is a bit longer than the first.\n\n1. One\n2. Two\n3. Three";
        var split = MessageSplitting.SplitIntoParagraphs(message);
        var merged = MessageSplitting.MergeParagraphs(split, 10_000);
        CollectionAssert.AreEqual(new[] { message }, merged);
    }

    [TestMethod]
    public void MergeParagraphs_SplitThenMergeWithDiscordLimit_RoundTrips()
    {
        var message = string.Join("\n\n", Enumerable.Range(0, 20).Select(i => $"Paragraph number {i} is here to take up some space."));
        var split = MessageSplitting.SplitIntoParagraphs(message);
        var merged = MessageSplitting.MergeParagraphs(split, 2000);

        Assert.IsTrue(merged.All(p => p.Length <= 2000));
        CollectionAssert.AreEqual(split, merged.SelectMany(p => MessageSplitting.SplitIntoParagraphs(p)).ToList());
    }

    #endregion
}
