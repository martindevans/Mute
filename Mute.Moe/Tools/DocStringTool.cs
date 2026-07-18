using HandyAgentFramework;
using Microsoft.Extensions.AI;
using System.Reflection;

namespace Mute.Moe.Tools;

/// <summary>
/// Create AIFunction automatically, extracting method and parameter descriptions from XML doc comment strings
/// </summary>
public class DocStringTool
{
    private readonly string _name;
    private readonly string _group;
    private readonly Delegate _action;
    private readonly bool _default;

    /// <summary>
    /// Create a new DocStringTool
    /// </summary>
    /// <param name="name">Name for the function</param>
    /// <param name="group">The group of this function</param>
    /// <param name="action">Action to execute, XML doc strings will be fetched from this</param>
    /// <param name="default">Indicates if this is a default function</param>
    public DocStringTool(string name, string group, Delegate action, bool @default = false)
    {
        _name = name;
        _group = group;
        _action = action;
        _default = @default;
    }

    /// <summary>
    /// Create a new DocStringTool, deriving name and group from a tool group object
    /// </summary>
    /// <param name="group"></param>
    /// <param name="name"></param>
    /// <param name="action"></param>
    public DocStringTool(ToolGroups.BaseToolGroup group, string name, Delegate action)
        : this(group.ToolName(name), group.Name, action, group.IsDefault)
    {
    }
    
    /// <summary>
    /// Build the AI function
    /// </summary>
    /// <returns></returns>
    public ToolDefinition Build()
    {
        var func = AIFunctionFactory.Create(_action, new AIFunctionFactoryOptions
        {
            Name = _name,
            Description = _action.GetMethodInfo().GetDocumentation(),
            
            JsonSchemaCreateOptions = new AIJsonSchemaCreateOptions
            {
                ParameterDescriptionProvider = arg => arg.GetDocumentation(),
            }
        });

        return new ToolDefinition(
            func,
            _default,
            _group
        );
    }
    
    /// <summary>
    /// Implicitly run Build
    /// </summary>
    /// <param name="docTool"></param>
    /// <returns></returns>
    public static implicit operator ToolDefinition(DocStringTool docTool) => docTool.Build();
}