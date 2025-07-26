namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public interface IValueState<out T>
{
    T Value { get; }
}
