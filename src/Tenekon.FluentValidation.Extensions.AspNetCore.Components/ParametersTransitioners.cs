namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal static class ParametersTransitioners
{
    public static Action<EditContextualComponentBaseParameterSetTransition> CopyAncestorEditContextFieldReferencesToActorEditContextAction {
        get;
    } = static transition => {
        var ancestor = transition.AncestorEditContext;
        var actor = transition.ActorEditContext;

        // We assume that actor edit context never changes only once at first transition.

        if (ancestor.IsNewDifferent || transition.IsFirstTransition) {
            if (ancestor.IsNewNonNull && actor.IsNewNonNull) {
                /* TODO: We MUST NOT cascade field states and properties, because we do not want to have shared field states,
                 * between different validation contexts, so it becomes like this:
                 * <EditForm ...> Context A
                 *   <EditModelSubpath> // Context B - for demonstation purposes
                 *     <EditModelValidatorRootpath .../> // Writes to A & B
                 *     <EditModelSubpath> // Context C
                 *       <EditModelValidatorRootpath .../> Writes to A & C
                 *     </EditModelSubpath>
                 *   </EditModelSubpath>
                 * </EditForm>
                 */

                // // Cascade EditContext._fieldStates
                // var editContextFieldStatesMemberAccessor = EditContextAccessor.EditContextFieldStatesMemberAccessor;
                // var fieldStates = editContextFieldStatesMemberAccessor.GetValue(ancestor.New);
                // editContextFieldStatesMemberAccessor.SetValue(actor.New, fieldStates);
                //
                // // Cascade EditContext.Properties
                // EditContextAccessor.GetProperties(actor.New) = EditContextAccessor.GetProperties(ancestor.New);

                // Cascade EditContext.Model
                EditContextAccessor.GetModel(actor.New) = EditContextAccessor.GetModel(ancestor.New);

                actor.InvalidateCache();
            }
        }
    };
}
