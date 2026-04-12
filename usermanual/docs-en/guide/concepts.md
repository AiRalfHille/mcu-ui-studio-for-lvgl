# Concepts

This chapter describes the basic ideas behind MCU UI Studio for LVGL.

It is not about every single feature, but about the principles that connect
the editor, preview, and code generation.

## One Shared Model

A central concept of the tool is a shared internal model for screens and
widgets.

A screen is not treated only as a visual surface, but as a structured
description containing:

- element type
- attributes
- events
- child elements

Internally this model is managed as a JSON-based structure. That keeps a
screen understandable not only visually, but also technically.

## One Model for Multiple Paths

The same model is used by several parts of the application at once.

It is the basis for:

- the structure tree in the editor
- the property editor
- the native preview and simulator path
- the code generator

This avoids different parts of the tool working with different
interpretations of the same screen.

## Metamodel Instead of Unlimited Freedom

The tool does not allow arbitrary structures without rules. It works with its
own LVGL-oriented metamodel.

This metamodel defines:

- which widgets are known
- which properties belong to a widget
- which value types are used
- which child elements are allowed
- which areas are currently supported

The editor is therefore intentionally guided. The goal is not unlimited
freedom at any cost, but technical clarity and predictability.

## Visible Support Instead of Silent Assumptions

Another core principle is the visible indication of support level.

Widgets and properties are not only listed, but also evaluated as supported or
not yet fully supported.

The reason is simple:

- users should recognize early what can be used reliably
- the editor should not create false confidence
- differences between model, preview, and target path should not appear only
  late in the process

That is why not-yet-fully-supported areas are marked deliberately inside the
editor.

!!! warning "Attention"
    A visible widget in the editor does not automatically mean that it is
    fully supported in all paths. The decisive factor is the consistent link
    between model, preview, and generation.

## Preview and Display Should Match

The simulator preview is not meant to be only a visual approximation.

An important project goal is that what appears as supported in the simulator
should also reappear meaningfully in the display path.

That creates a strict measure:

- support should not exist only on paper
- a widget or property is considered really supported only when the relevant
  paths fit together

This principle helps users avoid searching for target-side problems that are
actually caused by inconsistent tool support.

## LVGL as a Target, Not a Black Box

The project is functionally based on LVGL.

LVGL is not treated merely as an external target system. Central LVGL
structures are mirrored in the tool's own model. The intention is to keep the
path from screen description to generated LVGL C code as transparent as
possible.

This clearly differs from tools that rely more heavily on opaque proprietary
intermediate models.

## Extended Structural Information

Beyond general widget properties, the model also contains structural
information that becomes useful later in real projects.

Examples include:

- `id`
- `useUpdate`

These fields make widgets not only visible, but technically addressable for
updates, mapping, and systematic reuse.

## Perspective on Generation and Binding

The tool does not end with visual screen editing.

The broader direction of the project also includes:

- clearer generator paths for MCU projects
- stronger typing of transferred values
- structured contracts between UI and application logic
- later, stronger bindings

Not every part of this is fully implemented yet, but it belongs to the
direction of the project.

!!! info "In Development"
    More advanced contracts, stronger typing, and later binding layers are
    part of the project direction, but they are not fully implemented in all
    areas yet.

## Summary

The most important concepts in MCU UI Studio for LVGL are:

- a shared JSON-based model
- a guided LVGL-oriented metamodel
- visible support markings
- a close relationship between editor, preview, and code generation
- a transparent path toward LVGL C code

These principles shape the tool more strongly than individual UI details.
