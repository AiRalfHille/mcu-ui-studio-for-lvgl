# Widgets — Übersicht

Dieses Kapitel beschreibt die im aktuellen Metamodell vorhandenen Widgets von
MCU UI Studio for LVGL.

Jedes Widget wird in demselben Raster beschrieben:

- Zweck
- typische Verwendung
- wichtige Properties
- Kinder erlaubt oder nicht
- Unterstützungsstand

## Grundsatz

Nicht jedes im Metamodell vorhandene Widget ist im aktuellen Stand bereits
vollständig umgesetzt.

Der Unterstützungsstand bezieht sich deshalb nicht nur auf die Existenz im
Metamodell, sondern darauf, ob ein Widget im Editor, im Simulator und im
relevanten Generierungspfad sinnvoll zusammenpasst.

!!! note "Hinweis"
    Ein Widget kann also im Metamodell vorhanden sein und trotzdem noch nicht
    als vollständig unterstützt gelten. Maßgeblich ist der durchgängige
    Zusammenhang zwischen Modell, Vorschau und Generierung.

## Was ein Widget im Editor bedeutet

Ein Widget ist ein einzelner funktionaler Baustein eines Screens.

Je nach Typ kann ein Widget:

- Struktur bereitstellen
- Inhalte anzeigen
- Eingaben entgegennehmen
- Werte darstellen
- grafische Elemente erzeugen

Ein Screen besteht somit nicht aus freier Grafik, sondern aus einer geordneten
Struktur von Widgets mit definierten Eigenschaften und Beziehungen.

## Allgemeine Attribute vieler Widgets

Viele Widgets besitzen unabhängig von ihrem genauen Typ einige gemeinsame oder
wiederkehrende Attribute.

Dazu gehören vor allem:

- `id`  
  Dient der eindeutigen Bezeichnung eines Widgets im Modell und ist besonders
  wichtig für spätere Zuordnung, Updates und Contracts.
- `x` und `y`  
  Steuern die Position eines Widgets.
- `width` und `height`  
  Steuern die Größe eines Widgets.
- `align`  
  Bestimmt die Ausrichtung eines Widgets innerhalb seines Bezugskontexts.
- Stilbezogene Angaben  
  Zum Beispiel Farben, Rahmen, Deckkraft oder andere visuelle Eigenschaften.
- inhaltliche Grundwerte  
  Etwa `text`, `value`, `minValue`, `maxValue` oder `src`, abhängig vom
  Widgettyp.
- `useUpdate`  
  Dient bei unterstützten Strukturen dazu, ein Widget gezielt für spätere
  Update-Pfade vorzubereiten.

Nicht jedes Widget besitzt alle diese Attribute, aber viele Screens entstehen
aus genau dieser Kombination:

- Struktur
- Positionierung
- Sichtbarkeit
- fachlicher Inhalt
- und bei Bedarf eine eindeutige technische Zuordnung

## *Widget-Properties und MCU-Properties*

*Im praktischen Arbeiten ist es wichtig, zwischen zwei Arten von Eigenschaften zu
unterscheiden:*

- *eigentliche Widget-Properties*
- *technische MCU-/Generator-Properties*

*Zu den eigentlichen Widget-Properties gehören zum Beispiel:*

- *`text`*
- *`width`*
- *`height`*
- *`x`*
- *`y`*
- *`backgroundColor`*
- *`borderWidth`*
- *`minValue`*
- *`maxValue`*

*Diese Werte beschreiben das Widget selbst, also Aussehen, Position, Größe oder
seinen fachlichen Grundzustand.*

*Daneben gibt es Eigenschaften, die stärker für Contract, Code-Generierung und
MCU-Integration gedacht sind:*

- *`id`*
- *`useUpdate`*
- *`callback`*
- *`action`*
- *`parameter`*
- *`eventGroup`*
- *`eventType`*
- *`useMessages`*

*Diese Angaben gehören funktional nicht mehr nur zur Widgetbeschreibung. Sie
bestimmen mit, wie Generatoren Handles, Events, Nachrichtenpfade und
Update-Zugänge ableiten.*

