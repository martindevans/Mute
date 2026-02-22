using BalderHash.Extensions;
using FuzzySharp.PreProcess;
using FuzzySharp.SimilarityRatio.Scorer.Composite;
using Mute.Moe.Services.Database;
using Mute.Moe.Utilities;
using System.Text;

namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// Stores agent written profiles for individual users
/// </summary>
public class AgentDomainDocumentStorage
    : SimpleJsonBlobTable<AgentDomainDocument>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="database"></param>
    public AgentDomainDocumentStorage(IDatabaseService database)
        : base("AgentDomainDocumentStorage", database)
    {
    }
}

/// <summary>
/// A profile of a user written by an agent
/// </summary>
public record AgentDomainDocument
{
    /// <summary>
    /// Root sections of this document
    /// </summary>
    public required List<AgentDomainDocumentSection> Sections { get; init; }

    #region ToString
    /// <summary>
    /// Render this domaindoc as a markdown document
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return ToString(expanded:null);
    }

    /// <summary>
    /// Render this domaindoc as a markdown document
    /// </summary>
    /// <param name="expanded">Set of expanded sections, if null all sections are considered to be expanded</param>
    public string ToString(IReadOnlySet<ushort>? expanded = null)
    {
        var builder = new StringBuilder();
        foreach (var section in Sections)
        {
            section.ToStringBuilder(builder, 1, expanded);
            builder.AppendLine();
        }
        return builder.ToString();
    }
    #endregion

    /// <summary>
    /// Recursively get all sections
    /// </summary>
    /// <param name="expanded">Only include expanded sections</param>
    public IEnumerable<AgentDomainDocumentSection> GetDescendants(IReadOnlySet<ushort>? expanded = null)
    {
        foreach (var section in Sections)
        {
            if (expanded == null || expanded.Contains(section.ID))
            {
                yield return section;

                foreach (var innerSection in section.GetDescendants(expanded))
                    yield return innerSection;
            }
        }
    }

    #region search
    /// <summary>
    /// Search expanded sections of the domain doc
    /// </summary>
    /// <param name="query"></param>
    /// <param name="limit"></param>
    /// <param name="expanded">Only search within expanded sections, null collection is equivalent to expanding all sections</param>
    public IReadOnlyList<(int Relevance, string Title, string ID)> Search(string query, int limit = 5, IReadOnlySet<ushort>? expanded = null)
    {
        var scorer = new WeightedRatioScorer();
        var items = new PriorityQueue<AgentDomainDocumentSection, int>();

        // Get the N best values by score
        foreach (var section in GetDescendants(expanded))
        {
            var score = scorer.Score(query, section.Content, PreprocessMode.Full);
            items.Enqueue(section, score);

            while (items.Count > limit)
                items.Dequeue();
        }

        return (
            from item in items.UnorderedItems
            orderby item.Priority descending
            select (Relevance: item.Priority, Title: item.Element.Title, ID: item.Element.ID.BalderHash())
        ).ToArray();
    }
    #endregion

    #region open/close
    /// <summary>
    /// Try to expand a section
    /// </summary>
    public ExpandResult Open(ushort id, ISet<ushort> expanded)
    {
        foreach (var section in Sections)
        {
            var result = section.Open(id, expanded);
            if (result.HasValue)
                return result.Value;
        }

        return ExpandResult.SectionNotFound;
    }

    /// <summary>
    /// Results from calling <see cref="AgentDomainDocument.Open"/>
    /// </summary>
    public enum ExpandResult
    {
        /// <summary>
        /// Section was expanded
        /// </summary>
        Ok,

        /// <summary>
        /// Could not find a section with this ID
        /// </summary>
        SectionNotFound,
    }

    /// <summary>
    /// Try to collapse a section (auto collapses all descendants)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="expanded"></param>
    /// <returns></returns>
    public CollapseResult Close(ushort id, ISet<ushort> expanded)
    {
        foreach (var section in Sections)
        {
            var result = section.Close(id, expanded);
            if (result.HasValue)
                return result.Value;
        }

        return CollapseResult.SectionNotFound;
    }

    /// <summary>
    /// Results from calling <see cref="AgentDomainDocument.Close"/>
    /// </summary>
    public enum CollapseResult
    {
        /// <summary>
        /// Section was folded
        /// </summary>
        Ok,

        /// <summary>
        /// Could not find a section with this ID
        /// </summary>
        SectionNotFound,
    }
    #endregion

    #region edit
    /// <summary>
    /// Within the given section, replace `from` with `to`
    /// </summary>
    /// <param name="id"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="expanded"></param>
    /// <param name="aiEdit">Indicates if this is an AI edit</param>
    /// <returns></returns>
    public ReplaceResult Replace(ushort id, string from, string to, IReadOnlySet<ushort>? expanded, bool aiEdit)
    {
        foreach (var section in Sections)
        {
            var result = section.Replace(id, from, to, expanded, aiEdit);

            if (result.HasValue)
                return result.Value;
        }

        return ReplaceResult.SectionNotFound;
    }

    /// <summary>
    /// Possible results from calling <see cref="Replace"/>
    /// </summary>
    public enum ReplaceResult
    {
        /// <summary>
        /// Edit applied successfully
        /// </summary>
        Ok,

        /// <summary>
        /// Could not find a section with the given ID
        /// </summary>
        SectionNotFound,

        /// <summary>
        /// The section was found, but it did not contain the given text
        /// </summary>
        TextNotFound,

        /// <summary>
        /// Section and text was found, but the from text exists multiple times
        /// </summary>
        TextNotUnique,

        /// <summary>
        /// The section was found, but is not editable
        /// </summary>
        SectionNotEditable,
    }

    /// <summary>
    /// Set the short summary for a section
    /// </summary>
    /// <param name="id"></param>
    /// <param name="summary"></param>
    public SetSummaryResult SetSummary(ushort id, string summary)
    {
        foreach (var section in Sections)
        {
            var result = section.SetSummary(id, summary);

            if (result.HasValue)
                return result.Value;
        }

        return SetSummaryResult.SectionNotFound;
    }

    /// <summary>
    /// Possible results from calling <see cref="SetSummary"/>
    /// </summary>
    public enum SetSummaryResult
    {
        /// <summary>
        /// summary set successfully
        /// </summary>
        Ok,

        /// <summary>
        /// Could not find a section with the given ID
        /// </summary>
        SectionNotFound
    }
    #endregion
}

