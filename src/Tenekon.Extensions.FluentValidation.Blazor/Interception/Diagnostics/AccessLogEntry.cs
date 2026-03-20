namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Diagnostics;

internal static class AccessLogEntry
{
    public static AccessLogEntry<T> Of<T>(T value, AccessLogSubject subject, int indexShift = 0)
        => new(value, subject, IndexShift: indexShift);
}
