# Benutzeroberfläche: Statusleiste

Dieses Kapitel beschreibt die Statusleiste am unteren Rand der Anwendung.

![Statusleiste](../assets/screenshots/statusbar.png)

## Aufgabe der Statusleiste

Die Statusleiste zeigt den aktuellen technischen und editorbezogenen Zustand
der Anwendung in kompakter Form an.

Sie ist damit kein zusätzlicher Arbeitsbereich, sondern eine laufende
Orientierungshilfe. Besonders während der täglichen Arbeit lässt sich dort
schnell erkennen, ob der Editor verbunden, verändert oder in einem bestimmten
Projektmodus aktiv ist.

## 1. Simulator

Der Eintrag `Simulator` zeigt, ob die Preview bzw. der Simulator aktuell mit
dem Editor verbunden ist.

Typische Werte sind:

- `Ja`
- `Nein`

Damit lässt sich schnell einschätzen, ob Änderungen bereits an eine laufende
Vorschau weitergegeben werden können.

## 2. Änderungen

Der Eintrag `Änderungen` zeigt, ob seit dem letzten Speichern oder Laden ein
ungesicherter Bearbeitungsstand vorliegt.

Typische Werte sind:

- `Ja`
- `Nein`

Das ist besonders hilfreich, wenn zwischen mehreren Arbeitsschritten oder
Dateioperationen gewechselt wird.

## 3. LVGL

Der Eintrag `LVGL` zeigt die aktuell im Projekt eingestellte LVGL-Version.

Im Screenshot ist das zum Beispiel:

- `LVGL: 9.4`

Dieser Wert gehört zum Projektkontext und hilft dabei, Vorschau, Metamodell und
Generierung schneller einzuordnen.

## 4. Modus

Der Eintrag `Modus` zeigt den aktuell aktiven Projektmodus.

Typische Werte sind zum Beispiel:

- `LVGL`
- `LVGL-XML`

Der Modus beeinflusst, welche Generator- und Laufzeitpfade im Projekt aktiv
sind.

## 5. Strikt

Der Eintrag `Strikt` zeigt, ob die strenge Validierung im Projekt aktiv ist.

Typische Werte sind:

- `Ja`
- `Nein`

Damit ist direkt sichtbar, ob das Projekt mit stärkerer formaler Prüfung
arbeitet.

## 6. Backend

Ganz rechts zeigt die Statusleiste das aktive Preview-Backend an.

Im Screenshot ist das:

- `Backend: Native LVGL Preview (SDL)`

Dieser Wert ist hilfreich, wenn mehrere Preview-Pfade möglich sind oder wenn
bei Diagnose und Vergleich klar sein soll, auf welchem Backend die Vorschau
gerade läuft.

## Verwendung im Arbeitsablauf

Die Statusleiste ist besonders nützlich, um ohne Umweg zu erkennen:

- ob der Simulator verbunden ist
- ob noch ungespeicherte Änderungen vorliegen
- welche LVGL-Version gerade verwendet wird
- in welchem Modus das Projekt arbeitet
- ob strenge Validierung aktiv ist
- welches Preview-Backend verwendet wird

Gerade bei Projektwechseln, Diagnose oder Generatorarbeit ist diese kompakte
Zusammenfassung oft schneller erfassbar als ein Blick in mehrere Dialoge oder
Projektdateien.
