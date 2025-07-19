using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public sealed class ValidatorHandle(IValidator validator, AsyncServiceScope? serviceScope) : IAsyncDisposable, IDisposable
{
    public IValidator Validator { get; } = validator;

    public void Dispose() => serviceScope?.Dispose();

    public ValueTask DisposeAsync()
    {
        if (!serviceScope.HasValue) {
            return ValueTask.CompletedTask;
        }

        return serviceScope.Value.DisposeAsync();
    }
}
