# About This Manual

This manual accompanies MCU UI Studio for LVGL and is intended for users who
work with the tool, create projects, edit screens, and want to better
understand the generated LVGL code.

## Structure

The manual is organized into the following sections:

- **Introduction**: preface, manual orientation, license, and third-party
  components
- **Getting Started**: the first workflow in the tool
- **User Interface**: the main editor work areas
- **Project Templates**: the supported project modes and their differences
- **Widgets**: available widgets, their role in the editor, and their
  properties
- **Code Generation**: the structure and meaning of the generated files
- **Examples**: the included example projects and their corresponding ESP32
  targets

## Conventions

The following note types are used throughout the manual:

!!! note "Note"
    Additional information for better understanding.

!!! tip "Tip"
    A recommendation for efficient work.

!!! warning "Attention"
    Important information that should not be overlooked.

!!! info "In Development"
    This part is not fully implemented yet.

Code examples are shown in their own block:

```c
lv_obj_t *btn = lv_button_create(parent);
```

Filenames, paths, properties, and identifiers are highlighted inline like
`this`.

## How to Use This Manual

For a first orientation it makes sense to start with **Introduction** and
**Getting Started**. After that, the **User Interface** section helps put the
editor layout into context.

The later chapters on **Widgets**, **Project Templates**, and
**Code Generation** are primarily reference sections. They do not need to be
read from start to finish in one go.

The **Examples** section is meant as a practical extension. It helps relate
what you see in the editor to the generated artifacts and the corresponding
ESP32 target projects.

## Version Context

This manual refers to the current project state of MCU UI Studio for LVGL with
LVGL `9.4`.
