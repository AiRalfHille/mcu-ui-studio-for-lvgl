# User Interface: Overview

This chapter gives an overview of the main work areas of MCU UI Studio for
LVGL.

The following figure shows the editor's main window and the central areas that
matter in normal work.

![Editor main window](../assets/screenshots/UI-Editor.png)

## 1. Toolbox

The toolbox contains the widgets that are available in the current context.

This is where new elements are inserted into the screen. It also shows which
widgets are already supported and which are not yet fully implemented in the
preview and display path.

Depending on sorting and context, items are grouped or shown alphabetically.

## 2. Structure

The structure tree shows the current hierarchy of the screen.

It makes visible:

- which widgets exist on the screen
- how containers and sub-elements are nested
- which element is currently selected

The structure tree is the central view for the logical organization of the
screen.

## 3. Properties

The properties area is where the currently selected element is edited.

This includes in particular:

- general data such as `id`
- layout and sizing values
- widget-specific properties
- event-related fields

The available fields depend on the selected widget type. Properties that are
not yet fully supported are marked inside the editor.

## 4. Diagnostics

The diagnostics area collects technical feedback about the current document.

This includes:

- validation
- technical log
- event callbacks
- JSON preview

It helps make errors, inconsistencies, and the internal state of the current
screen easier to understand.

## 5. Status Bar

The status bar at the bottom shows the current application state in a compact
form.

Depending on the situation this includes:

- whether a simulator is active
- whether unsaved changes exist
- which LVGL version is being used
- which project mode is active
- which preview backend is active

This makes it a fast technical summary of the current state.

## 6. Toolbar

The toolbar at the top groups the most important actions of the application.

These include in particular:

- opening or creating a project
- editing theme and `lv_conf`
- creating, loading, and saving screen files
- code generation
- structure navigation and editing
- language switching

The toolbar is designed for short, frequent work paths.

## Working Style of the UI

The main window is designed so that structure, properties, and technical
feedback remain visible in parallel.

This reduces the need to constantly switch between separate dialogs or views.
Screen structure, element editing, and technical feedback remain visible in a
shared context.
