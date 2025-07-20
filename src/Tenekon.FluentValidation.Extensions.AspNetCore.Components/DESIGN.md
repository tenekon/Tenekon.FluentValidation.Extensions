# Design

<!-- problem:
https://github.com/dotnet/aspnetcore/issues/57573#issuecomment-2744321059 -->

## 💥 Problem Overview

FluentValidation + Blazor = love
but it is difficult as soon you begin split or abstract away blazor components, thus the validation of EditForm (or similiar) can become a quite complex.

## 🔍 Behavior of EditContext

EditForm always implictly cascades an EditContext value (lets call it "edit context A"). That one is always associated with a concrete non-null model (lets call it "model A"), otherwise EditContext would throw anyway.

## 🧠 Field Identifier Semantics

```csharp
record A(string? AB = null)
{
    public string? AB { get; set; } = AB;

    [field: AllowNull]
    [field: MaybeNull]
    public B B { get => field ??= new B(); }
}

record B(string? BA = null)
{
    public string? BA { get; set; } = BA;
}
```

`FieldIdentifier.Create(() => A.AB)`  has Model = A as A and FieldName = "AB" as string;
`FieldIdentifier.Create(() => A.B.BA)`  has Model = B as B and FieldName = "BA" as string;

Two examples of how field identifier is derived from expression.

1. top-level field members

Imaging having A.B, and a validatable component validates against field member A.B.BA, then edit context A will invoke `NotifyFieldChanged(new FieldIdentifier(model: B, fieldName: "BA"))` and triggering a corresponding OnFieldChanged event on edit context A.

2. list field members

Imaging having A.B where A.B is a list of C and you begin to render a validatable component that validates against `A.AB[0]`, then edit context A will invoke `NotifyFieldChanged(new FieldIdentifier(model: C, fieldName: "0"))` and triggering a corresponding OnFieldChanged event on edit context A.

you you cannot simply pass around sub models to sub components and expect that validators registered to edit context A are able to validate against field members of sub models (non top-level field members) nor elements accessed by indexers.

## Existing Solutions (and Flaws)

Luckily we have FluentValidation.

There exists a few established Blazor integrations for FluentValidation:

1. Blazored.FluentValidation
   ❌ literally dysfunctional: not component life cycle aware, memory leaking, no edit context nesting*1
2. Blazorise.FluentValidation
   ⚠️ generalized, complex, very opiniated approaches, no edit context nesting*1
3. vNext.BlazorComponents.FluentValidation
   ✅ solid, but desired default behaviour having AssemblyScannerValidatorFactory as fallback in case of ServiceProviderValidatorFactory results into null-validator?, no edit context nesting*1

*1: "no edit context nesting" means you have to work with one and the same edit context when using the their FluentValidation aware validator component across the wohle component graph beneath (inside ChildContent RenderFragment) the EditForm (or similiar).

## My Proposal

My solution is a more modular approach, where you plug'n'play validators to your behave, a solution that is not too opinionated, so what I ended up with?

1. `ComponentValidatorRootpath`
2. `ComponentValidatorSubpath`
3. `ComponentValidatorRoutes`

each having its own characteristicas.

**`ComponentValidatorRootpath`**

A. It searches for a ancestor edit context of type `EditContext` and does not work without one, thus the validator component must be a child of for example a `EditForm` or `CascadedValue` with an instance of `EditContext`.

B. The ancestor edit context becomes the root edit context if the ancestor edit itself does not have a key-value-pair stored in `Properties` of ancestor edit context that represents the root edit context, otherwise the ancestor edit context will now store itself as property key-value-pair to `Properties`.

C. The ancestor edit context becomes the actor edit context. If using `ChildContent`, then the actor edit context is casdaded via `CascadedValue`, thus it can become the ancestor edit context to child validatable and validating components.

D. The validator component acts on the model validation requests of the ancestor edit context and communicates the model validation results to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.

E. The validator component acts on the field validation requests of the actor edit context and communicates the field validation results to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.

F. If ancestor and actor edit context are not the same instance, then the validator component acts on the field validation requests of the actor edit context and communicates the field validation results to a validator component scoped `ValidationMessageStore` that is attached to the actor edit context.

**`ComponentValidatorSubpath`**

A. It searches for a ancestor edit context of type `EditContext` and does not work without one, thus the validator component must be a child of for example a `EditForm` or `CascadedValue` with an instance of `EditContext`.

B. The ancestor edit context becomes the root edit context if the ancestor edit itself does not have a key-value-pair stored in `Properties` of ancestor edit context that represents the root edit context, otherwise the ancestor edit context will now store itself as property key-value-pair to `Properties`.

C. The ancestor edit context won't becomes the actor edit context, instead a parameter-provided `EditContext` or a new edit context is created from parameter-provided `Model`. If using `ChildContent`, then the actor edit context is casdaded via `CascadedValue`, thus it can become the ancestor edit context to child validatable and validating components.

D. The validator component acts on the model validation requests of the ancestor edit context and communicates the model validation results to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.

E. If ancestor and actor edit context are not the same instance, then the validator component acts on the model validation requests of the actor edit context by delegating it to ancestor edit context and triggering C.

F. The validator component acts on the field validation requests of the actor edit context and communicates the field validation results to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.

G. If ancestor and actor edit context are not the same instance, then the validator component acts on the field validation requests of the actor edit context and communicates the field validation results to a validator component scoped `ValidationMessageStore` that is attached to the actor edit context.

**`ComponentValidatorRoutes`**

A. It searches for a ancestor edit context of type `EditContext` and does not work without one, thus the validator component must be a child of for example a `EditForm` or `CascadedValue` with an instance of `EditContext`.

B. The ancestor edit context becomes the root edit context if the ancestor edit itself does not have a key-value-pair stored in `Properties` of ancestor edit context that represents the root edit context, otherwise the ancestor edit context will now store itself as property key-value-pair to `Properties`.

TODO

<!-- C. The ancestor edit context won't becomes the actor edit context, instead a new edit context is created with the model of the ancestor edit context, then the actor edit context is casdaded via `CascadedValue`, thus it can become the ancestor edit context to child validatable and validating components.

E. If ancestor and actor edit context are not the same instance, then the validator component acts on the model validation requests of the actor edit context by delegating it to ancestor edit context and triggering C.

F. The validator component acts on the field validation requests of the actor edit context and communicates the field validation results to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.

G. If ancestor and actor edit context are not the same instance, then the validator component acts on the field validation requests of the actor edit context and communicates the field validation results to a validator component scoped `ValidationMessageStore` that is attached to the actor edit context. -->