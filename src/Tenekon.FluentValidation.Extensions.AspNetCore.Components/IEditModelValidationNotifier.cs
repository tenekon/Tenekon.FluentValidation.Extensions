using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IComponentValidationNotifier
{
    bool IsInScope(EditContext candidate);
    void NotifyModelValidationRequested(EditModelValidatorModelValidationRequestedArgs args);
    void NotifyDirectFieldValidationRequested(EditModelValidatorDirectFieldValidationRequestedArgs args);
    void NotifyNestedFieldValidationRequested(EditModelValidatorNestedFieldValidationRequestedArgs args);
}
