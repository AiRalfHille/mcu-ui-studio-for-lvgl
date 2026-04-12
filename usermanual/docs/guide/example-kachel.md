# Beispiel: Kachel

Das Beispiel `kachel` ist ein Standard-Projekt mit klar gegliedertem
Screen-Aufbau.

Es eignet sich gut, um eine klassische Struktur aus Views, Buttons, Labels und
weiteren Standard-Widgets im Zusammenhang zu betrachten.

Für das Handbuch ist es vor allem als verständliches Standard-Beispiel
interessant.

## *Verbindung zwischen Editor und ESP32-Projekt*

*Auch `kachel` besteht aktuell aus zwei passenden Teilen:*

- *dem Editor-Projekt unter `examples/kachel`*
- *dem zugehörigen ESP32-Zielprojekt unter `examples/targets/kachel`*

*Damit lässt sich im Standard-Pfad direkt vergleichen:*

- *welche Event- und Update-Dateien der Editor erzeugt*
- *wie diese Dateien in ein ESP32-Projekt übernommen werden*
- *wie sich das Verhalten anschließend auf dem Display zeigt*

## *Warum `kachel` als Standard-Beispiel wichtig ist*

*Im aktuellen Stand eignet sich `kachel` besonders gut, um den Event-Pfad der
Vorlage `Standard` sichtbar zu machen.*

*Das Beispiel zeigt naemlich:*

- *wie mehrere Widgets ueber eine gemeinsame `eventGroup` in einer Dispatcher-
  Funktion zusammenlaufen*
- *wie innerhalb dieses Dispatchers ueber `eventType` unterschieden wird*
- *wie `action` und ein getypter `parameter` im generierten Code auftauchen*
- *wie ein Wert-Widget wie der Slider seinen aktuellen Laufzeitwert separat
  mitliefern kann*

*Damit ist `kachel` ein gutes Referenzbeispiel fuer MCU-Entwickler, die nicht
nur den Screen-Aufbau sehen wollen, sondern auch verstehen moechten, was sie
spaeter im generierten `*_event.c` und `*_event.h` erwarten koennen.*

## *Rolle des ESP32-Targets*

*Das Target `examples/targets/kachel` ist bewusst so angepasst, dass es den
aktuellen Standard-Generatorpfad wirklich benutzt. Damit zeigt es nicht nur
einen statischen Screen, sondern die praktische Weiterverwendung von:*

- *`ui_start.c` und `ui_start.h`*
- *dem Standard-Eventpfad*
- *dem Standard-Updatepfad*

*Gerade fuer MCU-Entwickler ist das wichtig, weil `kachel` damit nicht nur ein
Editorbeispiel bleibt, sondern ein nachvollziehbarer Referenzpfad bis in das
ESP32-Projekt hinein ist.*
