namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public class ComponentValidationException : InvalidOperationException
{
    public ComponentValidationException()
    {
    }

    public ComponentValidationException(string? message) : base(message)
    {
    }

    public ComponentValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
