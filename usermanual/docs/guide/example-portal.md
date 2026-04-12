# Beispiel: Portal

Das Beispiel `portal` ist ein Projekt auf Basis der Vorlage
`RTOS-Messages`.

Es dient als Referenz dafür, wie ein Screen im Editor aufgebaut wird, wenn der
Schwerpunkt nicht nur auf der Oberfläche selbst, sondern auch auf einer
strukturierten Weitergabe von Ereignissen und Updates liegt.

Für das Handbuch ist dieses Beispiel besonders interessant, weil es den
Übergang von der Editorseite in Richtung MCU-Integration andeutet.

## *Verbindung zwischen Editor und ESP32-Projekt*

*Das Beispiel besteht aktuell aus zwei zusammengehörigen Teilen:*

- *dem Editor-Projekt unter `examples/portal`*
- *dem zugehörigen ESP32-Zielprojekt unter `examples/targets/portal`*

*Dadurch lässt sich der Weg gut nachvollziehen:*

- *Screen im Editor bearbeiten*
- *Dateien generieren*
- *generierte Dateien im ESP32-Projekt verwenden*
- *Verhalten auf dem realen Display prüfen*

## *Rolle des Beispiels*

*`portal` zeigt zwei Dinge gleichzeitig:*

- *die Screen-Struktur einer typischen Portal- oder Übersichtsseite*
- *die Schichtung zwischen generiertem UI-Code und handgeschriebenem
  MCU-Anwendungscode*

*Auf der linken Seite liegen Eingabewidgets wie Buttons und ein `slider`. In der
Mitte befinden sich Status- und Rückkanal-Anzeigen. Dadurch eignet sich das
Beispiel gut, um Ereignisse und Rückmeldungen in beide Richtungen zu zeigen.*

## *Bestätigter RTOS-Pfad im Beispiel*

*Für den aktuellen Stand ist im Beispiel `portal` verifiziert:*

- *der RTOS-Generator überträgt Widgetwerte getrennt von freien Event-Parametern*
- *ein `slider` kann dadurch seinen aktuellen Wert senden und zusätzlich einen
  freien Parameter wie `WARNING` mitgeben*
- *das ESP-Beispiel verarbeitet beide Informationen sauber weiter*

*Im praktischen Ablauf bedeutet das:*

- *die Geschwindigkeit des Sliders kommt als eigener numerischer Wert an*
- *`WARNING` bleibt als zusätzlicher Textparameter erhalten*

*Damit ist `portal` aktuell ein belastbares Referenzbeispiel für:*

- *`id` als Contract-Zuordnung*
- *`action` als fachliche Bedeutung eines Events*
- *`parameter` als freien Zusatzwert*
- *`useUpdate` für Rückkanal- und Update-Ziele*

## *Was im ESP32-Zielprojekt dazu gehört*

*Das zugehörige Target `examples/targets/portal` zeigt, wie der generierte
RTOS-Pfad praktisch eingebunden wird:*

- *Display-Initialisierung*
- *Theme-Initialisierung*
- *generierter Contract-, Event- und Update-Code*
- *handgeschriebene Controller-/Fieldbus-Integration*

*Gerade diese Kombination macht `portal` zum Referenzbeispiel für den
RTOS-Messages-Pfad.*

## *Theme und Displaybezug*

*Im ESP-Referenzprojekt ist außerdem bestätigt:*

- *`theme_project.c` wird wirklich auf das Display angewendet*
- *`lv_conf_project.h` wird im Build berücksichtigt*
- *ein Dark Mode funktioniert grundsätzlich auf dem Zielsystem*

*Wichtig war dabei die Erkenntnis:*

- *theme-neutrale Screens funktionieren robuster als Screens mit fest hellen
  Standardfarben*

## *Farbformat*

*Für das aktuelle Referenzsystem gilt:*

- *SDL-Simulator: `16 Bit`*
- *ESP-Display: `16 Bit`, konkret `RGB565`*

*Das ist für das Beispiel relevant, weil Farben im Desktop-Preview und auf dem
realen Display sichtbar unterschiedlich wirken können.*
