# 📘 Component Validator Cookbook

A practical guide to using `ComponentValidatorRootpath`, `ComponentValidatorSubpath`, and `ComponentValidatorRoutes` components in `Tenekon.FluentValidation.Extensions.AspNetCore.Components`.

<!-- omit from toc -->
## Table of Contents

- [📘 Component Validator Cookbook](#-component-validator-cookbook)
  - [✅ Scenario 1: EditForm → Rootpath](#-scenario-1-editform--rootpath)
    - [🧠 When to use](#-when-to-use)
    - [✨ Example](#-example)
    - [⚖️ Notes](#️-notes)
  - [✅ Scenario 2: EditForm → Subpath](#-scenario-2-editform--subpath)
    - [🧠 When to use](#-when-to-use-1)
    - [✨ Example](#-example-1)
    - [⚖️ Notes](#️-notes-1)
  - [✅ Scenario 3: EditForm → Rootpath → Subpath](#-scenario-3-editform--rootpath--subpath)
    - [🧠 When to use](#-when-to-use-2)
    - [✨ Example](#-example-2)
    - [⚖️ Notes](#️-notes-2)
  - [✅ Scenario 4: EditForm → Rootpath with Routes (Parameter)](#-scenario-4-editform--rootpath-with-routes-parameter)
    - [🧠 When to use](#-when-to-use-3)
    - [✨ Example](#-example-3)
    - [⚖️ Notes](#️-notes-3)
  - [✅ Scenario 5: EditForm → Subpath with Routes (Parameter)](#-scenario-5-editform--subpath-with-routes-parameter)
    - [🧠 When to use](#-when-to-use-4)
    - [✨ Example](#-example-4)
    - [⚖️ Notes](#️-notes-4)
  - [✅ Scenario 6: EditForm → Rootpath with Routes (Component)](#-scenario-6-editform--rootpath-with-routes-component)
    - [🧠 When to use](#-when-to-use-5)
    - [✨ Example](#-example-5)
    - [⚖️ Notes](#️-notes-5)
  - [✅ Scenario 7: EditForm → Subpath with Routes (Component)](#-scenario-7-editform--subpath-with-routes-component)
    - [🧠 When to use](#-when-to-use-6)
    - [✨ Example](#-example-6)
    - [⚖️ Notes](#️-notes-6)
  - [🔧 Tips](#-tips)
    - [🧭 Route Targeting Guidelines](#-route-targeting-guidelines)


---

## ✅ Scenario 1: EditForm → Rootpath

### 🧠 When to use

Use when you want to attach validation logic directly to the root `EditForm`, typically for top-level models. The validator logic is attached to the `EditContext` cascaded by `EditForm`, enabling validation for its bound model at the form level.

### ✨ Example

```razor
<EditForm Model="Model">
    <ComponentValidatorRootpath ValidatorType="typeof(MyValidator)" />
</EditForm>
```

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
    <ComponentValidatorSubpath Model="MainModel.Address" ValidatorType="typeof(AddressValidator)">
        <!-- Components like inputs that should be validated by this subpath must be placed in ChildContent -->
    </ComponentValidatorSubpath>
</EditForm>
```

### ⚖️ Notes

* `ComponentValidatorSubpath` must not be self-closing
* Must pass `Model` or `EditContext`
* `Model` should be a property of the main form's model

---

## ✅ Scenario 3: EditForm → Rootpath → Subpath

### 🧠 When to use

When you want **both** top-level and scoped validation (e.g. `MainModel` + `MainModel.Address`).

### ✨ Example

```razor
<EditForm Model="MainModel">
    <ComponentValidatorRootpath ValidatorType="typeof(MainModelValidator)" />

    <ComponentValidatorSubpath Model="MainModel.Address" ValidatorType="typeof(AddressValidator)">
        <!-- Components like inputs that should be validated by this subpath must be placed in ChildContent -->
    </ComponentValidatorSubpath>
</EditForm>
```

### ⚖️ Notes

* `ComponentValidatorSubpath` must not be self-closing
* Combine multiple validators cleanly
* Each `Subpath` gets its own scoped context

---

## ✅ Scenario 4: EditForm → Rootpath with Routes (Parameter)

### 🧠 When to use

For **multi-step or wizard-style forms** where nested submodels determine what gets validated.

### ✨ Example

```razor
<EditForm Model="Model">
    <ComponentValidatorRootpath 
        ValidatorType="typeof(MyValidator)"
        Routes="[() => Model.Step1, () => Model.Step2]">
        <!-- Components like inputs that should be validated by this routed context must be placed in ChildContent -->
    </ComponentValidatorRootpath>
</EditForm>
```

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
    <ComponentValidatorSubpath 
        Model="Model.StepData" 
        ValidatorType="typeof(StepValidator)"
        Routes="[() => Model.StepData.Section1, () => Model.StepData.Section2]">
        <!-- Components like inputs that should be validated by this routed context must be placed in ChildContent -->
    </ComponentValidatorSubpath>
</EditForm>
```

### ⚖️ Notes

* Do **not** use `<ComponentValidatorRoutes>` here — only pass `Routes` as a parameter
* Use only for nested complex objects

---

## ✅ Scenario 6: EditForm → Rootpath with Routes (Component)

### 🧠 When to use

Use when you want to decouple routing logic into a `<ComponentValidatorRoutes></ComponentValidatorRoutes>` block.

### ✨ Example

```razor
<EditForm Model="Model">
    <ComponentValidatorRootpath ValidatorType="typeof(MyValidator)">
        <ComponentValidatorRoutes Routes="[() => Model.PartA, () => Model.PartB]">
            <!-- Components like inputs that should be validated by this routed context must be placed in ChildContent -->
        </ComponentValidatorRoutes>
    </ComponentValidatorRootpath>
</EditForm>
```

### ⚖️ Notes

* `<ComponentValidatorRoutes>` must not be self-closing
* Used for manual routing control
* Each block scopes its own validation context

---

## ✅ Scenario 7: EditForm → Subpath with Routes (Component)

### 🧠 When to use

Use to nest `ComponentValidatorRoutes` manually inside a scoped submodel validator.

### ✨ Example

```razor
<EditForm Model="Model">
    <ComponentValidatorSubpath Model="Model.Step" ValidatorType="typeof(StepValidator)">
        <ComponentValidatorRoutes Routes="[() => Model.PartA, () => Model.PartB]">
            <!-- Components like inputs that should be validated by this routed context must be placed in ChildContent -->
        </ComponentValidatorRoutes>
    </ComponentValidatorSubpath>
</EditForm>
```

### ⚖️ Notes

* `<ComponentValidatorRoutes>` must not be self-closing
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