*Der Property-Bereich hebt diese technische Gruppe deshalb bewusst hervor. In
beiden Tabs werden diese Eigenschaften grün und fett angezeigt.*

## Strukturbildende Widgets

### `screen`

- Zweck: Wurzelelement eines Screens.
- Typische Verwendung: oberste Fläche für Breite, Höhe und Screen-Struktur.
- Wichtige Properties: `name`, `width`, `height`, Hintergrund- und Layoutwerte.
- Kinder erlaubt oder nicht: ja, `screen` ist der Einstiegspunkt für die restliche Struktur.
- Unterstützungsstand: unterstützt.

### `view`

- Zweck: allgemeiner Container und zentrale Layout-Fläche.
- Typische Verwendung: Bereichsaufteilung, Gruppierung von Widgets, Unterstruktur eines Screens.
- Wichtige Properties: Position, Größe, Layout-, Scroll- und Stilwerte.
- Kinder erlaubt oder nicht: ja, `view` ist der wichtigste allgemeine Container.
- Unterstützungsstand: unterstützt.

### `list`

- Zweck: Container mit listenartigem Aufbau.
- Typische Verwendung: strukturierte Eintragslisten oder Navigationsbereiche.
- Wichtige Properties: Layout- und Listeneigenschaften.
- Kinder erlaubt oder nicht: grundsätzlich ja, aber aktuell nicht vollständig geführt.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `menu`

- Zweck: menüartige Containerstruktur.
- Typische Verwendung: Navigations- oder Einstellungsbereiche.
- Wichtige Properties: menübezogene Struktur- und Zustandswerte.
- Kinder erlaubt oder nicht: ja, konzeptionell containerartig.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `messageBox`

- Zweck: modaler Dialog- oder Hinweiscontainer.
- Typische Verwendung: Meldungen, Bestätigungen, Hinweise.
- Wichtige Properties: Text-, Sichtbarkeits- und Strukturwerte.
- Kinder erlaubt oder nicht: konzeptionell ja, aktuell aber nicht vollständig umgesetzt.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `tabView`

- Zweck: Container mit Reitern.
- Typische Verwendung: mehrseitige Oberflächen innerhalb eines Screens.
- Wichtige Properties: tab-bezogene Struktur und Auswahl.
- Kinder erlaubt oder nicht: ja, über Tab-Unterstruktur.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `tabViewTabBar`

- Zweck: Reiterleiste eines `tabView`.
- Typische Verwendung: Verwaltung der Tabs.
- Wichtige Properties: tab-spezifische Anzeige.
- Kinder erlaubt oder nicht: nur im Zusammenhang mit `tabView` sinnvoll.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `tabViewTab`

- Zweck: einzelner Tab-Inhalt.
- Typische Verwendung: Teilfläche innerhalb eines `tabView`.
- Wichtige Properties: Tab-Name und Strukturwerte.
- Kinder erlaubt oder nicht: ja, als Container für Tab-Inhalte.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `tabViewTabButton`

- Zweck: einzelner Reiter-Button.
- Typische Verwendung: Navigation innerhalb eines `tabView`.
- Wichtige Properties: Beschriftung und Tab-Zuordnung.
- Kinder erlaubt oder nicht: normalerweise nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `tileView`

- Zweck: kachel- oder seitenartige Containerstruktur.
- Typische Verwendung: swipebare Flächen oder Seitenraster.
- Wichtige Properties: Struktur- und Navigationswerte.
- Kinder erlaubt oder nicht: ja, als Container.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `win`

- Zweck: fensterartiger Container.
- Typische Verwendung: eigenständige Bereiche mit Titel und Inhalt.
- Wichtige Properties: Titel-, Inhalts- und Strukturwerte.
- Kinder erlaubt oder nicht: ja, konzeptionell Container.
- Unterstützungsstand: noch nicht vollständig unterstützt.

## Eingabe- und Standard-Widgets

### `button`

