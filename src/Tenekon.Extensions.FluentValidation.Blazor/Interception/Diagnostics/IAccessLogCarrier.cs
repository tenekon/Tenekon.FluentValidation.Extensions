namespace Tenekon.Extensions.FluentValidation.Blazor.Interception.Diagnostics;

internal interface IAccessLogCarrier
{
    IAccessLogger? AccessLogger { get; }
}
