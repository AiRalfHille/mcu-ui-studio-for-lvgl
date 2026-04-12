# Project Template: Standard

MCU UI Studio for LVGL supports different project templates so that the editor
can adapt to different target structures.

The **Standard** template is the simpler and more direct workflow. It is meant
for projects where the screen, its widgets, and the generated LVGL C code are
used without an additional contract structure or RTOS-specific message path.

## Purpose of the Standard Template

The Standard template is intended for classic LVGL projects.

It is especially useful when:

- screens should be edited directly
- the focus is on layout, widgets, and properties
- generated LVGL C code should be reused as directly as possible
- no additional message or contract mechanism is the main concern

That makes it the natural choice for getting started and for many use cases
without extra system abstraction.

!!! tip "Tip"
    The Standard template is usually the best choice when layout, widgets, and
    readable LVGL C code are the main focus.

## Character of the Template

The Standard template follows a direct work model:

- edit the screen in the editor
- define structure and properties
- verify the result in the simulator
- generate LVGL C code

The focus is on visible screen structure and on a transparent relation between
editor state and generated code.

## Typical Use

The Standard template is especially suitable for:

- small and medium LVGL projects
- simple to moderate UI structures
- projects with a direct display-side integration
- learning and prototyping scenarios
- cases where readable and directly usable generated code matters

## Relation to Code Generation

Even in the Standard template, work is based on the same internal screen model
as in the rest of the tool.

The difference is not the model itself, but how the project is understood:

- more direct
- less formalized
- without focusing on contract or queue-based structures

That keeps the Standard template particularly suitable for projects where the
screen and its LVGL C output are the main concern.

## Relevant Attribute Groups

Even in Standard mode, some attribute groups go beyond pure layout or style.

The **Data** group especially includes:

- `id`
- `useUpdate`

These values help identify widgets clearly and prepare them for later updates
or structured reuse.

There is also an **MCU Integration** area with attributes such as:

- `callback`
- `action`
- `parameter`
- `eventGroup`
- `eventType`
- `useMessages`

These fields go beyond pure widget description and prepare MCU-side
integration in a more structured way.

The editor highlights these technical fields together with `id` and
`useUpdate` so it stays visible which values affect generators and target-side
integration.

## How Event Information Is Used in the Standard Path

In the current Standard generator, event information is no longer treated only
as loose callback metadata. It is transferred into a more MCU-oriented binding
structure.

The roles are important:

- `eventGroup` creates one shared dispatcher function per group
- `eventType` distinguishes functional subcases within that group
- `action` describes the primary functional meaning of the event
- `parameter` is carried as a typed additional value
- value widgets such as `slider`, `bar`, `arc`, or `spinbox` can additionally
  provide their current runtime value together with type information

That means the generated Standard code is already functionally readable for
MCU projects:

- which object raised the event
- which `action` it means
- which optional `parameter` is attached
- whether an actual runtime value is included as well

The LVGL trigger itself, such as `clicked` or `released`, remains mainly an
internal generator detail for `lv_obj_add_event_cb(...)`. For MCU-side logic,
the more important fields are `action`, `parameter`, `eventGroup`,
`eventType`, and optional runtime values.

## Dispatcher Behavior in the Standard Path

If multiple events use the same `eventGroup`, the generator creates exactly
one shared dispatcher function for that group.

Inside that function, the generated code typically distinguishes step by step:

- first by the actual LVGL event such as `CLICKED` or `RELEASED`
- then by `eventType`
- and, if needed, by the concrete source object

For MCU developers that means `eventGroup` is not decorative. It directly
changes the structure of the generated C code.

## Typed Parameter and Runtime Value

The Standard path now distinguishes, like the RTOS path, between two value
kinds:

- a free event `parameter`
- the current runtime value of a value widget

Both are prepared in typed form inside the generated binding.

That allows a `slider`, for example, to:

- carry an additional text parameter such as `WARNING`
- and at the same time provide its current numeric value separately

This is especially helpful for ESP32 and other MCU projects because functional
extra information and live widget values remain clearly separated.

## When Standard Is the Right Choice

The **Standard** template is usually the right choice when:

- a project is just being started
- UI structure is the first priority
- the editor should be used without additional system logic
- generated LVGL C code should be integrated directly into the target project

For many workflows, it is the natural starting point.

## Difference from the RTOS-Messages Template

In contrast to **RTOS-Messages**, the Standard template does not require a
more formalized model around events, IDs, and UI-to-MCU communication.

Still, the current Standard path is already more than raw LVGL callback code:
it transports `action`, typed `parameter`, and optional runtime values so that
the MCU side can continue without losing information.

RTOS-Messages is therefore better suited to projects where the UI should be
embedded in a more formal message or contract structure.

The Standard template remains the more direct and simpler path.

## Target System Examples

Complementing the editor examples, this template can also be paired with
simple target-system projects that show how the generated code is integrated
into a concrete MCU application.

The purpose of these projects is not to provide complete reference products,
but to make the transition from editor to target system as clear and
traceable as possible.
