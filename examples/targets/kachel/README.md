# esp32-ui-runtime

Dieses Repository ist als einfache ESP32-Laufzeit fuer `MCU UI Studio for LVGL` gedacht.

Ziele:

- generierte C-Dateien aus dem Editor auf einem physikalischen Display testen
- verschiedene Screens schnell mit derselben einfachen Runtime ausprobieren
- bewusst ohne zusaetzliche Controller-/Fieldbus-/Queue-Architektur starten

## Aktuelle Struktur

- `main/`
  minimales ESP-IDF-Anwendungsmodul
- `main/include/`
  Header fuer die Runtime-Module
- `main/generated/`
  Ziel fuer generierte Dateien aus dem Editor

## Idee dieses Templates

Dieses Projekt ist absichtlich schlicht gehalten:

- Display starten
- generierten Screen laden
- LVGL zyklisch im Runtime-Task bedienen

Die Runtime soll zunaechst nur das aufnehmen, was direkt aus dem Codegenerator
kommt. Weitere Architektur wie Controller, Fieldbus oder Queue-Modelle kann
spaeter als eigenes Template oder als Ausbauvariante folgen.

## Erwartete generierte Dateien

Unter `main/generated/` liegen typischerweise:

- `ui_start.c`
- `ui_start.h`
- optional weitere generierte `ui_*.c` Artefakte
- `lv_conf_project.h`
- `theme_project.c`

Die Build-Konfiguration bindet alle generierten `.c`-Dateien aus diesem Ordner
automatisch ein, ausgenommen `*_event.c`.
