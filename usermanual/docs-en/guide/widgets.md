# Widgets: Overview

This chapter describes the widgets currently present in the metamodel of MCU
UI Studio for LVGL.

Each widget is described using the same pattern:

- purpose
- typical use
- important properties
- whether children are allowed
- support status

## Principle

Not every widget that exists in the metamodel is already fully implemented.

The support status therefore does not depend only on presence in the metamodel,
but on whether the widget fits together meaningfully in the editor, simulator,
and relevant generation path.

!!! note "Note"
    A widget can exist in the metamodel and still not yet count as fully
    supported. The decisive factor is the consistent relation between model,
    preview, and generation.

## What a Widget Means in the Editor

A widget is a single functional building block of a screen.

Depending on its type, a widget can:

- provide structure
- display content
- accept input
- represent values
- draw graphical elements

A screen is therefore not free-form graphics, but an ordered structure of
widgets with defined properties and relations.

## Common Attributes of Many Widgets

Many widgets share some common or recurring attributes, independent of their
exact type. These include:

- `id`
- `x` and `y`
- `width` and `height`
- `align`
- style-related values such as colors, borders, or opacity
- content values such as `text`, `value`, `minValue`, `maxValue`, or `src`
- `useUpdate`

Not every widget provides all of these, but many practical screens are built
from exactly this combination:

- structure
- positioning
- visibility
- functional content
- and, if needed, technical identification

## Widget Properties and MCU Properties

In practical work it is useful to distinguish between two kinds of properties:

- actual widget properties
- technical MCU or generator properties

Typical widget properties include:

- `text`
- `width`
- `height`
- `x`
- `y`
- `backgroundColor`
- `borderWidth`
- `minValue`
- `maxValue`

These describe the widget itself: its appearance, position, size, or basic
functional state.

In contrast, the following properties are more strongly aimed at generation,
contracts, and MCU integration:

- `id`
- `useUpdate`
- `callback`
- `action`
- `parameter`
- `eventGroup`
- `eventType`
- `useMessages`

The properties panel highlights this technical group deliberately so their role
stays visible.

## Structuring Widgets

### `screen`

- Purpose: root element of a screen
- Typical use: top-level area for width, height, and screen structure
- Important properties: `name`, `width`, `height`, background and layout
  values
- Children allowed: yes
- Support status: supported

### `view`

- Purpose: general container and primary layout area
- Typical use: partitioning the screen, grouping widgets, nested structure
- Important properties: position, size, layout, scroll, and style values
- Children allowed: yes
- Support status: supported

### `list`, `menu`, `messageBox`, `tabView`, `tileView`, `win`

- Purpose: specialized container structures
- Typical use: menus, list-like areas, tabbed views, tile pages, or window-like
  sections
- Children allowed: conceptually yes
- Support status: currently not yet fully supported

## Input and Standard Widgets

### Fully Supported Core Widgets

The following widgets are currently part of the reliable core:

- `button`
- `checkbox`
- `dropdown`
- `label`
- `roller`
- `slider`
- `spinbox`
- `switch`
- `textArea`

Typical roles:

- `button`: trigger an action
- `checkbox` and `switch`: boolean choices
- `dropdown` and `roller`: choose from a set of values
- `label`: show text
- `slider` and `spinbox`: select or edit numeric values
- `textArea`: accept text input

### Additional Input-Related Widgets

The following input-oriented widgets already exist in the metamodel but are
not yet fully supported across all paths:

- `buttonMatrix`
- `calendar`
- `calendarHeaderArrow`
- `calendarHeaderDropdown`
- `dropdownList`
- `imageButton`
- `keyboard`

## Display and Value Widgets

### Supported Value and Display Widgets

The following are currently part of the supported core:

- `arc`
- `arcLabel`
- `bar`
- `led`
- `scale`
- `scaleSection`
- `spinner`

Typical use:

- `arc` and `bar`: value visualization
- `arcLabel`: text related to an arc
- `led`: compact state indicator
- `scale` and `scaleSection`: scale and highlighted ranges
- `spinner`: activity or waiting animation

### Partially Supported Display Widgets

The following widgets are present but not yet fully supported across all
relevant paths:

- `qrCode`

## Media and Graphics Widgets

### Supported

- `image`
- `line`

### Present but Not Yet Fully Supported

- `texture3d`
- `animatedImage`
- `canvas`
- `lottie`

## Data and Structure Widgets

The following widgets are currently present in the metamodel but not yet fully
supported:

- `chart`
- `chartSeries`
- `chartCursor`
- `chartAxis`
- `table`
- `tableColumn`
- `tableCell`

## Text and Special Text Widgets

The following text-oriented widgets are currently present but not yet fully
supported:

- `spanGroup`
- `spanGroupSpan`

## Current Core Widgets of the Project

For the current state of the tool, the following widgets are especially
relevant and reliable:

- `screen`
- `view`
- `button`
- `checkbox`
- `dropdown`
- `label`
- `roller`
- `slider`
- `spinbox`
- `switch`
- `textArea`
- `arc`
- `arcLabel`
- `bar`
- `led`
- `scale`
- `scaleSection`
- `spinner`
- `image`
- `line`

With these widgets, a meaningful and stable core set of screens can already be
built that fits together across editor, simulator, and the relevant generator
paths.

## Relation to the Next Chapters

This page is intended as an overview and reference.

For practical use, the following chapters are also important:

- **User Interface**, to see where widgets are edited in the editor
- **Concepts**, to understand the relation between model, preview, and
  generation
- **Examples**, to see widgets in the context of real screens
