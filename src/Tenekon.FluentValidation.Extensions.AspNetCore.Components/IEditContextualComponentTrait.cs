using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

public interface IEditContextualComponentTrait
{
    EditContext? OwnEditContext { get; }
}
