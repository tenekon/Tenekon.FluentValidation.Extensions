<!-- omit from toc -->
# 📘 Validator Components Cookbook [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

A practical guide to using `EditModelValidatorRootpath`, `EditModelValidatorSubpath`, `EditModelValidatorRoutes` and `EditModelScope` components in `Tenekon.FluentValidation.Extensions.AspNetCore.Components`.

<!-- omit from toc -->
## Table of Contents

- [✅ Scenario 1: EditForm → EditModelValidatorRootpath](#-scenario-1-editform--editmodelvalidatorrootpath)
  - [🧠 When to use](#-when-to-use)
  - [✨ Example](#-example)
- [✅ Scenario 2: EditForm → EditModelValidatorSubpath](#-scenario-2-editform--editmodelvalidatorsubpath)
  - [🧠 When to use](#-when-to-use-1)
  - [✨ Example](#-example-1)
- [✅ Scenario 3: EditForm → EditModelValidatorRootpath + EditModelValidatorSubpath](#-scenario-3-editform--editmodelvalidatorrootpath--editmodelvalidatorsubpath)
  - [🧠 When to use](#-when-to-use-2)
  - [✨ Example](#-example-2)
- [✅ Scenario 4: EditForm → EditModelValidatorRootpath + Routes (Parameter)](#-scenario-4-editform--editmodelvalidatorrootpath--routes-parameter)
  - [🧠 When to use](#-when-to-use-3)
  - [✨ Example](#-example-3)
- [✅ Scenario 5: EditForm → EditModelValidatorSubpath + Routes (Parameter)](#-scenario-5-editform--editmodelvalidatorsubpath--routes-parameter)
  - [🧠 When to use](#-when-to-use-4)
  - [✨ Example](#-example-4)
- [✅ Scenario 6: EditForm → EditModelValidatorRootpath → EditModelValidatorRoutes + Routes (Parameter)](#-scenario-6-editform--editmodelvalidatorrootpath--editmodelvalidatorroutes--routes-parameter)
  - [🧠 When to use](#-when-to-use-5)
  - [✨ Example](#-example-5)
- [✅ Scenario 7: EditForm → EditModelValidatorSubpath → EditModelValidatorRoutes + Routes  (Parameter)](#-scenario-7-editform--editmodelvalidatorsubpath--editmodelvalidatorroutes--routes--parameter)
  - [🧠 When to use](#-when-to-use-6)
  - [✨ Example](#-example-6)
- [✅ Scenario 8: EditForm → EditModelScope → EditModelValidatorRootpath](#-scenario-8-editform--editmodelscope--editmodelvalidatorrootpath)
  - [🧠 When to use](#-when-to-use-7)
  - [✨ Example](#-example-7)
- [✅ Scenario 8: EditForm → EditModelScope + Model (Parameter) → EditModelValidatorRootpath](#-scenario-8-editform--editmodelscope--model-parameter--editmodelvalidatorrootpath)
  - [🧠 When to use](#-when-to-use-8)
  - [✨ Example](#-example-8)
- [🔧 Tips](#-tips)
  - [🧭 Route Targeting Guidelines](#-route-targeting-guidelines)


---

## ✅ Scenario 1: EditForm → EditModelValidatorRootpath

### 🧠 When to use

Use `EditModelValidatorRootpath`  when you want to attach validation logic to the model of the cascaded EditContext, in the example cascaded by EditForm.

### ✨ Example

```razor
<EditForm Model="_mainModel">
    <EditModelValidatorRootpath ValidatorType="typeof(MainModelValidator)" />

    <InputText @bind-Value="_mainModel.SomeText" />
    <InputNumber @bind-Value="_mainModel.SomeNumber" />
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* Can use `Validator` or `ValidatorType`
* Auto-binds to `EditForm.Model` if not overridden

---

## ✅ Scenario 2: EditForm → EditModelValidatorSubpath

### 🧠 When to use

Use this when validating a nested model, such as a property or collection item inside the main model.

### ✨ Example

```razor
<EditForm Model="_mainModel">
    <EditModelValidatorSubpath Model="_mainModel.Address" ValidatorType="typeof(AddressValidator)">
        <InputText @bind-Value="_mainModel.Address.City" />
        <InputNumber @bind-Value="_mainModel.Address.Zip" />
    </EditModelValidatorSubpath>
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

- Not self-closing
- Requires Model or EditContext
- Scoped validation context for the sub-model

---

## ✅ Scenario 3: EditForm → EditModelValidatorRootpath + EditModelValidatorSubpath

### 🧠 When to use

When you want **both** isolated top-level validation and isolated nested-level subpath validation.

### ✨ Example

```razor
<!-- Cascades EditContext A --->
<EditForm Model="_mainModel">
    <!-- Validation errors bubbles up to EditContext A -->
    <EditModelValidatorRootpath ValidatorType="typeof(MainModelValidator)" />

    <InputText @bind-Value="_mainModel.SomeText" />
    <InputNumber @bind-Value="_mainModel.SomeNumber" />
    
    @{
        var address = _mainModel.Address;
        <!-- Cascades EditContext B -->
        <!-- Validation errors bubbles up to EditContext A & B -->
        <EditModelValidatorSubpath Model="address" ValidatorType="typeof(AddressValidator)">
                <InputText @bind-Value="address.City" />
                <InputNumber @bind-Value="address.Zip" />
        </EditModelValidatorSubpath>
    }
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

TODO

---

## ✅ Scenario 4: EditForm → EditModelValidatorRootpath + Routes (Parameter)

### 🧠 When to use

TODO

### ✨ Example

```razor
<!-- Cascades EditContext A --->
<EditForm Model="_mainModel">
    <!-- Cascades EditContext B -->
    <!-- Validation errors bubbles up to EditContext A & B -->
    <EditModelValidatorRootpath ValidatorType="typeof(MainModelValidator)"
                                Routes="[() => _mainModel.Step1, () => _mainModel.Step2]">
            <InputText @bind-Value="_mainModel.Step1.SomeText" />
            <InputNumber @bind-Value="_mainModel.Step2.SomeNumber" />
    </EditModelValidatorRootpath>
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* `Routes` must point to **nested complex objects**, not primitive properties
* Only validatable descedants within EditModelValidatorRootpath use FluentValidation validator

---

## ✅ Scenario 5: EditForm → EditModelValidatorSubpath + Routes (Parameter)

### 🧠 When to use

TODO

### ✨ Example

```razor
<!-- Cascades EditContext A --->
<EditForm Model="_mainModel">
    @{
        var stepData = _mainModel.StepData;
        <!-- Cascades EditContext B -->
        <!-- Validation errors bubbles up to EditContext A & B -->
        <EditModelValidatorSubpath Model="stepData"
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

TODO

---

## ✅ Scenario 6: EditForm → EditModelValidatorRootpath → EditModelValidatorRoutes + Routes (Parameter)

### 🧠 When to use

Use when you want to decouple routing logic into a `<EditModelValidatorRoutes></EditModelValidatorRoutes>` block.

### ✨ Example

```razor
<!-- Cascades EditContext A --->
<EditForm Model="_mainModel">
    <!-- Cascades EditContext B --->
    <!-- Validation errors bubbles up to EditContext A & B -->
    <EditModelValidatorRootpath ValidatorType="typeof(MainModelValidator)">
        <EditModelValidatorRoutes Routes="[() => _mainModel.PartA, () => _mainModel.PartB]">
            <InputText @bind-Value="_mainModel.PartA.SomeText" />
            <InputNumber @bind-Value="_mainModel.PartB.SomeNumber" />
        </EditModelValidatorRoutes>
    </EditModelValidatorRootpath>
</EditForm>
```

<!-- omit from toc -->
### ⚖️ Notes

* Used for manual routing control
* Each block scopes its own validation context

---

## ✅ Scenario 7: EditForm → EditModelValidatorSubpath → EditModelValidatorRoutes + Routes  (Parameter)

### 🧠 When to use

Use to nest `EditModelValidatorRoutes` manually inside a scoped submodel validator.

### ✨ Example

```razor
<EditForm Model="_mainModel">
    @{
        var step = _mainModel.Step;
        <EditModelValidatorSubpath Model="step" ValidatorType="typeof(StepValidator)">
            <EditModelValidatorRoutes Routes="[() => step.PartA, () => step.PartB]">
                <InputText @bind-Value="step.PartA.SomeText" />
                <InputNumber @bind-Value="step.PartB.SomeNumber" />
            </EditModelValidatorRoutes>
        </EditModelValidatorSubpath>
    }
</EditForm>
```

---

## ✅ Scenario 8: EditForm → EditModelScope → EditModelValidatorRootpath

### 🧠 When to use

TODO

### ✨ Example

```razor
<EditForm Model="_mainModel">
    @{
        <EditModelScope>
            <EditModelValudatorRootpath ValidatorType="typeof(MainModelValidator)">
            <InputText @bind-Value="step.SomeText" />
            <InputNumber @bind-Value="step.SomeNumber" />
        </EditModelScope>
    }
</EditForm>
```

---

## ✅ Scenario 8: EditForm → EditModelScope + Model (Parameter) → EditModelValidatorRootpath

### 🧠 When to use

TODO

### ✨ Example

```razor
<EditForm Model="_mainModel">
    @{
        var step = _mainModel.Step;
        <EditModelScope Model="step">
            <EditModelValudatorRootpath ValidatorType="typeof(StepValidator)">
            <InputText @bind-Value="step.SomeText" />
            <InputNumber @bind-Value="step.SomeNumber" />
        </EditModelScope>
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

* Always specify `Model` or `EditContext` in EditModelValidatorSubpath validators
* Use `ValidatorType` for DI-resolved validators, `Validator` for direct instantiation
* Only one validator should handle each part of the model to avoid collisions
* When using route-aware validation, always pass the `Routes` parameter with **nested property expressions** that targets complex types only

---

### 🧭 Route Targeting Guidelines

* ✅ Use `() => _mainModel.City.Address` — target nested models only
* ❌ Avoid `() => _mainModel.City.Address.Street` — no primitive routing
* 🔍 Only specify **paths that are actually used**
  For example:

  ```csharp
  Routes = new[] { () => _mainModel.City.Address } // ✅ Good
  Routes = new[] { () => _mainModel.City, () => _mainModel.City.Address } // ❌ Redundant
  ```
* Redundant as long as you do not validate against `_mainModel.City.Name` within the ChildContent of the component declaring the routes.
* Routes have a scoped `EditContext` used by inputs within `ChildContent`. The scoped `Editontext` uses the routes to be able to navigate back to `Model` as shown in the example above.
