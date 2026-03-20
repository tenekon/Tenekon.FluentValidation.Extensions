namespace Tenekon.Extensions.FluentValidation.Blazor;

public interface IValueState<out T>
{
    T Value { get; }
}