- Zweck: Auslösen einer Aktion.
- Typische Verwendung: Befehle, Navigation, Bedienhandlungen.
- Wichtige Properties: `id`, Größe, Position, Stil, Event-Angaben.
- Kinder erlaubt oder nicht: technisch möglich, aber im Modell nicht die bevorzugte Struktur für inhaltliche Unterelemente.
- Unterstützungsstand: unterstützt.

### `checkbox`

- Zweck: boolesche Auswahl.
- Typische Verwendung: Optionen mit ein/aus-Zustand.
- Wichtige Properties: `text`, Status, Größe, Position.
- Kinder erlaubt oder nicht: normalerweise nein.
- Unterstützungsstand: unterstützt.

### `dropdown`

- Zweck: Auswahl aus einer Liste.
- Typische Verwendung: Moduswahl, Einstellungswerte, kompakte Optionen.
- Wichtige Properties: Optionen, Auswahlwert, Größe, Position.
- Kinder erlaubt oder nicht: normalerweise nein.
- Unterstützungsstand: unterstützt.

### `label`

- Zweck: Textdarstellung.
- Typische Verwendung: Beschriftungen, Statuswerte, Hinweise.
- Wichtige Properties: `text`, `longMode`, Ausrichtung, Größe, Position.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `roller`

- Zweck: rollende Auswahl.
- Typische Verwendung: Picklisten, Zahlen- oder Optionsauswahl.
- Wichtige Properties: Optionen, sichtbare Zeilen, Auswahl.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `slider`

- Zweck: Wertauswahl über einen Schieber.
- Typische Verwendung: Bereichswerte wie Geschwindigkeit, Lautstärke oder Helligkeit.
- Wichtige Properties: `minValue`, `maxValue`, `value`, `orientation`, `mode`.
- Kinder erlaubt oder nicht: technisch möglich, aber nicht als bevorzugte Struktur gedacht.
- Unterstützungsstand: unterstützt.

### `spinbox`

- Zweck: numerische Eingabe oder Auswahl.
- Typische Verwendung: Zahlenwerte mit definiertem Wertebereich.
- Wichtige Properties: Min/Max, Schrittweite, Ziffernlogik.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `switch`

- Zweck: Schaltzustand ein/aus.
- Typische Verwendung: aktiv/inaktiv, an/aus, boolesche Zustände.
- Wichtige Properties: Zustand, Größe, Position, Orientierung.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `textArea`

- Zweck: Texteingabe.
- Typische Verwendung: Benutzereingaben, freie Texte, Eingabefelder.
- Wichtige Properties: Text, Placeholder, Cursor- und Eingabeverhalten.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `buttonMatrix`

- Zweck: mehrere Buttons in einer gemeinsamen Matrix.
- Typische Verwendung: Keypads, kompakte Tastenfelder.
- Wichtige Properties: Button-Definitionen, Matrix-Layout, Auswahlzustände.
- Kinder erlaubt oder nicht: nein, da die Matrix ihre Buttons intern verwaltet.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `calendar`

- Zweck: Kalenderdarstellung.
- Typische Verwendung: Datumswahl oder Terminbezug.
- Wichtige Properties: Datum, markierte Tage, Header-Verhalten.
- Kinder erlaubt oder nicht: nein, arbeitet eher als spezialisiertes Widget.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `calendarHeaderArrow`

- Zweck: Navigationspfeile für Kalender-Header.
- Typische Verwendung: Monats- oder Bereichswechsel.
- Wichtige Properties: Richtung und Zuordnung.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `calendarHeaderDropdown`

- Zweck: Dropdown-Header für den Kalender.
- Typische Verwendung: Monats- oder Jahreswahl.
- Wichtige Properties: Auswahl- und Headerwerte.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `dropdownList`

- Zweck: interne oder getrennte Listenansicht eines Dropdowns.
- Typische Verwendung: spezialisierte Auswahlansichten.
- Wichtige Properties: Auswahl- und Anzeigeverhalten.
- Kinder erlaubt oder nicht: normalerweise nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `imageButton`

