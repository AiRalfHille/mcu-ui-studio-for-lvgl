# Projektstatus

MCU UI Studio for LVGL ist ein nutzbares Werkzeug zur Erstellung,
Bearbeitung und Generierung von LVGL-Oberflächen und wird aktiv
weiterentwickelt.

Der aktuelle Schwerpunkt liegt auf einem klar strukturierten Editor,
einer nativen Simulator-Vorschau und der Generierung von LVGL-C-Code für
den Display-Pfad.

## Oeffentlicher Release-Status

Der aktuelle oeffentliche Release stellt bereits Desktop-Pakete bereit, bringt
aber weiterhin plattformspezifische Hinweise mit sich.

- Windows x64 steht als direktes Desktop-Paket bereit.
- macOS ARM64 wird aktuell als unsigniertes App-Bundle ausgeliefert.

Fuer das aktuelle macOS-Paket gilt:

- Archiv entpacken
- die App ueber `Start MCU UI Studio.command` starten
- das Hilfsskript entfernt lokal das Quarantine-Flag vom entpackten App-Bundle
  und oeffnet danach die App

Unter Windows wird das Handbuch derzeit im externen Browser geoeffnet, weil
der eingebettete WebView-Pfad aktuell noch nicht stabil genug ist.

## Wofür das Werkzeug gedacht ist

MCU UI Studio for LVGL richtet sich an Anwender, die:

- LVGL-Screens strukturiert entwerfen möchten
- Widgets und Properties in einem Editor bearbeiten wollen
- den Aufbau eines Screens im Simulator prüfen möchten
- generierten LVGL-C-Code nachvollziehen und weiterverwenden möchten
- eine klarere Verbindung zwischen UI-Modell, Vorschau und Zielsystem suchen

## Aktueller Kern des Projekts

Zum aktuellen Kern des Werkzeugs gehören insbesondere:

- Projekt- und Screen-Verwaltung
- strukturierter Widget-Baum
- typisierter Property-Editor
- nativer Preview- und Simulatorpfad
- Generierung von LVGL-C-Code
- Handbuchintegration im Editor

## Interne Grundlage

Screens werden intern in einem JSON-basierten Modell beschrieben.

Dieses Modell bildet die Grundlage dafür, dass Editor, Simulator und
Code-Generierung auf denselben strukturellen Informationen aufbauen.
Dadurch bleibt der Aufbau eines Screens nachvollziehbar und technisch
vergleichsweise transparent.

## Unterstützungsgrad

Das Werkzeug deckt bereits einen belastbaren Kernbereich ab, unterstützt
aber nicht in allen Bereichen den vollständigen LVGL-Umfang.

!!! note "Hinweis"
    Der Unterstützungsgrad bezieht sich nicht nur auf das Vorhandensein eines
    Widgets im Metamodell. Entscheidend ist, ob Editor, Vorschau und
    Generierung für diesen Bereich sinnvoll zusammenpassen.

Das bedeutet konkret:

- nicht jedes LVGL-Widget ist bereits vollständig umgesetzt
- nicht jede Property ist in allen Pfaden durchgängig verfügbar
- unterstützte und nicht vollständig unterstützte Bereiche werden im Editor
  bewusst kenntlich gemacht

## Grundsatz für Vorschau und Display

Ein wichtiger Grundsatz des Projekts ist, dass die Simulator-Vorschau und der
Display-Pfad fachlich möglichst zusammenpassen sollen.

Was im Simulator sichtbar und als unterstützt markiert ist, soll nicht erst
bei der Nutzung im Zielsystem zu unerwarteten Abweichungen führen.

Deshalb werden Widgets und Properties, die noch nicht durchgängig unterstützt
sind, im Editor bewusst markiert.

## Einordnung

MCU UI Studio for LVGL ist kein theoretischer Entwurf, sondern ein praktisch
nutzbares Werkzeug mit einem klaren Kernumfang.

Gleichzeitig wird das Projekt weiterhin erweitert und verfeinert. Der
Funktionsumfang wächst schrittweise, ohne den Anspruch aufzugeben, den
aktuellen Stand im Editor und im Handbuch ehrlich sichtbar zu machen.