/// <summary>
/// A section of
/// </summary>
public record AgentDomainDocumentSection
{
    /// <summary>
    /// Unique ID of this section
    /// </summary>
    public required ushort ID { get; set; }

    /// <summary>
    /// The title of this section
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// The text content of this section
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// The text summarising the content of this section
    /// </summary>
    public required string Summary { get; set; }

    /// <summary>
    /// Indicates if the agent may edit this section
    /// </summary>
    public bool AgentEditable { get; set; }

    /// <summary>
    /// Indicates if this section was orignally created by the agent
    /// </summary>
    public bool AgentCreated { get; set; }

    /// <summary>
    /// All edits ever applied to this content
    /// </summary>
    public required List<StringEdit> Edits { get; set; }

    /// <summary>
    /// Subsections of this section
    /// </summary>
    public required List<AgentDomainDocumentSection> Subsections { get; init; }

    /// <summary>
    /// Indicates if this section may be closed
    /// </summary>
    public bool IsClosable { get; set; }

    /// <summary>
    /// Append this section to a string builder
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="depth"></param>
    /// <param name="open">Expanded sections</param>
    public void ToStringBuilder(StringBuilder builder, int depth, IReadOnlySet<ushort>? open)
    {
        // Title is:
        // ## Title (🔒) {#section-id}

        // ####
        builder.Append('#', depth);
        builder.Append(' ');

        // Title
        builder.Append(Title);

        // Lock emoji
        if (!AgentEditable)
            builder.Append(" (🔒)");

        // ID
        builder.Append(" {#");
        builder.Append(ID.BalderHash());
        builder.Append('}');

        // Space after title to content
        builder.AppendLine();
        builder.AppendLine();

        // Check if this section is folded away
        var isFolded = IsClosable && !(open?.Contains(ID) ?? true);
        if (isFolded)
        {
            // Summary
            builder.AppendLine($"Section closed, summary: {Summary}");
        }
        else
        {
            // Content
            builder.AppendLine(Content);

            // Subsections
            if (Subsections.Count > 0)
            {
                builder.AppendLine();

                // Write out subsections
                foreach (var subsection in Subsections)
                {
                    subsection.ToStringBuilder(builder, depth + 1, open);
                    builder.AppendLine();
                }
            }
        }
    }