- Zweck: Button mit Bildcharakter.
- Typische Verwendung: Icon-Buttons oder zustandsabhängige Grafiktasten.
- Wichtige Properties: Bildquellen je Zustand, Größe, Position.
- Kinder erlaubt oder nicht: normalerweise nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `keyboard`

- Zweck: Bildschirmtastatur.
- Typische Verwendung: Eingabe in Verbindung mit `textArea`.
- Wichtige Properties: Modus, Zuordnung zum Eingabefeld, Layout.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

## Anzeige- und Wert-Widgets

### `arc`

- Zweck: Bogen- oder Kreisabschnitt zur Wertdarstellung.
- Typische Verwendung: Skalen, Rundanzeigen, Eingabe per Arc.
- Wichtige Properties: `minValue`, `maxValue`, `value`, Winkelbereich.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `arcLabel`

- Zweck: Text im funktionalen Zusammenhang mit einem Arc.
- Typische Verwendung: Wert- oder Statusbeschriftung rund um Arc-Darstellungen.
- Wichtige Properties: Text, Ausrichtung, Arc-Bezug.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `bar`

- Zweck: Balkenförmige Wertanzeige.
- Typische Verwendung: Fortschritt, Füllstand, einfache Werte.
- Wichtige Properties: `minValue`, `maxValue`, `value`, `startValue`, `orientation`, `mode`.
- Kinder erlaubt oder nicht: technisch möglich, aber nicht als bevorzugte Struktur gedacht.
- Unterstützungsstand: unterstützt.

### `led`

- Zweck: kompakte Statusanzeige.
- Typische Verwendung: aktiv/inaktiv, Zustand sichtbar machen.
- Wichtige Properties: Helligkeit, Farbe, Größe.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `scale`

- Zweck: Skalen-Widget.
- Typische Verwendung: Teilstriche, Messskalen, Instrumente.
- Wichtige Properties: Min/Max, Teilung, visuelle Skalenwerte.
- Kinder erlaubt oder nicht: indirekt über `scaleSection`.
- Unterstützungsstand: unterstützt.

### `scaleSection`

- Zweck: Abschnitt innerhalb einer Skala.
- Typische Verwendung: hervorgehobene Wertebereiche, etwa grün/gelb/rot.
- Wichtige Properties: `minValue`, `maxValue`, abschnittsbezogene Stilwerte.
- Kinder erlaubt oder nicht: nein, gehört inhaltlich zu `scale`.
- Unterstützungsstand: unterstützt.

### `spinner`

- Zweck: animierte Aktivitätsanzeige.
- Typische Verwendung: Laden, Warten, Hintergrundaktivität.
- Wichtige Properties: Dauer, Stil, Größe.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `qrCode`

- Zweck: QR-Code-Darstellung.
- Typische Verwendung: Links, Codes, Maschinen- oder Benutzerübergaben.
- Wichtige Properties: Inhalt, Größe, Farbwerte.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

## Medien- und Grafik-Widgets

### `image`

- Zweck: statische Bilddarstellung.
- Typische Verwendung: Logos, Icons, Dekoration, Statusgrafiken.
- Wichtige Properties: Bildquelle, Rotation, Skalierung, Pivot, Position.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `line`

- Zweck: einfache Linienzeichnung.
- Typische Verwendung: Trennlinien, grafische Akzente, einfache Linienzüge.
- Wichtige Properties: `points`, `yInvert`, Farbe, Breite.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: unterstützt.

### `texture3d`

- Zweck: 3D-Textur- oder Effekt-Widget.
- Typische Verwendung: spezialisierte grafische Darstellung.
- Wichtige Properties: textur- und effektbezogene Angaben.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `animatedImage`

- Zweck: animierte Bilddarstellung.
- Typische Verwendung: einfache Bildfolgen oder Statusanimationen.
- Wichtige Properties: Bildquellen, Timing, Zustand.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `canvas`

