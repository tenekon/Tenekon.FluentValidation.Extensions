using System.Collections;
using System.Runtime.CompilerServices;

using Tenekon.Extensions.FluentValidation.Blazor.Reflection;

namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Maps;

internal static class FieldStateMapAccessLogRegistry
{
    private sealed class AccessLogHolder(IAccessLog accessLog)
    {
        public IAccessLog AccessLog { get; set; } = accessLog;
    }

    private static readonly ConditionalWeakTable<object, AccessLogHolder> s_accessLogs = new();

    public static void Register(IDictionary fieldStateMap, IAccessLog accessLog)
    {
        var accessLogHolder = s_accessLogs.GetValue(fieldStateMap, _ => new AccessLogHolder(accessLog));
        accessLogHolder.AccessLog = accessLog;
    }

    public static bool TryGet(IDictionary fieldStateMap, out IAccessLog accessLog)
    {
        if (s_accessLogs.TryGetValue(fieldStateMap, out var accessLogHolder)) {
            accessLog = accessLogHolder.AccessLog;
            return true;
        }

        accessLog = null!;
        return false;
    }
}
