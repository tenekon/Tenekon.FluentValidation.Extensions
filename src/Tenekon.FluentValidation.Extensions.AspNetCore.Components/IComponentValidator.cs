using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IComponentValidator
{
    bool IsInScope(EditContext editContext);
    void NotifyModelValidationRequested(ComponentValidatorModelValidationRequestedArgs args);
    void NotifyDirectFieldValidationRequested(ComponentValidatorDirectFieldValidationRequestedArgs args);
    void NotifyNestedFieldValidationRequested(ComponentValidatorNestedFieldValidationRequestedArgs args);
}
