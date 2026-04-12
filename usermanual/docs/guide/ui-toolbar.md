# Benutzeroberfläche: Toolbar

Dieses Kapitel beschreibt die Toolbar im oberen Bereich der Anwendung.

![Toolbar](../assets/screenshots/toolbar.png)

## Aufgabe der Toolbar

Die Toolbar bündelt die wichtigsten Direktaktionen der Anwendung.

Sie ist auf häufige Arbeitsabläufe ausgelegt und ermöglicht den schnellen
Zugriff auf Funktionen, die sonst über mehrere Dialoge oder Zwischenschritte
erreichbar wären.

Die Toolbar ist dabei grob in mehrere Funktionsblöcke gegliedert:

- Anwendung und Projektkontext
- Screen-Dateien
- Bearbeitung des ausgewählten Elements
- Sprache
- Preview-Hilfe

## 1. Anwendung und Projektkontext

Im linken Bereich liegen die eher globalen Funktionen:

- `Beenden`
  Schließt die Anwendung.
- `Projekt`
  Öffnet den Projektdialog für Projektpfad, Ausgabepfade, Vorlage und weitere
  projektspezifische Einstellungen.
- `Theme`
  Öffnet den Theme-Dialog für `theme_project.c`.
- `lv_conf`
  Öffnet den Dialog zur Bearbeitung der projektbezogenen `lv_conf`.
- `Code generieren`
  Startet die MCU-Codegenerierung für das aktuelle Projekt.

Dieser Bereich betrifft vor allem Dinge, die nicht nur einen einzelnen Screen,
sondern den generellen Projektkontext betreffen.

## 2. Screen-Dateien

Danach folgt der Block für die direkte Arbeit mit Screen-Dateien:

- `Neu`
  Legt einen neuen Screen bzw. eine neue Screen-Datei an.
- `Öffnen`
  Öffnet eine vorhandene Screen-Datei aus dem Projekt.
- `Speichern`
  Speichert den aktuellen Stand der geöffneten Screen-Datei.

Dieser Block ist für den laufenden Bearbeitungsfluss besonders wichtig, weil er
den Einstieg in die Screen-Arbeit und das Zwischenspeichern direkt zugänglich
macht.

## 3. Struktur- und Elementbearbeitung

Im nächsten Block liegen die direkten Bearbeitungsaktionen für das aktuell
markierte Element:

- `Element hinzufügen`
  Fügt das aktuell im Werkzeugkasten gewählte Widget an der ausgewählten Stelle
  in die Struktur ein.
- `Duplizieren`
  Erstellt eine Kopie des aktuell markierten Elements.
- `Löschen`
  Entfernt das aktuell markierte Element aus der Struktur.
- `Nach oben`
  Verschiebt das markierte Element in der Hierarchie eine Position nach oben.
- `Nach unten`
  Verschiebt das markierte Element in der Hierarchie eine Position nach unten.

Diese Gruppe arbeitet eng mit Werkzeugkasten und Strukturbaum zusammen. Sie ist
vor allem dann hilfreich, wenn die Bearbeitung nicht über Drag & Drop, sondern
gezielt über direkte Aktionen erfolgen soll.

## 4. Sprache

Rechts in der Toolbar befindet sich die Sprachauswahl.

Dort kann die aktuelle Oberflächensprache direkt umgestellt werden, ohne einen
separaten Einstellungsdialog öffnen zu müssen.

Im Screenshot ist dabei zum Beispiel `Deutsch` ausgewählt.

## 5. Preview-Hilfe

Ganz rechts liegt aktuell ein zusätzlicher Schnellzugriff für die Preview:

- `Preview auf Originalgröße zurücksetzen`

Diese Funktion hilft dabei, die Vorschau wieder in einen klaren Ausgangszustand
zu bringen, wenn zuvor mit Größe oder Zoom gearbeitet wurde.

## Verwendung im Arbeitsablauf

Die Toolbar ist nicht als vollständiger Ersatz anderer Ansichten gedacht,
sondern als schneller Zugriff auf wiederkehrende Aktionen.

Sie ergänzt:

- den Werkzeugkasten
- den Strukturbaum
- die Properties
- die Dialoge für Projekt-, Theme- und `lv_conf`-Bearbeitung

Dadurch lassen sich viele Standardaktionen ohne Umweg direkt auslösen.
