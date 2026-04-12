# Getting Started

This chapter describes a first simple workflow with MCU UI Studio for LVGL.

The goal is not to explain every feature in detail yet, but to get through a
complete short cycle:

- choose a project directory
- create or load a screen
- place the first widgets
- check the preview
- save the screen and generate code

## 1. Open a Project Directory

At startup the application first asks for a project directory.

This uses an internal directory dialog inside the application. There you can:

- choose an existing project directory
- create a new directory
- use an empty directory for a new project

If an empty directory is selected, the application creates the project
structure there. This especially includes:

- the project file `*.lvglproj`
- the folder `screens/`
- the folder `build/`

If the directory already contains a project file, the editor continues working
with that project.

The file and folder selection is intentionally handled through internal
application dialogs rather than native platform dialogs. This gives more
consistent behavior and proved more robust especially on macOS.

!!! tip "Tip"
    For the first run, an empty project directory or a very small example
    project is usually the easiest start.

## 2. Create or Load the First Screen

After opening the project directory, you can either load an existing screen or
create a new screen file.

Screens are stored under `screens/`. Internally they are based on a JSON model
shared by the editor, simulator, and code generation.

For a first start, a simple screen with only a few widgets is usually the best
choice.

## 3. Understand the Main UI Layout

The application is divided into several work areas:

- **Toolbox** on the left
- **Structure tree** in the center
- **Properties** to the right of the structure tree
- **Diagnostics** in the lower section
- **Simulator preview** in the main right area
- **Manual panel** on the far right

The toolbox shows available widgets. The structure tree shows the current
screen hierarchy. The properties panel shows the attributes of the currently
selected element.

## 4. Place the First Widgets

A typical first screen uses only a few simple widgets, for example:

- `view`
- `button`
- `label`
- `slider`

Widgets are inserted from the toolbox into the screen or into a suitable
container. They can then be selected in the structure tree and edited through
the property editor.

For first steps, these values are especially useful:

- `id`
- position and size
- text for labels and buttons
- simple widget-specific values such as slider or bar values

## 5. Selection and Preview

The simulator preview shows the current screen state.

When an element is selected in the structure tree, the corresponding widget is
highlighted in the simulator as well. This makes it easy to see which object
is currently being edited.

This becomes especially helpful for larger screens with several similar
widgets or nested views.

## 6. Supported and Not Yet Fully Supported Areas

The editor marks widgets and properties that are not yet fully supported.

These areas are highlighted intentionally so that it becomes visible early
which elements already behave reliably in the simulator and display path and
where restrictions still exist.

The basic principle is:

- what is marked as supported should match in simulator and display path
- what is not yet fully supported should not silently look fully usable

!!! warning "Attention"
    Not-yet-fully-supported widgets or properties should not first be tried
    only on the final target system. The markings in the editor are meant as
    an early warning.

## 7. Save and Generate Code

After the first changes, the screen can be saved.

After that, code generation can be executed. The generated code follows LVGL C
style and represents the current screen state for later use in the target
project.

The generated files are written into the project context and can then be
integrated into the MCU or application workflow.

## 8. What to Read Next

After this first cycle, the following chapters are usually the most useful:

- **User Interface**, to understand the work areas in more detail
- **Concepts**, to better understand model, preview, and generation
- **Widgets**, as a reference for individual elements
- **Examples**, to see typical screen structures in context

At that point the first complete workflow is finished and the basic structure
of the tool is visible.
