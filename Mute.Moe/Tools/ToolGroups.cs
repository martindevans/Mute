namespace Mute.Moe.Tools;

/// <summary>
/// String for tools groups
/// </summary>
public static class ToolGroups
{
    /// <summary>
    /// Information tools
    /// </summary>
    public static class Info
    {
        /// <summary>
        /// Weather related tools group
        /// </summary>
        public static BaseToolGroup Weather { get; } = new ToolGroup("info.weather");
        
        /// <summary>
        /// Time related tools group
        /// </summary>
        public static BaseToolGroup Time { get; } = new ToolGroup("info.time", isDefault: true);

        /// <summary>
        /// Stock market related tools group
        /// </summary>
        public static BaseToolGroup Stocks { get; } = new ToolGroup("info.finance.stocks");

        /// <summary>
        /// Currency related tools group (FOREX, Cryptocurrency)
        /// </summary>
        public static BaseToolGroup Currency { get; } = new ToolGroup("info.finance.currency");

        /// <summary>
        /// Info about users
        /// </summary>
        public static BaseToolGroup Users { get; } = new ToolGroup("info.users");

        /// <summary>
        /// Info about Discord guilds
        /// </summary>
        public static BaseToolGroup Guilds { get; } = new ToolGroup("info.guilds");

        /// <summary>
        /// Info about server status
        /// </summary>
        public static BaseToolGroup SelfStatus { get; } = new ToolGroup("info.self_status");

        /// <summary>
        /// Weather related tools group
        /// </summary>
        public static BaseToolGroup WebSearch { get; } = new ToolGroup("info.web", isDefault: true);

        /// <summary>
        /// Mathematical tools
        /// </summary>
        public static BaseToolGroup Mathematics { get; } = new ToolGroup("info.mathematics", isDefault: true);

        /// <summary>
        /// Mathematical tools
        /// </summary>
        public static BaseToolGroup Weeb { get; } = new ToolGroup("info.weeb");
    }

    /// <summary>
    /// Tools for executing code
    /// </summary>
    public static class CodeExecution
    {
        /// <summary>
        /// Code execution environment
        /// </summary>
        public static BaseToolGroup Python { get; } = new ToolGroup("code_execution.python", isDefault:true);
    }

    /// <summary>
    /// Base class for tool groups
    /// </summary>
    public abstract class BaseToolGroup
    {
        /// <summary>
        /// Indicates if this is a default tool group
        /// </summary>
        public bool IsDefault { get; }
        
        /// <summary>
        /// Group name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Get the name for a tool
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string ToolName(string name)
        {
            return $"{Name}.{name}";
        }

        /// <summary>
        /// Create a new tool group
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isDefault"></param>
        protected BaseToolGroup(string name, bool isDefault = false)
        {
            Name = name;
            IsDefault = isDefault;
        }
    }

    private class ToolGroup(string name, bool isDefault = false) : BaseToolGroup(name, isDefault);
}