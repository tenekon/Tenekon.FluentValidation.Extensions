using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.Extensions.FluentValidation.Blazor;

internal interface IEditContextualComponentTrait
{
    EditContext? ActorEditContext { get; }
}
