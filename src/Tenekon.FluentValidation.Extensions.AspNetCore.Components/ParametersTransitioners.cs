using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

file static class EditContextAccessor
{
    private const string EditContextFieldStatesFieldName = "_fieldStates";

    // We cannot use UnsafeAccessor and must work with reflection because part of the targeting signature is internal. :/
    [field: AllowNull]
    [field: MaybeNull]
    public static FieldInfo EditContextFieldStatesMemberAccessor =>
        field ??= typeof(EditContext).GetField(EditContextFieldStatesFieldName, BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new NotImplementedException(
                $"{nameof(EditContext)} does not implement the {EditContextFieldStatesFieldName} field anymore.");

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Properties>k__BackingField")]
    public static extern ref EditContextProperties GetProperties(EditContext editContext);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<Model>k__BackingField")]
    public static extern ref object GetModel(EditContext editContext);
}

internal static class ParametersTransitioners
{
    public static Action<EditContextualComponentBaseParameterSetTransition> CopyAncestorEditContextFieldReferencesToActorEditContextAction {
        get;
    } = static (transition) => {
        var ancestor = transition.AncestorEditContext;
        var actor = transition.ActorEditContext;

        // We assume that actor edit context never changes only once at first transition.

        if (ancestor.IsNewDifferent || transition.IsFirstTransition) {
            if (ancestor.IsNewNonNull && actor.IsNewNonNull) {
                // Cascade EditContext._fieldStates
                var editContextFieldStatesMemberAccessor = EditContextAccessor.EditContextFieldStatesMemberAccessor;
                var fieldStates = editContextFieldStatesMemberAccessor.GetValue(ancestor.New);
                editContextFieldStatesMemberAccessor.SetValue(actor.New, fieldStates);

                // Cascade EditContext.Properties
                EditContextAccessor.GetProperties(actor.New) = EditContextAccessor.GetProperties(ancestor.New);

                // Cascade EditContext.Model
                EditContextAccessor.GetModel(actor.New) = EditContextAccessor.GetModel(ancestor.New);

                actor.InvalidateCache();
            }
        }
    };
}
