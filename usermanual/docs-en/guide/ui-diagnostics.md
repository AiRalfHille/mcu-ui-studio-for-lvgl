# User Interface: Diagnostics

This chapter describes the diagnostics area in the lower section of the
application.

![Diagnostics](../assets/screenshots/Diagnose.png){ width="860" }

## Purpose of the Diagnostics Area

The diagnostics area collects technical feedback about the current document.

It is not meant for building the screen itself, but for checking and
understanding the current state.

This makes it a technical companion to the actual editing workflow.

!!! tip "Tip"
    The diagnostics area is especially useful when it is not obvious whether a
    problem comes from the model, event data, or generation.

## Diagnostics Tabs

Several tabs are available in the diagnostics area. The current set includes:

- `Validation`
- `Technical Log`
- `Event Callbacks`
- `JSON Preview`

Each tab helps answer a different question.

## Validation

The `Validation` tab shows the result of the formal checks performed on the
current document.

It helps reveal whether the current screen model is internally consistent or
whether defined rules are being violated.

## Technical Log

The technical log makes internal messages and processing hints visible.

It is especially helpful when preview, generation, or related workflows need
to be traced.

## Event Callbacks

This section is focused on event logic and event-related output.

It becomes relevant when a screen is not only viewed visually, but also
examined in terms of callbacks and event structure.

## JSON Preview

The JSON preview shows the current document model in its structured form.

This makes the internal description of the screen visible and helps compare
the editor state with the underlying model directly.

## How It Is Used

The diagnostics area is usually used in a supporting role:

- after changing a screen
- when checking model structure
- when investigating errors or inconsistencies
- when comparing editor state and internal document model

That makes diagnostics an important tool for transparency and technical
control, not just an extra panel.

!!! note "Note"
    Diagnostics do not replace target-side integration. They are meant as an
    early verification and traceability layer inside the editor.
