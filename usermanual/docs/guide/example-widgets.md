# Beispiel: Widgets

Das Beispiel `widgets` dient als Sammelbeispiel für unterschiedliche
Widget-Typen in einem gemeinsamen Screen.

Es eignet sich besonders, um:

- unterstützte Widgets im Zusammenhang zu sehen
- einzelne Widget-Typen schnell auszuprobieren
- das Verhalten von Vorschau und Property-Editor zu vergleichen

Für das Handbuch ist dieses Beispiel vor allem als praktische Ergänzung zum
Kapitel **Widgets** interessant.

## *Verbindung zwischen Editor und ESP32-Projekt*

*Das Beispiel `widgets` gehört ebenfalls zu einem einfachen ESP32-Zielprojekt:*

- *Editor-Projekt: `examples/widgets`*
- *ESP32-Zielprojekt: `examples/targets/widgets`*

*Im Unterschied zu `portal` oder `kachel` ist dieses Beispiel bewusst sehr
einfach gehalten. Es dient nicht dazu, Event- oder Updatepfade zu zeigen,
sondern einen gemeinsamen Widget-Screen als Displayprojekt nutzbar zu machen.*

## *Was dieses Beispiel bewusst nicht zeigen soll*

*`widgets` ist keine Event-Demo und kein MCU-Contract-Beispiel. Deshalb gilt
hier bewusst:*

- *keine fachliche `m1`-Struktur*
- *keine Event-Dateien*
- *keine Update-Dateien*
- *kein zusätzlicher Laufzeitpfad im Zielprojekt*

*Das zugehörige ESP32-Target bleibt dadurch sehr schlank und eignet sich vor
allem dazu, den reinen Screen-Aufbau auf echter Hardware zu betrachten.*
