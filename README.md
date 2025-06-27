# Tenekon.FluentValidation.Extensions

> :construction: This is a **new** project. You are very welcome to anticipate in this project, shaping the public API and submit ideas. Because this project is currently in _alpha_ state, the public API may change drastically.

## Table of Contents

- [Tenekon.FluentValidation.Extensions](#tenekonfluentvalidationextensions)
  - [Table of Contents](#table-of-contents)
  - [Packages](#packages)
    - [`Tenekon.FluentValidation.Extensions.AspNetCore.Components`](#tenekonfluentvalidationextensionsaspnetcorecomponents)
    - [Features](#features)
    - [Installation](#installation)
    - [Usage](#usage)
    - [Target Framework](#target-framework)
    - [Dependencies](#dependencies)
  - [Development](#development)
  - [License](#license)

## Packages 

| Package                                                     | Description                                  |
| ----------------------------------------------------------- | -------------------------------------------- |
| `Tenekon.FluentValidation.Extensions.AspNetCore.Components` | Core Blazor integration for FluentValidation |

### `Tenekon.FluentValidation.Extensions.AspNetCore.Components`

An extension to integrate [FluentValidation](https://fluentvalidation.net/) with ASP.NET Core Components (Blazor). This library enhances component-level validation in Blazor applications using FluentValidation.

---

### Features

- ✅ Seamless integration with Blazor forms.
- 🔁 Component-level validator injection
- 🧩 Validator components are nestable
- 🚀 Optimized with FastExpressionCompiler

### Installation

```bash
dotnet add package Tenekon.FluentValidation.Extensions.AspNetCore.Components
```

### Usage

1. **Define your validator** using `FluentValidation`:

```csharp
public class MyModelValidator : AbstractValidator<MyModel>
{
    public MyModelValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
    }
}
```

2. **Register the validator** in your DI container:

```csharp
builder.Services.AddValidatorsFromAssemblyContaining<MyModelValidator>();
```

3. **Use the component validator in your Blazor form**:

```razor
<EditForm Model="@model" OnValidSubmit="@HandleValidSubmit">
    <ComponentValidatorRootpath ValidatorType="typeof(MyModelValidator)" />
    <InputText @bind-Value="model.Name" />
    <ValidationMessage For="@(() => model.Name)" />
    <button type="submit">Submit</button>
</EditForm>
```

---

**TBD**

TBD: Document which scenarios are covered by the different components and parameters, e.g.

- `<ComponentValidatorRootpath ../>`
- `<ComponentValidatorRootpath ..>..</ComponentValidatorRootpath>`
- `<ComponentValidatorRootpath Routes=".." ../>`
- `<ComponentValidatorSubpath ..>..</ComponentValidatorSubpath>`
- `<ComponentValidatorSubpath Routes=".." ..>..</ComponentValidatorSubpath>`
- `<ComponentValidator*path SuppressInvalidatableFieldModels=".." ../>`
- `<ComponentValidatorRoutes>..</ComponentValidatorRoutes>`

### Target Framework

* `.NET 9.0`

### Dependencies

* `FluentValidation` 12.x
* `Microsoft.AspNetCore.Components.Forms` 9.x
* `FastExpressionCompiler` 5.x

## Development

```bash
git clone https://github.com/tenekon/Tenekon.FluentValidation.Extensions.git
cd Tenekon.FluentValidation.Extensions
dotnet build
```

## License

MIT License - see [LICENSE](LICENSE) for details.
