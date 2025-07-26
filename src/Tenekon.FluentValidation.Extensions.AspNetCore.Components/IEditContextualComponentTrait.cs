using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IEditContextualComponentTrait
{
    EditContext? ActorEditContext { get; }
}
