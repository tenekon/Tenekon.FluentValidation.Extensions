<!-- omit from toc -->
# Tenekon.FluentValidation.Extensions

> :construction: This is a **new** project. You are very welcome to participate in this project, help shape the public API and share ideas. The project is currently in _alpha_ state, so the public API may change significantly.

<!-- omit from toc -->
## Table of Contents

- [Packages](#packages)
  - [`Tenekon.FluentValidation.Extensions.AspNetCore.Components`](#tenekonfluentvalidationextensionsaspnetcorecomponents)
  - [Features](#features)
  - [Quickstart](#quickstart)
  - [Documentation \& Resources](#documentation--resources)
  - [Target Framework](#target-framework)
  - [Dependencies](#dependencies)
- [Development](#development)
- [License](#license)

## Packages

| Package Description                                                                                                                                                                                                                                                                                                                                        |                                         |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------- |
| [`Tenekon.FluentValidation.Extensions.AspNetCore.Components`](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)<br/>[![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components) | Blazor integration for FluentValidation |

### `Tenekon.FluentValidation.Extensions.AspNetCore.Components`

An extension to integrate [FluentValidation](https://fluentvalidation.net/) with ASP.NET Core Components (Blazor). This
library enhances component-level validation in Blazor applications using FluentValidation.

> :open_book: For usage examples, see the [Component Validator Cookbook](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/COOKBOOK.md) covering common and advanced scenarios.

---

### Features

- :sun_with_face: **Seamless integration with Blazor forms**<sup>1</sup>
- :electric_plug: **Component-level validation** — plug component validators<sup>2</sup> into forms or any part of a form, even deeply nested components<sup>3</sup>.
- :jigsaw: **Nestable component validators** – deep child component validators<sup>2</sup> still hook into the validation of the main form<sup>3</sup>.

<small><sup>1</sup>: Any form that provides a cascaded `EditContext`, even a plain `CascadedValue Value="new EditContext(..)">..</CascadedValue>` is sufficient.</small><br/>
<small><sup>2</sup>: Refers to the usage of validator components of this library.</small><br/>
<small><sup>3</sup>: Nested child component validators automatically receive the nearest `EditContext`, captured by the first validator component<sup>2</sup> higher in the hierarchy (usually from a form<sup>1</sup>).</small>

### Quickstart

**1\. Install the NuGet package:** [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

```bash
dotnet add package Tenekon.FluentValidation.Extensions.AspNetCore.Components
```

**2\. Define your validator** using `FluentValidation`:

```csharp
public class MyModelValidator : AbstractValidator<MyModel>
{
    public MyModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

> Requires `FluentValidation` package to be installed.

**3\. Register the validator** in your DI container:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<MyModelValidator>();
```

> Requires `FluentValidation.DependencyInjectionExtensions` package to be installed.

**4\. Use the component validator in your Blazor form**:

```razor
<EditForm Model="model" OnValidSubmit="HandleValidSubmit">
    <ComponentValidatorRootpath ValidatorType="typeof(MyModelValidator)" />
    <InputText @bind-Value="model.Name" />
    <ValidationMessage For="() => model.Name" />
    <button type="submit">Submit</button>
</EditForm>
```

### Documentation & Resources

- :open_book: **Cookbook**: [Component Validator Cookbook](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/COOKBOOK.md) —
  Examples & use cases for all common and advanced scenarios.
- :bricks: **Architecture** [Component Validator Architecture](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/ARCHITECTURE.md) — Architectural approach of the components, and their integration with Blazor's EditContext.
- :microscope: **Flow Logic**: [Component Validator Flow Logic](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/FLOWLOGIC.md) — Visual guide to
  how component validators interact with each other and Blazor's `EditContext`.

### Target Framework

* `.NET 8.0`

### Dependencies

* `FluentValidation` 12.x
* `Microsoft.AspNetCore.Components.Forms` 8.x
* `FastExpressionCompiler` 5.x

## Development

```bash
git clone https://github.com/tenekon/Tenekon.FluentValidation.Extensions.git
cd Tenekon.FluentValidation.Extensions
dotnet build
```

## License

MIT License - see [LICENSE](LICENSE) for details.
