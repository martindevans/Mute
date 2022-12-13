using System;

namespace Mute.Moe.Discord.Services.Responses.Ellen.Knowledge;

/// <summary>
/// A set of knowledge the bot knows
/// </summary> 
public interface IKnowledge
{
    /// <summary>
    /// The set of knowledge the bot previously knew
    /// </summary>
    IKnowledge? Previous { get; }
}

public static class IKnowledgeExtensions
{
    /// <summary>
    /// Try to retrieve something from the knowledge chain
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="head"></param>
    /// <returns></returns>
    public static T? TryGet<T>(this IKnowledge? head)
        where T : class, IKnowledge
    {
        while (head != null)
        {
            if (head is T t)
                return t;

            head = head.Previous;
        }

        return null;
    }

    /// <summary>
    /// Get an item from the knowledge chain or add it to the end.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="head"></param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public static (T, IKnowledge) GetOrAdd<T>(this IKnowledge? head, Func<IKnowledge?, T> factory)
        where T : class, IKnowledge
    {
        var item = TryGet<T>(head);
        if (item == null)
        {
            item = factory(head);
            return (item, item);
        }

        return (item, head!);
    }

    /// <summary>
    /// Get an item from the knowledge chain or add it to the end.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="head"></param>
    /// <param name="ok">Check if an existing instance is ok, if not a new one will be added to the chain</param>
    /// <param name="factory"></param>
    /// <returns></returns>
    public static (T, IKnowledge) GetOrAdd<T>(this IKnowledge? head, Func<T, bool> ok, Func<IKnowledge?, T> factory)
        where T : class, IKnowledge
    {
        var item = TryGet<T>(head);
        if (item == null || !ok(item))
        {
            item = factory(head);
            return (item, item);
        }

        return (item, head!);
    }
}