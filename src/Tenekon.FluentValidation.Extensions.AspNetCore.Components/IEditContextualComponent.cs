using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public interface IEditContextualComponent
{
    IEditContextualComponentState ComponentState { get; }
    EditContext EditContext { get; }
}
