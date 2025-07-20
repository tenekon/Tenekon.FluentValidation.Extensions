using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public interface IComponentValidatorSubpathTrait
{
    public static readonly Func<ErrorContext, Exception> DefaultExceptionFactory = DefaultExceptionFactoryImpl;

    private static Exception DefaultExceptionFactoryImpl(ErrorContext errorContext) =>
        errorContext.Identifier switch {
            ErrorIdentifier.ActorEditContextAndModel => new InvalidOperationException(
                $"{errorContext.Provocateur?.GetType().Name} requires a non-null {nameof(Model)} property or a non-null {nameof(ActorEditContext)} property, but not both."),
            ErrorIdentifier.NoActorEditContextAndNoModel => new InvalidOperationException(
                $"{errorContext.Provocateur?.GetType().Name} requires either a non-null {nameof(Model)} property or a non-null {nameof(ActorEditContext)} property."),
            _ => new Exception()
        };

    Func<ErrorContext, Exception> ExceptionFactory => DefaultExceptionFactory;

    /// <summary>
    /// Indicates having non-null <see cref="ActorEditContext"/> parameter.
    /// </summary>
    [MemberNotNullWhen(true, nameof(ActorEditContext))]
    bool HasActorEditContextBeenSetExplicitly { get; set; }

    EditContext? ActorEditContext { get; set; }
    object? Model { get; set; }

    /// <summary>
    /// Sets <see cref="ActorEditContext"/> to <paramref name="editContext"/>.
    /// <see cref="HasActorEditContextBeenSetExplicitly"/> is set to <c>true</c> if <paramref name="editContext"/> is not <c>null</c>, otherwise <c>false</c>.
    /// </summary>
    /// <param name="editContext">The edit context to set.</param>
    void SetActorEditContextExplicitly(EditContext? editContext)
    {
        ActorEditContext = editContext;
        HasActorEditContextBeenSetExplicitly = editContext is not null;
    }

    Task OnSubpathParametersSetAsync()
    {
        if (HasActorEditContextBeenSetExplicitly && Model is not null) {
            // We have ActorEditContext and Model
            throw ExceptionFactory(new ErrorContext(this, ErrorIdentifier.ActorEditContextAndModel));
        }

        if (!HasActorEditContextBeenSetExplicitly && Model is null) {
            // We have no ActorEditContext and no Model
            throw ExceptionFactory(new ErrorContext(this, ErrorIdentifier.NoActorEditContextAndNoModel));
        }

        // Re-assign only if Model is not null and either ActorEditContext is null or Model differs from ActorEditContext.Model,
        // or in other words: do not re-assign if Model is null or equal to the current ActorEditContext.Model.
        if (Model is not null && !ReferenceEquals(Model, ActorEditContext?.Model)) {
            ActorEditContext = new EditContext(Model!);
        }
        
        return Task.CompletedTask;
    }

    public sealed class ErrorContext(object? provocateur, ErrorIdentifier identifier)
    {
        public object? Provocateur { get; } = provocateur;
        public ErrorIdentifier Identifier { get; } = identifier;
    }

    public enum ErrorIdentifier
    {
        ActorEditContextAndModel,
        NoActorEditContextAndNoModel
    }
}
