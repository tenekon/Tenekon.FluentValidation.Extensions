namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Diagnostics;

internal interface IAccessLogEntry
{
    AccessLogSubject Subject { get; }
    object? UntypedValue { get; }
}
