namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Diagnostics;

internal interface IAccessLogger
{
    void LogAccess<T>(AccessLogEntry<T> accessLogEntry);
}
