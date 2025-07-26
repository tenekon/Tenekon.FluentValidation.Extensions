using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public interface IEditContextualComponentState
{
    IValueState<EditContext> RootEditContext { get; }
    IValueState<EditContext> AncestorEditContext { get; }
    IValueState<EditContext> ActorEditContext { get; }
}
