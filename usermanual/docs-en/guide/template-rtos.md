# Project Template: RTOS-Messages

The **RTOS-Messages** project template is intended for scenarios in which the
user interface is not treated only as a direct LVGL screen, but as part of a
more structured communication path between display and application logic.

Compared to the **Standard** template, the focus here is not only on generated
LVGL C code, but also on an explicit message and contract model.

## Purpose of the RTOS-Messages Template

This template is especially useful when:

- UI events should be forwarded in an ordered way to a controller or task
  structure
- updates from application logic should flow back into the UI systematically
- communication between display and MCU logic should be typed rather than ad
  hoc
- a queue or message architecture is already part of the overall project

This makes the template better suited to structured embedded projects with a
clear separation between display, event handling, and controller logic.

!!! warning "Attention"
    RTOS-Messages is not only a different generator path. It also requires
    more discipline around `id`, actions, and update assignments.

## Core Idea

The RTOS-Messages template treats a screen not only as a collection of visible
widgets, but as a set of interacting objects with defined roles.

Two directions are especially important:

- **Display to controller**
  - a widget raises an event
  - that event becomes a defined message
- **Controller to display**
  - application logic updates the state of a widget
  - this happens through clearly named update paths

That creates an explicit contract between UI and application logic.

## Role of IDs and Actions

For this approach to work, participating elements need additional structural
information.

The most important fields are:

- `id`
- event callbacks with an assigned `action`
- optional `useUpdate` for explicit update paths

In this template, `id` is more than an internal name. It becomes a stable key
inside the contract model.

That means a visual widget alone is not enough for event sources. It must also
be functionally identifiable.

## Relevant Attribute Groups

In this template, additional technical attributes play a particularly
important role.

The **Data** group especially includes:

- `id`
- `useUpdate`

These values make widgets not only visually defined, but also technically
addressable for contract and update paths.

The following fields are especially central in the Events area:

- `callback`
- `action`
- `parameter`
- `eventGroup`
- `eventType`
- `useMessages`

Their roles are not identical:

- `callback` is mainly the technical event assignment
- `action` is the functional meaning inside the contract
- `parameter` is a free additional value
- `eventGroup`, `eventType`, and `useMessages` structure the event path for
  generators and MCU integration

The editor highlights these fields together with `id` and `useUpdate` so that
their technical importance is immediately visible.

## Generated Contract

In RTOS-Messages mode, generation does not stop at general LVGL C code. It
also creates a structured contract layer.

Current artifacts include:

- a generated contract header
- event code for outgoing display events
- update code for incoming UI updates

The generator maps objects, actions, and parameters into typed structures.

The current path distinguishes between two value kinds:

- a free event `parameter`
- a separate runtime value of the widget

This is especially important for widgets such as `slider`, `bar`, `arc`, or
`spinbox`. An event can therefore carry both a functional extra parameter and
the actual widget value separately.

For understanding the RTOS path, one more point matters: the technical LVGL
trigger such as `clicked` or `released` remains necessary internally for
callback registration, but it is not exposed as the main contract field to the
controller side. On the contract side, `action`, `parameter`, and optional
runtime values are what matter.

## Relation to Queues and Messages

This template is meant to move UI events into a message model.

In the generated RTOS path, events are prepared in a way that allows them to
be forwarded as structured messages, for example into a queue-based controller
or task architecture.

This makes the template especially suitable for projects where:

- display and logic are decoupled
- events should not be handled directly in an ad hoc way
- updates should flow back to the UI in an ordered way

## What This Template Makes Stricter

The RTOS-Messages template demands more discipline than the Standard
template. This becomes visible because:

- IDs need to be unique
- event sources need a clear identity
- events are not meant without an assigned action
- the UI is treated as part of a contract

This additional structure is not an end in itself. It is intended to prevent
communication between UI and MCU logic from becoming unclear or difficult to
maintain.

## When RTOS-Messages Is the Right Choice

This template is a good fit when:

- a project already has a task or queue architecture
- display and control logic should remain clearly separated
- UI events should be forwarded formally to other system parts
- maintainability and traceability are more important than the shortest direct
  path

It is therefore usually not the simplest starting point, but often the better
path for systematically structured embedded applications.

## Difference from the Standard Template

Compared to **Standard**, RTOS-Messages is:

- more structured
- more formal
- more focused on events, actions, and updates
- closer to controller-oriented or task-oriented system integration

The Standard template remains the more direct path for classic LVGL projects.
RTOS-Messages is better suited when the UI should be built as part of a clear
message and contract structure.

## Target System Examples

For this template in particular, target system examples are valuable because
they show the path from the editor into a real MCU application.

Such projects can demonstrate:

- how the generated contract is integrated
- how messages and actions are processed on the MCU side
- how queue or task structures are connected to the UI

The goal of those examples is not complexity, but clarity and traceable
integration of the generated code into a real target system such as an ESP32
project.
