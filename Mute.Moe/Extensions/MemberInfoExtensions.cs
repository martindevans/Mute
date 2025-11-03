using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Mute.Moe.Extensions;

/// <summary>
/// Get XML doc strings for members
/// </summary>
public static class MemberInfoExtensions
{
    /// <summary>
    /// A cache to store loaded XML documentation files.
    /// </summary>
    private static readonly Dictionary<string, XDocument?> DocumentationCache = new();

    /// <summary>
    /// Gets the documentation summary for a member.
    /// </summary>
    /// <param name="memberInfo">The MemberInfo to get documentation for.</param>
    /// <returns>The summary documentation, or null if not found.</returns>
    public static string? GetDocumentation(this MemberInfo memberInfo)
    {
        return GetMemberXmlElement(memberInfo)
             ?.Element("summary")
             ?.Value
              .Trim();
    }

    /// <summary>
    /// Gets the documentation for a method parameter.
    /// </summary>
    /// <param name="parameterInfo">The ParameterInfo to get documentation for.</param>
    /// <returns>The parameter's documentation, or null if not found.</returns>
    public static string? GetDocumentation(this ParameterInfo parameterInfo)
    {
        return GetMemberXmlElement((MethodInfo)parameterInfo.Member)
             ?.Elements("param")
              .FirstOrDefault(p => p.Attribute("name")?.Value == parameterInfo.Name)
             ?.Value
              .Trim();
    }

    private static XElement? GetMemberXmlElement(MemberInfo? memberInfo)
    {
        if (memberInfo == null)
            return null;

        var memberName = GetMemberXmlName(memberInfo);
        if (string.IsNullOrEmpty(memberName))
            return null;

        var assembly = memberInfo.Module.Assembly;

        var assemblyName = assembly.FullName;
        if (string.IsNullOrEmpty(assemblyName))
            return null;

        if (!DocumentationCache.TryGetValue(assemblyName, out var xmlDoc))
        {
            var xmlDocumentationPath = GetXmlDocumentationPath(assembly);
            xmlDoc = File.Exists(xmlDocumentationPath)
                   ? XDocument.Load(xmlDocumentationPath)
                   : null;

            DocumentationCache.Add(assemblyName, xmlDoc);
        }

        if (xmlDoc == null)
            return null;

        // Find the <member> element
        var memberElement = xmlDoc.XPathSelectElement($"/doc/members/member[@name='{memberName}']");

        // Check for <inheritdoc /> and search base types if found
        if (memberElement?.Element("inheritdoc") != null)
            return FindInheritedDocumentation(memberInfo);

        return memberElement;
    }

    private static XElement? FindInheritedDocumentation(MemberInfo memberInfo)
    {
        if (memberInfo is not MethodInfo methodInfo)
            return null;

        // Get the base method (if it's an override)
        var baseMethod = methodInfo.GetBaseDefinition();

        // If GetBaseDefinition returns the same method, it's not a direct override,
        // so we need to check interfaces.
        if (baseMethod != methodInfo)
        {
            var baseElement = GetMemberXmlElement(baseMethod);
            if (baseElement != null)
                return baseElement;
        }

        // If it's not an override or base documentation was not found, check implemented interfaces
        var declaringType = methodInfo.DeclaringType;
        if (declaringType == null)
            return null;

        var implementedInterfaces = declaringType.GetInterfaces();
        foreach (var interfaceType in implementedInterfaces)
        {
            // Find the corresponding method on the interface
            var interfaceMethod = interfaceType.GetMethod(
                methodInfo.Name,
                methodInfo.GetParameters().Select(p => p.ParameterType).ToArray()
            );

            if (interfaceMethod != null)
            {
                var interfaceElement = GetMemberXmlElement(interfaceMethod);
                if (interfaceElement != null && interfaceElement.Element("inheritdoc") == null)
                {
                    return interfaceElement;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Generates the unique "name" attribute for a member as it appears in the XML documentation file.
    /// </summary>
    private static string GetMemberXmlName(MemberInfo memberInfo)
    {
        var memberName = new StringBuilder();
        switch (memberInfo.MemberType)
        {
            case MemberTypes.Method:
                var methodInfo = (MethodInfo)memberInfo;
                memberName.Append($"M:{methodInfo.DeclaringType?.FullName}.{methodInfo.Name}");
                var parameters = methodInfo.GetParameters();
                if (parameters.Length > 0)
                {
                    memberName.Append('(');
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        memberName.Append(parameters[i].ParameterType.FullName);
                        if (i < parameters.Length - 1)
                            memberName.Append(',');
                    }
                    memberName.Append(')');
                }
                break;

            case MemberTypes.Constructor:
            case MemberTypes.Event:
            case MemberTypes.Field:
            case MemberTypes.Property:
            case MemberTypes.TypeInfo:
            case MemberTypes.Custom:
            case MemberTypes.NestedType:
            case MemberTypes.All:
            default:
                break;
        }

        // The XML documentation file uses '.' for nested classes, while reflection uses '+'.
        return memberName.ToString().Replace('+', '.');
    }

    /// <summary>
    /// Gets the path to the XML documentation file for a given assembly.
    /// </summary>
    private static string? GetXmlDocumentationPath(Assembly assembly)
    {
        var assemblyLocation = assembly.Location;
        if (string.IsNullOrEmpty(assemblyLocation))
            return null;

        return Path.ChangeExtension(assemblyLocation, ".xml");
    }
}