- Zweck: freie Zeichenfläche.
- Typische Verwendung: eigene Grafiklogik, Pixel- oder Shape-Ausgaben.
- Wichtige Properties: Puffer, Größe, Zeichenkontext.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `lottie`

- Zweck: vektorbasierte Animationsdarstellung.
- Typische Verwendung: Statusanimationen oder animierte Icons.
- Wichtige Properties: Quelle, Wiedergabe, Timing.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

## Daten- und Struktur-Widgets

### `chart`

- Zweck: Diagramm-Widget.
- Typische Verwendung: Datenreihen, Kurven, Verlauf.
- Wichtige Properties: Achsen, Bereich, Datenreihen.
- Kinder erlaubt oder nicht: indirekt über Chart-Unterelemente.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `chartSeries`

- Zweck: Datenreihe eines Charts.
- Typische Verwendung: einzelne Mess- oder Wertreihe.
- Wichtige Properties: Reihenwerte, Stil, Zuordnung.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `chartCursor`

- Zweck: Cursor oder Marker innerhalb eines Charts.
- Typische Verwendung: Selektion oder Wertablesung.
- Wichtige Properties: Position, Stil, Chart-Zuordnung.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `chartAxis`

- Zweck: Achsenbeschreibung eines Charts.
- Typische Verwendung: Skalen, Beschriftung, Wertebereiche.
- Wichtige Properties: Achsenparameter.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `table`

- Zweck: tabellarische Darstellung.
- Typische Verwendung: strukturierte Daten in Zeilen und Spalten.
- Wichtige Properties: Zeilen, Spalten, Zellenwerte.
- Kinder erlaubt oder nicht: indirekt über Tabellenunterstruktur.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `tableColumn`

- Zweck: Spaltendefinition einer Tabelle.
- Typische Verwendung: Strukturierung von Tabellenlayout und Inhalt.
- Wichtige Properties: Spaltenbezug, Breite, Stil.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `tableCell`

- Zweck: einzelne Tabellenzelle.
- Typische Verwendung: Zellenwert oder Zellenstil.
- Wichtige Properties: Inhalt, Position, Stil.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

## Text- und Spezialtext-Widgets

### `spanGroup`

- Zweck: gruppierte Textspannen.
- Typische Verwendung: unterschiedlich formatierte Textsegmente innerhalb eines Bereichs.
- Wichtige Properties: Textsegmente, Stilzuordnung, Reihenfolge.
- Kinder erlaubt oder nicht: ja, über `spanGroupSpan`.
- Unterstützungsstand: noch nicht vollständig unterstützt.

### `spanGroupSpan`

- Zweck: einzelnes Textsegment innerhalb einer `spanGroup`.
- Typische Verwendung: Teiltexte mit eigener Formatierung.
- Wichtige Properties: Text, Stil, Segmentwerte.
- Kinder erlaubt oder nicht: nein.
- Unterstützungsstand: noch nicht vollständig unterstützt.

## Kernwidgets für den aktuellen Projektstand

Für den aktuellen Stand des Werkzeugs sind vor allem diese Widgets besonders
relevant und belastbar:

- `screen`
- `view`
- `button`
- `checkbox`
- `dropdown`
- `label`
- `roller`
- `slider`
- `spinbox`
- `switch`
- `textArea`
- `arc`
- `arcLabel`
- `bar`
- `led`
- `scale`
- `scaleSection`
- `spinner`
- `image`
- `line`

Mit diesen Widgets lässt sich bereits ein belastbarer Kernbereich von Screens
aufbauen, der im Editor, im Simulator und in den relevanten Generierungspfaden
sinnvoll zusammenpasst.

## Bezug zu den nächsten Kapiteln

Diese Seite ist als Überblick und Nachschlagehilfe gedacht.

Für den praktischen Einsatz sind außerdem relevant:

- **Benutzeroberfläche**, um zu sehen, wo Widgets im Editor bearbeitet werden
- **Konzepte**, um den Zusammenhang zwischen Modell, Vorschau und Generierung
  zu verstehen
- **Beispiele**, um Widgets im Zusammenhang echter Screens zu sehen
