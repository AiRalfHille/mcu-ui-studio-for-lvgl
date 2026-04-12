# Beispiele

MCU UI Studio for LVGL enthält mehrere Beispielprojekte, die den Einsatz des
Editors an konkreten Screens zeigen.

Diese Beispiele sollen nicht nur Funktionen auflisten, sondern einen
praktischen Einstieg erleichtern. Sie helfen dabei, die Arbeitsweise des
Editors, die Struktur eines Projekts und die Generierung im Zusammenhang zu
sehen.

## Zweck der Beispiele

Die Beispiele dienen vor allem dazu:

- typische Screen-Strukturen nachvollziehbar zu machen
- unterschiedliche Projektvorlagen zu zeigen
- Widgets im Zusammenhang zu sehen
- die Verbindung zwischen Editor, Vorschau und Generierung greifbarer zu machen

## Aktuell vorhandene Beispiele

### `portal`

- Projektvorlage: `RTOS-Messages`
- Zweck: Beispiel für einen Screen mit strukturierterem UI-MCU-Bezug
- Schwerpunkt: Vorlage mit Nachrichten- und Contract-Idee
- Editor-Projekt: `examples/portal`
- ESP32-Zielprojekt: `examples/targets/portal`

### `kachel`

- Projektvorlage: `Standard`
- Zweck: Beispiel für einen strukturierten Standard-Screen
- Schwerpunkt: klassischer Editor- und Generierungspfad
- Editor-Projekt: `examples/kachel`
- ESP32-Zielprojekt: `examples/targets/kachel`

### `widgets`

- Projektvorlage: `Standard`
- Zweck: Überblick über unterschiedliche Widgets in einem gemeinsamen Beispiel
- Schwerpunkt: Widget-orientiertes Ausprobieren und Prüfen
- Editor-Projekt: `examples/widgets`
- ESP32-Zielprojekt: `examples/targets/widgets`

## Verwendung der Beispiele

Die Beispiele können direkt im Editor geöffnet werden und dienen als praktische
Ausgangsbasis für:

- das Verständnis der Projektstruktur
- den Umgang mit Screens
- das Testen einzelner Widgets oder Eigenschaften
- den Vergleich zwischen Editorzustand und Generierung
- den Übergang vom Editor-Projekt in ein passendes ESP32-Referenzprojekt

## Weiterer Ausbau

Die Beispielseiten im Handbuch sollen die Verbindung zwischen zwei Ebenen
sichtbar machen:

- Editor-Beispiel unter `examples/...`
- zugehöriges ESP32-Zielprojekt unter `examples/targets/...`

Dabei ist besonders interessant:

- was ein Beispiel einem Anwender tatsächlich vermittelt
- welche Teile davon eher Einstieg sind
- und wie weit das jeweilige Beispiel bereits bis in ein MCU-Zielprojekt
  durchverbunden ist
