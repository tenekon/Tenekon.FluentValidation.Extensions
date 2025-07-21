<!-- omit from toc -->
# :microscope: Component Validator Flow Logic [![NuGet](https://img.shields.io/nuget/v/Tenekon.FluentValidation.Extensions.AspNetCore.Components?label=Tenekon.FluentValidation.Extensions.AspNetCore.Components)](https://www.nuget.org/packages/Tenekon.FluentValidation.Extensions.AspNetCore.Components)

<!-- omit from toc -->
## Table of Contents

- [Component Interaction](#component-interaction)
- [Event Handling](#event-handling)

## Component Interaction

```mermaid
---
config:
  layout: dagre
  theme: base
  look: classic
---
flowchart TD
 subgraph s2["ValidationMessageStore Handling"]
        RegisterStores["ValidationMessageStore Handling"]
        RootStore["Register new indepdendent ValidationMessageStore to RootEditContext"]
        ActorVsSuper{"ActorEditContext ≠ AncestorEditContext?"}
        RegisterActorStore["Also register new indepdendent ValidationMessageStore to ActorEditContext"]
        SkipActorStore["Skip ActorEditContext ValidationMessageStore"]
        n2["Inherits from ComponentValidatorBase?"]
  end
 subgraph s3["Resolve ActorEditContext"]
        ActorDefined{"ActorEditContext already defined?"}
        SetActorAsSuper["Set ActorEditContext = AncestorEditContext"]
        UseActor["Use explicit or created ActorEditContext"]
        CascadeActor["Cascade ActorEditContext further down the component graph via CascadedValue"]
        StoreRoot["Write RootEditContext to ActorEditContext Properties[RootEditContextKey]"]
  end
 subgraph s4["Resolve RootEditContext"]
        CheckRootKey{"Does AncestorEditContext Properties[RootEditContextKey] exist?"}
        UseExistingRoot["Set RootEditContext = found RootEditContext"]
        UseSuperAsRoot["Set RootEditContext = AncestorEditContext"]
        StoreRootToSuper["Store RootEditContext to AncestorEditContext Properties[RootEditContextKey]"]
  end
 subgraph s1["Sub Event Handling"]
        SubVR["Subscribe OnValidationRequested (RootEditContext)"]
        SubFC["Subscribe OnFieldChanged (ActorEditContext)"]
        BubbleCheck{"ActorEditContext ≠ AncestorEditContext?"}
        BubbleVR["Subscribe OnValidationRequested (ActorEditContext) → bubble up to RootEditContext"]
  end
 subgraph s6["ComponentValidatorSubpath"]
        Subpath["ComponentValidatorSubpath"]
        SubCtxCheck{"ActorEditContext / Model?"}
        ErrorBoth["❌ ERROR: Both EditContext and Model set"]
        ErrorNone["❌ ERROR: Neither EditContext nor Model set"]
        CreateCtx["Create new EditContext from set Model"]
        UseCtx["Use EditContext as-is"]
        Stop2(("Throw error"))
        n3["Continue component initialization"]
  end
 subgraph s7["ComponentValidatorRoutes"]
        StartRoutes[["Cascaded IComponentValidator from Rootpath or Subpath ancestor"]]
        RoutesComponent["ComponentValidatorRoutes"]
        RoutesComponentActor["Always provide ActorEditContext via model sentinel"]
  end
 subgraph componentValdatorBase["ComponentValidatorBase"]
        n39["Begin component initialization"]
        componentValidatorClassGraph["componentValidatorClassGraph"]
        ComponentValidatorBaseEditContextCascadingParameter[["Cascaded EditContext becomes AncestorEditContext"]]
  end
 subgraph componentValidatorBaseNesting["&nbsp;"]
        n36["Allowed Nesting"]
        n37["✅ Rootpath and Subpath can be freely mixed"]
        n38["⚠ Routes must be nested under Rootpath or Subpath"]
  end
 subgraph componentValidatorClassGraph["&nbsp;"]
        ComponentValidatorBaseClass["ComponentValidator[Rootpath, Subpath or Routes]"]
        componentValidatorBaseNesting
  end
    StartRoutes --> RoutesComponent
    RoutesComponent --> RoutesComponentActor
    ComponentValidatorBaseEditContextCascadingParameter --> ComponentValidatorBaseClass
    ComponentValidatorBaseClass --> CheckRootKey
    CheckRootKey -- Yes --> UseExistingRoot
    UseExistingRoot --> ActorDefined
    CheckRootKey -- No --> UseSuperAsRoot
    UseSuperAsRoot --> StoreRootToSuper
    StoreRootToSuper --> ActorDefined
    ActorDefined -- No --> SetActorAsSuper
    ActorDefined -- Yes --> UseActor
    UseActor --> CascadeActor
    SetActorAsSuper --> CascadeActor
    CascadeActor --> StoreRoot
    RegisterStores --> RootStore & ActorVsSuper
    ActorVsSuper -- Yes --> RegisterActorStore
    ActorVsSuper -- No --> SkipActorStore
    SkipActorStore --> SubVR
    SubVR --> SubFC
    SubFC --> BubbleCheck
    BubbleCheck -- Yes --> BubbleVR
    BubbleCheck -- No --> n12["Complete component initialization"]
    Subpath --> SubCtxCheck
    SubCtxCheck -- Both --> ErrorBoth
    SubCtxCheck -- Neither --> ErrorNone
    SubCtxCheck -- OnlyModel --> CreateCtx
    SubCtxCheck -- OnlyEditContext --> UseCtx
    CreateCtx --> n3
    UseCtx --> n3
    ErrorNone --> Stop2
    n2 -- Yes --> RegisterStores
    RegisterActorStore --> SubVR
    StoreRoot --> n2
    n2 -- No --> SubVR
    ErrorBoth --> Stop2
    BubbleVR --> n12
    n36 --> n37 & n38
    n3 --> ComponentValidatorBaseClass & n39
    n39 --> ComponentValidatorBaseClass
    RoutesComponentActor --> n39
    n2@{ shape: diam}
    n3@{ shape: rect}
    n39@{ shape: rect}
    n36@{ shape: rect}
    n37@{ shape: rect}
    n38@{ shape: rect}
    style ActorVsSuper fill:#C8E6C9,stroke:#000,stroke-width:1px,color:#000000
    style n2 fill:#C8E6C9,color:#000000
    style ActorDefined fill:#C8E6C9,color:#000000
    style CheckRootKey fill:#C8E6C9,stroke:#000,stroke-width:1px,color:#000000
    style BubbleCheck fill:#C8E6C9,stroke:#000,stroke-width:1px,color:#000000
    style Subpath fill:#E1BEE7,color:#000000
    style SubCtxCheck fill:#C8E6C9,color:#000000
    style Stop2 fill:#FFE0B2
    style n3 fill:#FFE0B2
    style StartRoutes fill:#BBDEFB,stroke:#000,stroke-width:1px,color:#000000
    style RoutesComponent fill:#E1BEE7,color:#000000
    style n39 fill:#FFE0B2
    style ComponentValidatorBaseEditContextCascadingParameter fill:#BBDEFB,stroke:#000,stroke-width:1px,color:#000000
    style n36 fill:#FFF9C4,stroke:#000,color:#000000
    style n37 fill:#dfd,color:#000000
    style n38 fill:#fdd,color:#000000
    style ComponentValidatorBaseClass fill:#E1BEE7,stroke:#000,stroke-width:1px,color:#000000
    style componentValidatorBaseNesting fill:transparent
    style n12 fill:#FFE0B2
```

## Event Handling

```mermaid
---
config:
  layout: elk
  theme: base
  look: classic
---
flowchart TD
 subgraph s8["ComponentValidatorRoutes Event Handling"]
        n16["OnValidationRequested (RootEditContext)"]
        n17@{ label: "<span style=\"padding-left:\">The\n component associated to that edit context that was triggered by the \nOnValidationRequested event won't delegate model validation request to \nIComponentValidator; the model validation request become a no-op.</span>" }
        n18["OnFieldChanged (ActorEditContext)"]
        n19["The
 component associated to that edit context that was triggered by the 
OnFieldChanged event delegates a direct or a nested field validation request to IComponentValidator."]
        n20["EditContext Event Handling"]
  end
 subgraph s9["ComponentValidatorBase Event Handling"]
        n21["OnValidationRequested (ActorEditContext)"]
        n22["The
 validation request bubbles up to RootEditContext."]
        n23["Write to Root & Actor Stores"]
        n24["OnFieldChanged (ActorEditContext)"]
        n25["The
 component associated to that edit context that was triggered by the 
OnFieldChanged event runs ValidateDirectField(..) or ValidateNestedField(..,..)"]
        n32["OnValidationRequested (RootEditContext)"]
        n33@{ label: "<span style=\"padding-left:\">The\n component associated to that edit context that was triggered by the \nOnValidationRequested event runs ValidateModel()</span>" }
        n34["EditContext Event Handling"]
  end
    n16 --> n17
    n18 --> n19
    n20 --> n18 & n16
    n21 --> n22
    n24 --> n25
    n25 --> n23
    n32 --> n33
    n33 --> n23
    n34 --> n21 & n24 & n32
    s8 --> s9
    n16@{ shape: rect}
    n17@{ shape: rect}
    n18@{ shape: rect}
    n19@{ shape: rect}
    n20@{ shape: rect}
    n21@{ shape: rect}
    n22@{ shape: rect}
    n23@{ shape: rect}
    n24@{ shape: rect}
    n25@{ shape: rect}
    n32@{ shape: rect}
    n33@{ shape: rect}
    n34@{ shape: rect}
    style n16 fill:#FFE0B2,color:#000000
    style n18 fill:#FFE0B2,color:#000000
    style n21 fill:#FFE0B2,color:#000000
    style n23 fill:#fab,stroke:#000,stroke-width:1px,color:#000000
    style n24 fill:#FFE0B2,color:#000000
    style n32 fill:#FFE0B2,color:#000000
```
