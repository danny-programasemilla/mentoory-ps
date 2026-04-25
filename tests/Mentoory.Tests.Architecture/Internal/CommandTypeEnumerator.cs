using System.Reflection;
using MediatR;

namespace Mentoory.Tests.Architecture.Internal;

/// <summary>
/// Returns every concrete command class across every <c>Mentoory.*.Application</c> assembly.
/// Force-loads the assemblies via touch-type references so <see cref="AppDomain.GetAssemblies"/>
/// finds them; handles <see cref="ReflectionTypeLoadException"/> by filtering out null entries.
/// </summary>
internal static class CommandTypeEnumerator
{
    public static IEnumerable<Type> GetAllCommandTypes()
    {
        _ = new object?[]
        {
            typeof(Mentoory.Access.Application.Commands.AssignRole.AssignRoleCommand),
            typeof(Mentoory.Diagnostic.Application.Commands.CorrectAnswer.CorrectAnswerCommand),
            typeof(Mentoory.Tenant.Application.Commands.AssignMentor.AssignMentorCommand),
            typeof(Mentoory.Shared.Application.DependencyInjection),
        };

        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName is not null
                        && a.FullName.StartsWith("Mentoory.", StringComparison.Ordinal)
                        && a.FullName.Contains(".Application"))
            .SelectMany(SafeGetTypes)
            .Where(t => t is not null)
            .Select(t => t!)
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(IsCommandType);
    }

    public static bool IsCommandType(Type type)
    {
        if (typeof(IBaseRequest).IsAssignableFrom(type))
        {
            return true;
        }

        return type.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
    }

    private static IEnumerable<Type?> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types;
        }
    }
}
