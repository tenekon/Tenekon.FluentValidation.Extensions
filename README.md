<!-- omit from toc -->
# Tenekon.FluentValidation.Extensions [![Starts](https://img.shields.io/github/stars/tenekon/Tenekon.FluentValidation.Extensions)](https://github.com/tenekon/Tenekon.FluentValidation.Extensions/stargazers) [![License](https://img.shields.io/github/license/tenekon/Tenekon.FluentValidation.Extensions)](https://github.com/tenekon/Tenekon.FluentValidation.Extensions/blob/main/LICENSE) [![Activity](https://img.shields.io/github/last-commit/tenekon/Tenekon.FluentValidation.Extensions)](https://github.com/tenekon/Tenekon.FluentValidation.Extensions/commits/main/) [![Discord](https://img.shields.io/discord/1288602831095468157?label=tenekon%20community)](https://discord.gg/VCa8ePSAqD)

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

An extension to integrate [FluentValidation](https://fluentvalidation.net/) with ASP.NET Core Components (Blazor Components). This
library enhances component-level validation in Blazor applications using FluentValidation.

> :open_book: For usage examples, see the [Validator Components Cookbook](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/COOKBOOK.md) covering common and advanced scenarios.

---

### Features

- :sun_with_face: **Seamless integration with Blazor forms**<sup>1</sup>
- :jigsaw: **Validate not only top-level anymore** – just wrap each edit model part by a validator component<sup>2,4</sup>
- :jigsaw: **Nestable edit model validators** – nested child validator components<sup>2</sup> still hook into the validation of the main form<sup>3</sup>.
- :electric_plug: **Component-region validation** — plug validators<sup>2</sup> into forms or any part of a form or any nested region or subregion.<sup>3</sup>

<small><sup>1</sup>: Any form that provides a cascaded `EditContext`, even a plain `CascadedValue Value="new EditContext(..)">..</CascadedValue>` is sufficient.</small><br/>
<small><sup>2</sup>: Refers to the usage of validator components of this library.</small><br/>
<small><sup>3</sup>: Nested child edit model validators automatically receive the nearest `EditContext`, captured by the first validator component<sup>2</sup> higher in the hierarchy (usually from a form<sup>1</sup>).</small><br/>
<small><sup>4</sup>: `EditModelValidatorSubpath` and `EditModelScope` have the capability to create an isolated `EditContxt` wired to the nearest `EditContext`<sup>3</sup>.

### Quickstart

**1\. Install the NuGet package:** [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

```bash
dotnet add package Tenekon.FluentValidation.Extensions.AspNetCore.Components
```

**2\. Define your validator** using `FluentValidation`:

```csharp
public class ModelValidator : AbstractValidator<MyModel>
{
    public ModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

> Requires `FluentValidation` package to be installed.

**3\. Register the validator** in your DI container:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<ModelValidator>();
```

> Requires `FluentValidation.DependencyInjectionExtensions` package to be installed.

**4\. Use the validator component(s) in your Blazor form**:

```razor
@code {
    private Model _model = new Model {
        Name = default(string)
        Children = (List<SubModel>)[
            new SubModel {
                Name = default(string)
            }
        ]
    };
}
<EditForm Model="_model" OnValidSubmit="HandleValidSubmit">
    <EditModelValidatorRootpath ValidatorType="typeof(ModelValidator)" />
    <InputText @bind-Value="_model.Name" />
    <ValidationMessage For="() => _model.Name" />
    @foreach(var child in _model.Children) {
        <EditModelValidatorSubpath Model="child" ValidatorType="typeof(SubModelValidator)">
            <InputText @bind-Value="child.Name"/>
            <ValidationMessage For="() => child.Name"/>
            <!-- Also triggers whole form submission — just for demonstration purposes -->
            <button type="submit">Prematurely Submit</button>
        </EditModelValidatorSubpath>
    }
    <button type="submit">Submit</button>
</EditForm>
```

### Documentation & Resources

- :open_book: **Cookbook**: [Validator Components Cookbook](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/COOKBOOK.md) —
  Examples & use cases for all common and advanced scenarios.
- :bricks: **Architecture** [Validator Components Architecture](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/ARCHITECTURE.md) — Architectural approach of the components, and their integration with Blazor's EditContext.
- :microscope: **Flow Logic**: [Validator Components Flow Logic](src/Tenekon.FluentValidation.Extensions.AspNetCore.Components/FLOWLOGIC.md) — Visual guide to
  how edit model validators interact with each other and Blazor's `EditContext`.

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