    /// <summary>
    /// Recursively get all sections
    /// </summary>
    /// <param name="open"></param>
    public IEnumerable<AgentDomainDocumentSection> GetDescendants(IReadOnlySet<ushort>? open = null)
    {
        foreach (var section in Subsections)
        {
            if (open == null || open.Contains(section.ID))
            {
                yield return section;

                foreach (var innerSection in section.GetDescendants(open))
                    yield return innerSection;
            }
        }
    }

    /// <summary>
    /// Open the section with the given ID, if it is a subsection all ancestors are also opened
    /// </summary>
    /// <param name="id"></param>
    /// <param name="open"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public AgentDomainDocument.ExpandResult? Open(ushort id, ISet<ushort> open)
    {
        // If it's this section expand immediately
        if (id == ID)
        {
            open.Add(ID);
            return AgentDomainDocument.ExpandResult.Ok;
        }

        // Check all descendants, if it's one of them expand this section
        foreach (var section in Subsections)
        {
            var result = section.Open(id, open);
            if (result == AgentDomainDocument.ExpandResult.Ok)
            {
                open.Add(ID);
                return AgentDomainDocument.ExpandResult.Ok;
            }
        }

        // Failed to find anything
        return null;
    }

    /// <summary>
    /// Close the section with the given ID and all subsections
    /// </summary>
    /// <param name="id"></param>
    /// <param name="open"></param>
    /// <returns></returns>
    public AgentDomainDocument.CollapseResult? Close(ushort id, ISet<ushort> open)
    {
        // If it's this section collapse immediately
        if (id == ID)
        {
            CloseAll(open);
            return AgentDomainDocument.CollapseResult.Ok;
        }

        // Search each subtree
        foreach (var section in Subsections)
        {
            var result = section.Close(id, open);
            if (result.HasValue)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Close this section and all descendants
    /// </summary>
    /// <param name="open"></param>
    private void CloseAll(ISet<ushort> open)
    {
        open.Remove(ID);

        foreach (var section in Subsections)
            section.CloseAll(open);
    }

    /// <summary>
    /// Within the given section, replace `from` with `to`
    /// </summary>
    /// <param name="id"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="expanded"></param>
    /// <param name="aiEdit"></param>
    /// <returns></returns>
    public AgentDomainDocument.ReplaceResult? Replace(ushort id, string from, string to, IReadOnlySet<ushort>? expanded, bool aiEdit)
    {
        // Not need to search if this section is closed
        if (expanded != null && !expanded.Contains(ID))
            return null;

        if (id == ID)
        {
            // Check if AI edits are allowed
            if (aiEdit && !AgentEditable)
                return AgentDomainDocument.ReplaceResult.SectionNotEditable;

            // Do the replacement
            var c = Content.ReplaceWithCount(from, to, out var replaced);

            // Check if it was valid (exactly one substring replaced)
            switch (replaced)
            {
                case 1:
                    Content = c;
                    return AgentDomainDocument.ReplaceResult.Ok;
                case 0:
                    return AgentDomainDocument.ReplaceResult.TextNotFound;
                default:
                    return AgentDomainDocument.ReplaceResult.TextNotUnique;
            }
        }
        else
        {
            foreach (var section in Subsections)
            {
                var result = section.Replace(id, from, to, expanded, aiEdit);
                if (result.HasValue)
                    return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Replace the summary for the section with the given ID
    /// </summary>
    /// <param name="id"></param>
    /// <param name="summary"></param>
    /// <returns></returns>
    public AgentDomainDocument.SetSummaryResult? SetSummary(ushort id, string summary)
    {
        if (id == ID)
        {
            Summary = summary;
            return AgentDomainDocument.SetSummaryResult.Ok;
        }
        else
        {
            foreach (var section in Subsections)
            {
                var result = section.SetSummary(id, summary);
                if (result.HasValue)
                    return result;
            }
        }

        return null;
    }
}