# Examples

Dieses Verzeichnis sammelt die aktiven Beispielprojekte fuer
`MCU UI Studio for LVGL`.

Struktur:

- Editor-Beispiele liegen direkt unter `examples/`
- zugehoerige ESP32-Zielprojekte liegen unter `examples/targets/`

Aktuelle Editor-Beispiele:

- `portal`
  RTOS-Messages-Beispiel mit zugehoerigem ESP32-Referenzprojekt
- `kachel`
  Standard-Beispiel mit Dispatcher-, Action- und Update-Pfad
- `widgets`
  einfache Widget-Galerie ohne Event- und Update-Demo

Ziel der Struktur:

- Editor-Projekt und Zielprojekt bleiben klar gekoppelt
- generierte Dateien lassen sich gegen das reale ESP32-Projekt pruefen
- Beispiele bleiben klein genug, um als Referenz und Testfall nutzbar zu sein
