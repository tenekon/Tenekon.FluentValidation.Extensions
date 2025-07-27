<!-- omit from toc -->
# :open_book: Validator Components Architecture [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

<!-- problem:
https://github.com/dotnet/aspnetcore/issues/57573#issuecomment-2744321059 -->

<!-- omit from toc -->
## Table of Contents

- [💥 Problem Overview](#-problem-overview)
- [🔍 Behavior of EditContext](#-behavior-of-editcontext)
- [🧠 Field Identifier Semantics](#-field-identifier-semantics)
- [Existing Solutions (and Flaws)](#existing-solutions-and-flaws)
- [Proposal](#proposal)


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

Two examples of how a `FieldIdentifier` is derived from a lambda expression:

**1. top-level field members**

Imaging having A.B, and a validatable component validates against field member A.B.BA, then edit context A will invoke `NotifyFieldChanged(new FieldIdentifier(model: B, fieldName: "BA"))` and triggering a corresponding OnFieldChanged event on edit context A.

**2. list field members**

Imaging having A.B where A.B is a list of C and you begin to render a validatable component that validates against `A.AB[0]`, then edit context A will invoke `NotifyFieldChanged(new FieldIdentifier(model: C, fieldName: "0"))` and triggering a corresponding OnFieldChanged event on edit context A.

So, you can’t simply pass complex field members of the model into custom Blazor components that trigger validation on a cascaded `EditContext`, and expect that validators registered to that context can handle expressions like `() => complexField.Field`. These complex members are not reference-equal or structurally associated with the root model tracked by the `EditContext`, so validators have no reliable way to trace them back or determine their position within the original model.

## Existing Solutions (and Flaws)

Luckily we have FluentValidation.

There exists a few established Blazor integrations for FluentValidation:

1. Blazored.FluentValidation
   ❌ literally dysfunctional: not component life cycle aware, memory leaking, no edit context nesting*1
2. Blazorise.FluentValidation
   ⚠️ generalized, complex, very opiniated approaches, no edit context nesting*1
3. vNext.BlazorComponents.FluentValidation
   ✅ solid, but is default behaviour having AssemblyScannerValidatorFactory as fallback in case of ServiceProviderValidatorFactory results into null-validator desired?, no edit context nesting*1

*1: "no edit context nesting" means you have to work with one and the same edit context when using the library-authored FluentValidation-aware validator component across the wohle component graph beneath (inside ChildContent RenderFragment) the EditForm (or similiar).

## Proposal

My proposal is a more modular approach, where you plug'n'play validators to your behave, a solution that is not too opinionated:

1. `EditModelValidatorRootpath`
2. `EditModelValidatorSubpath`
3. `EditModelValidatorRoutes`
4. `EditModelScope`

each having its own characteristicas.

**`EditModelValidatorRootpath`**

- A. It execpts an ancestor edit context of type `EditContext` and does not work without one, thus the validator component must be a child of for example a `EditForm` or `CascadedValue` with an instance of `EditContext`.

- B. The ancestor edit context becomes the root edit context if the ancestor edit itself does not have a key-value-pair stored in `Properties` of ancestor edit context that represents the root edit context, otherwise the ancestor edit context will now store itself as property key-value-pair to `Properties`.

- C. The ancestor edit context becomes the actor edit context. If using `ChildContent`, then the actor edit context is casdaded via `CascadedValue`, thus it can become the ancestor edit context to child validatable and validating components.

- D. The validator component acts on the model validation request of the actor edit context by bubbling it up to the root edit context, if the actor edit context is not reference equal to the root edit context.

- E. The validator component acts on the model validation request of the root edit context and communicates the model validation results
  - A. to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.
  - B. to a validator component scoped `ValidationMessageStore` that is attached to the actor edit context, if root edit context and actor edit context are not reference equal.

- F. The validator component acts on the field validation request of the actor edit context and communicates the field validation results
  - A. to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.
  - B. to a validator component scoped `ValidationMessageStore` that is attached to the actor edit context, if root edit context and actor edit context are not reference equal.

**`EditModelValidatorSubpath`**

- A. It expects an ancestor edit context of type `EditContext` and does not work without one, thus the validator component must be a child of for example a `EditForm` or `CascadedValue` with an instance of `EditContext`.

- B. The ancestor edit context becomes the root edit context if the ancestor edit itself does not have a key-value-pair stored in `Properties` of ancestor edit context that represents the root edit context, otherwise the ancestor edit context will now store itself as property key-value-pair to `Properties`.

- C. The ancestor edit context won't becomes the actor edit context, instead a parameter-provided `EditContext` or a new edit context is created from parameter-provided `Model`. If using `ChildContent`, then the actor edit context is casdaded via `CascadedValue`, thus it can become the ancestor edit context to child validatable and validating components.

- D. The validator component acts on the model validation request of the actor edit context by bubbling it up to the root edit context, if the actor edit context is not reference equal to the root edit context.

- E. The validator component acts on the model validation request of the root edit context and communicates the model validation results
  - A. to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.
  - B. to a validator component scoped `ValidationMessageStore` that is attached to the actor edit context, if root edit context and actor edit context are not reference equal.

- F. The validator component acts on the field validation request of the actor edit context and communicates the field validation results
  - A. to a validator component scoped `ValidationMessageStore` that is attached to the root edit context.
  - B. to a validator component scoped `ValidationMessageStore` that is attached to the actor edit context, if root edit context and actor edit context are not reference equal.

**`EditModelValidatorRoutes`**

- A. It expects an ancestor edit context of type `EditContext` and does not work without one, thus the validator component must be a child of for example `EditForm` or `CascadedValue` with an instance of `EditContext`.

- B. It expects an ancestor validator of type `IEditModelValidationNotifier` and does not work without one, thus the validator component must be a child of for example `EditModelValidatorRootpath` or `EditModelValidatorSubpath`.

- C. The ancestor edit context becomes the root edit context if the ancestor edit itself does not have a key-value-pair stored in `Properties` of ancestor edit context that represents the root edit context, otherwise the ancestor edit context will now store itself as property key-value-pair to `Properties`.

- D. The ancestor edit context won't becomes the actor edit context, instead a new edit context is created with the model of the ancestor edit context, then the actor edit context is casdaded via `CascadedValue`, thus it can become the ancestor edit context to child validatable and validating components.

- E. The validator component acts on the model validation request of the actor edit context by bubbling it up to the root edit context, if the actor edit context is not reference equal to the root edit context.

- F. The validator component acts on the field validation request of the actor edit context by delegating it to the ancestor validator component.

**`EditModelScope`**

TODO