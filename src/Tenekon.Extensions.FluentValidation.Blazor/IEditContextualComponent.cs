using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor;

public interface IEditContextualComponent
{
    IEditContextualComponentState ComponentState { get; }
    EditContext EditContext { get; }
}
