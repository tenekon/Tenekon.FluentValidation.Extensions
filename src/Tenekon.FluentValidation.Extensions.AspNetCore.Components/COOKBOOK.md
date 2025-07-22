# 📘 Validator Components Cookbook [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

A practical guide to using `EditModelValidatorRootpath`, `EditModelValidatorSubpath`, and `EditModelValidatorRoutes` components in `Tenekon.FluentValidation.Extensions.AspNetCore.Components`.

<!-- omit from toc -->
## Table of Contents

- [📘 Validator Components Cookbook ](#-validator-component-cookbook-)
  - [✅ Scenario 1: EditForm → Rootpath](#-scenario-1-editform--rootpath)
    - [🧠 When to use](#-when-to-use)
    - [✨ Example](#-example)
  - [✅ Scenario 2: EditForm → Subpath](#-scenario-2-editform--subpath)
    - [🧠 When to use](#-when-to-use-1)
    - [✨ Example](#-example-1)
  - [✅ Scenario 3: EditForm → Rootpath → Subpath](#-scenario-3-editform--rootpath--subpath)
    - [🧠 When to use](#-when-to-use-2)
    - [✨ Example](#-example-2)
  - [✅ Scenario 4: EditForm → Rootpath with Routes (Parameter)](#-scenario-4-editform--rootpath-with-routes-parameter)
    - [🧠 When to use](#-when-to-use-3)
    - [✨ Example](#-example-3)
  - [✅ Scenario 5: EditForm → Subpath with Routes (Parameter)](#-scenario-5-editform--subpath-with-routes-parameter)
    - [🧠 When to use](#-when-to-use-4)
    - [✨ Example](#-example-4)
  - [✅ Scenario 6: EditForm → Rootpath with Routes (Component)](#-scenario-6-editform--rootpath-with-routes-component)
    - [🧠 When to use](#-when-to-use-5)
    - [✨ Example](#-example-5)
  - [✅ Scenario 7: EditForm → Subpath with Routes (Component)](#-scenario-7-editform--subpath-with-routes-component)
    - [🧠 When to use](#-when-to-use-6)
    - [✨ Example](#-example-6)
  - [🔧 Tips](#-tips)
    - [🧭 Route Targeting Guidelines](#-route-targeting-guidelines)


---

## ✅ Scenario 1: EditForm → Rootpath

### 🧠 When to use

Use when you want to attach validation logic directly to the root `EditForm`, typically for top-level models. The validator logic is attached to the `EditContext` cascaded by `EditForm`, enabling validation for its bound model at the form level.

### ✨ Example

```razor
<EditForm Model="Model">
    <EditModelValidatorRootpath ValidatorType="typeof(MyValidator)" />

    <InputText @bind-Value="Model.SomeText" />
    <InputNumber @bind-Value="Model.SomeNumber" />
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* Can use `Validator` or `ValidatorType`
* Auto-binds to `EditForm.Model` if not overridden

---

## ✅ Scenario 2: EditForm → Subpath

### 🧠 When to use

Use when validating a **specific model subset** or child component independently.

### ✨ Example

```razor
<EditForm Model="MainModel">
    <EditModelValidatorSubpath Model="MainModel.Address" ValidatorType="typeof(AddressValidator)">
        <InputText @bind-Value="MainModel.Address.City" />
        <InputNumber @bind-Value="MainModel.Address.Zip" />
    </EditModelValidatorSubpath>
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* `EditModelValidatorSubpath` must not be self-closing
* Must pass `Model` or `EditContext`
* `Model` should be a property of the main form's model

---

## ✅ Scenario 3: EditForm → Rootpath → Subpath

### 🧠 When to use

When you want **both** top-level and scoped validation (e.g. `MainModel` + `MainModel.Address`).

### ✨ Example

```razor
<EditForm Model="MainModel">
    <EditModelValidatorRootpath ValidatorType="typeof(MainModelValidator)" />

    @{
        var address = MainModel.Address;
        <EditModelValidatorSubpath Model="address" ValidatorType="typeof(AddressValidator)">
                <InputText @bind-Value="address.City" />
                <InputNumber @bind-Value="address.Zip" />
        </EditModelValidatorSubpath>
    }
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* `EditModelValidatorSubpath` must not be self-closing
* Combine multiple validators cleanly
* Each `Subpath` gets its own scoped context

---

## ✅ Scenario 4: EditForm → Rootpath with Routes (Parameter)

### 🧠 When to use

For **multi-step or wizard-style forms** where nested submodels determine what gets validated.

### ✨ Example

```razor
<EditForm Model="Model">
    <EditModelValidatorRootpath 
        ValidatorType="typeof(MyValidator)"
        Routes="[() => Model.Step1, () => Model.Step2]">
            <InputText @bind-Value="Model.Step1.SomeText" />
            <InputNumber @bind-Value="Model.Step2.SomeNumber" />
    </EditModelValidatorRootpath>
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* `Routes` must point to **nested complex objects**, not primitive properties
* Component must not be self-closing when using `Routes`
* Each routed object receives a scoped `EditContext`

---

## ✅ Scenario 5: EditForm → Subpath with Routes (Parameter)

### 🧠 When to use

Combine scoped submodel validation with route awareness (e.g. wizard sections).

### ✨ Example

```razor
<EditForm Model="Model">
    @{
        var stepData = Model.StepData;
        <EditModelValidatorSubpath 
            Model="stepData" 
            ValidatorType="typeof(StepValidator)"
            Routes="[() => stepData.Section1, () => stepData.Section2]">
                <InputText @bind-Value="stepData.Section1.SomeText" />
                <InputNumber @bind-Value="stepData.Section2.SomeNumber" />
        </EditModelValidatorSubpath>
    }
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* Do **not** use `<EditModelValidatorRoutes>` here — only pass `Routes` as a parameter
* Use only for nested complex objects

---

## ✅ Scenario 6: EditForm → Rootpath with Routes (Component)

### 🧠 When to use

Use when you want to decouple routing logic into a `<EditModelValidatorRoutes></EditModelValidatorRoutes>` block.

### ✨ Example

```razor
<EditForm Model="Model">
    <EditModelValidatorRootpath ValidatorType="typeof(MyValidator)">
        <EditModelValidatorRoutes Routes="[() => Model.PartA, () => Model.PartB]">
            <InputText @bind-Value="Model.PartA.SomeText" />
            <InputNumber @bind-Value="Model.PartB.SomeNumber" />
        </EditModelValidatorRoutes>
    </EditModelValidatorRootpath>
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* `<EditModelValidatorRoutes>` must not be self-closing
* Used for manual routing control
* Each block scopes its own validation context

---

## ✅ Scenario 7: EditForm → Subpath with Routes (Component)

### 🧠 When to use

Use to nest `EditModelValidatorRoutes` manually inside a scoped submodel validator.

### ✨ Example

```razor
<EditForm Model="Model">
    @{
        var step = Model.Step;
        <EditModelValidatorSubpath Model="step" ValidatorType="typeof(StepValidator)">
            <EditModelValidatorRoutes Routes="[() => step.PartA, () => step.PartB]">
                <InputText @bind-Value="step.PartA.SomeText" />
                <InputNumber @bind-Value="step.PartB.SomeNumber" />
            </EditModelValidatorRoutes>
        </EditModelValidatorSubpath>
    }
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* `<EditModelValidatorRoutes>` must not be self-closing
* Enables flexible validation of deep models
* Keep route expressions targeting complex types only

---

## 🔧 Tips

* Always specify `Model` or `EditContext` in subpath validators
* Use `ValidatorType` for DI-resolved validators, `Validator` for direct instantiation
* Only one validator should handle each part of the model to avoid collisions
* When using route-aware validation, always pass the `Routes` parameter with **nested property expressions** that targets complex types only

---

### 🧭 Route Targeting Guidelines

* ✅ Use `() => Model.City.Address` — target nested models only
* ❌ Avoid `() => Model.City.Address.Street` — no primitive routing
* 🔍 Only specify **paths that are actually used**
  For example:

  ```csharp
  Routes = new[] { () => Model.City.Address } // ✅ Good
  Routes = new[] { () => Model.City, () => Model.City.Address } // ❌ Redundant
  ```
* Redundant as long as you do not validate against `Model.City.Name` within the ChildContent of the component declaring the routes.
* Routes have a scoped `EditContext` used by inputs within `ChildContent`. The scoped `Editontext` uses the routes to be able to navigate back to `Model` as shown in the example above.
