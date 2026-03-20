namespace Tenekon.Extensions.FluentValidation.Blazor;

internal interface IEditModelValidationNotifier
{
    void EvaluateValidationScope(ValidationScopeContext context);
    void NotifyModelValidationRequested(EditModelModelValidationRequestedArgs args);
    void NotifyDirectFieldValidationRequested(EditModelDirectFieldValidationRequestedArgs args);
    void NotifyNestedFieldValidationRequested(EditModelNestedFieldValidationRequestedArgs args);
}
