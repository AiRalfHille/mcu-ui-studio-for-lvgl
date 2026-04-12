# widgets target runtime

Simple ESP-IDF display project for the editor example `examples/widgets`.

This target is intentionally minimal:

- generated screen files live under `main/generated/`
- handwritten runtime code stays under `main/`
- there is no event or update demo in this example

The purpose of this project is to show the generated widget screen on real
ESP32-P4 hardware without adding extra controller, queue, or fieldbus logic.

Typical generated files in `main/generated/`:

- `ui_start.c`
- `ui_start.h`
- `lv_conf_project.h`
- `theme_project.c`
