using Microsoft.AspNetCore.Components.Forms;

namespace Tenekon.FluentValidation.Extensions.AspNetCore.Components;

internal interface IEditModelValidationNotifier
{
    bool IsInScope(EditContext candidate);
    void NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args);
    void NotifyDirectFieldValidationRequested(EditModelDirectFieldValidationRequestedArgs args);
    void NotifyNestedFieldValidationRequested(EditModelNestedFieldValidationRequestedArgs args);
}
