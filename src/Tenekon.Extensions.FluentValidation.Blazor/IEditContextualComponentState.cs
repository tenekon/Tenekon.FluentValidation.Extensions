using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor;

public interface IEditContextualComponentState
{
    IValueState<EditContext> RootEditContext { get; }
    IValueState<EditContext> AncestorEditContext { get; }
    IValueState<EditContext> ActorEditContext { get; }
}
