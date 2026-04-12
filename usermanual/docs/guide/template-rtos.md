# Projektvorlage: RTOS-Messages

Die Projektvorlage **RTOS-Messages** ist für Anwendungsfälle gedacht, in denen
die Benutzeroberfläche nicht nur als direkter LVGL-Screen betrachtet wird,
sondern als Teil einer strukturierteren Kommunikation zwischen Display und
Anwendungslogik.

Im Unterschied zur Vorlage **Standard** liegt der Schwerpunkt hier nicht nur
auf dem erzeugten LVGL-C-Code, sondern zusätzlich auf einem geordneten
Nachrichten- und Contract-Modell.

## Zweck der RTOS-Messages-Vorlage

Die Vorlage ist besonders dann sinnvoll, wenn:

- UI-Ereignisse geordnet an eine Controller- oder Task-Struktur übergeben
  werden sollen
- Updates aus der Anwendungslogik systematisch zurück in die UI fließen sollen
- die Kommunikation zwischen Display und MCU-Logik nicht lose, sondern
  typisiert beschrieben werden soll
- eine Queue- oder Nachrichtenarchitektur Teil des Gesamtprojekts ist

Damit richtet sich diese Vorlage stärker an strukturierte Embedded-Projekte mit
klarer Trennung zwischen Anzeige, Ereignisverarbeitung und Controllerlogik.

!!! warning "Achtung"
    RTOS-Messages ist nicht nur ein anderer Generatorpfad, sondern verlangt
    auch fachlich mehr Disziplin bei `id`, Aktionen und Update-Zuordnung.

## Grundidee

Die RTOS-Messages-Vorlage betrachtet einen Screen nicht nur als Sammlung
sichtbarer Widgets, sondern zusätzlich als Menge interagierender Objekte mit
definierten Rollen.

Dabei sind vor allem zwei Richtungen wichtig:

- **Display zu Controller**
  - ein Widget löst ein Ereignis aus
  - daraus wird eine definierte Nachricht
- **Controller zu Display**
  - die Anwendungslogik aktualisiert den Zustand eines Widgets
  - dies geschieht über klar benannte Update-Pfade

Dadurch entsteht ein expliziter Contract zwischen Oberfläche und Anwendungslogik.

## Rolle von ids und Aktionen

Damit dieser Ansatz funktioniert, benötigen beteiligte Elemente zusätzliche
Strukturinformationen.

Besonders wichtig sind:

- `id`
- Event-Callbacks mit zugeordneter `action`
- optional `useUpdate` für gezielte Update-Pfade

Die `id` dient dabei nicht nur als interner Name, sondern als verlässliche
Zuordnung im Contract-Modell.

Für Ereignisquellen reicht in dieser Vorlage ein rein visuelles Widget daher
nicht aus. Es muss auch fachlich identifizierbar sein.

## Relevante Attributgruppen

In dieser Vorlage spielen zusätzliche technische Attribute eine besonders
wichtige Rolle.

Zur **Data**-Gruppe gehören insbesondere:

- `id`
- `useUpdate`

Diese Werte helfen dabei, Widgets nicht nur visuell zu beschreiben, sondern
für Contract- und Update-Pfade eindeutig greifbar zu machen.

*Zusätzlich spielen im Event-Bereich besonders diese Angaben eine zentrale Rolle:*

- *`callback`*
- *`action`*
- *`parameter`*
- *`eventGroup`*
- *`eventType`*
- *`useMessages`*

*Dabei ist die fachliche Bedeutung nicht für alle Felder gleich:*

- *`callback` beschreibt primär die technische Event-Zuordnung*
- *`action` beschreibt die fachliche Bedeutung im Contract*
- *`parameter` beschreibt einen freien Zusatzwert*
- *`eventGroup`, `eventType` und `useMessages` strukturieren den Eventpfad für
  Generatoren und MCU-Integration*

*Im Editor werden diese Eigenschaften zusammen mit `id` und `useUpdate` grün und
fett dargestellt, damit die technische Relevanz im RTOS-Messages-Modus sofort
sichtbar ist.*

## Generierter Contract

Im RTOS-Messages-Modus wird nicht nur allgemeiner LVGL-C-Code erzeugt, sondern
zusätzlich eine strukturierte Contract-Schicht.

Im aktuellen Stand gehören dazu insbesondere:

- ein generierter Contract-Header
- Event-Code für ausgehende Display-Ereignisse
- Update-Code für eingehende UI-Aktualisierungen

Der Generator bildet dabei Objekte, Aktionen und Parameter in einer
typisierten Form ab.

*Im aktuellen Stand wird dabei zwischen zwei Arten von Werten unterschieden:*

- *einem freien Event-`parameter`*
- *einem separaten Laufzeitwert des Widgets*

*Das ist vor allem für Wert-Widgets wie `slider`, `bar`, `arc` oder `spinbox`
wichtig. Dadurch kann ein Event sowohl einen fachlichen Zusatzparameter tragen
als auch den aktuellen Widgetwert separat an den Contract übergeben.*

*Wichtig fuer das Verstaendnis des RTOS-Pfads ist ausserdem: Der technische
LVGL-Ausloeser wie `clicked` oder `released` bleibt zwar intern fuer die
Callback-Registrierung notwendig, wird aber nicht als zentrales Contract-Feld
an die Controller-Seite weitergegeben. Im fachlichen Contract stehen stattdessen
`action`, `parameter` und gegebenenfalls der Laufzeitwert im Vordergrund.*

## Bezug zu Queue und Nachrichten

Die Vorlage ist darauf ausgelegt, UI-Ereignisse in ein Nachrichtenmodell zu
überführen.

Im generierten RTOS-Pfad werden Ereignisse so vorbereitet, dass sie als
strukturierte Meldungen weitergegeben werden können, zum Beispiel an eine
Queue-basierte Controller- oder Task-Struktur.

Damit ist die Vorlage besonders geeignet für Projekte, in denen:

- Display und Logik entkoppelt sind
- Ereignisse nicht direkt ad hoc verarbeitet werden sollen
- Updates wieder geordnet zurück in die Oberfläche laufen

## Was diese Vorlage strenger macht

Die RTOS-Messages-Vorlage verlangt mehr Disziplin als die Standard-Vorlage.

Das zeigt sich unter anderem darin, dass:

- ids eindeutig sein müssen
- Ereignisquellen eine klare Identität brauchen
- Ereignisse nicht ohne zugeordnete Aktion gedacht sind
- die UI stärker als Teil eines Contracts verstanden wird

Diese zusätzliche Struktur ist kein Selbstzweck. Sie soll verhindern, dass die
Kommunikation zwischen UI und MCU-Logik unübersichtlich oder schwer wartbar
wird.

## Wann RTOS-Messages die richtige Wahl ist

Die Vorlage **RTOS-Messages** ist sinnvoll, wenn:

- ein Projekt bereits eine Task- oder Queue-Architektur besitzt
- Display und Steuerlogik klar getrennt bleiben sollen
- UI-Ereignisse formalisiert an andere Systemteile übergeben werden sollen
- spätere Erweiterbarkeit und Nachvollziehbarkeit wichtiger sind als der
  kürzeste direkte Weg

Sie ist damit weniger der einfachste Einstieg, aber oft der bessere Weg für
systematisch aufgebaute Embedded-Anwendungen.

## Abgrenzung zur Standard-Vorlage

Im Vergleich zur Vorlage **Standard** ist RTOS-Messages:

- strukturierter
- formaler
- stärker auf Ereignisse, Aktionen und Updates ausgerichtet
- näher an einer controller- oder taskorientierten Systemintegration

Die Standard-Vorlage bleibt der direktere Weg für klassische LVGL-Projekte.
RTOS-Messages ist dagegen der geeignetere Modus, wenn die UI als Teil einer
klaren Nachrichten- und Contract-Struktur aufgebaut werden soll.

## Zielsystem-Beispiele

Gerade für diese Vorlage sind ergänzende Zielsystem-Projekte sinnvoll, die den
Weg vom Editor bis in eine MCU-Anwendung sichtbar machen.

Solche Projekte können zeigen:

- wie der generierte Contract eingebunden wird
- wie Nachrichten und Aktionen auf MCU-Seite verarbeitet werden
- wie Queue- oder Task-Strukturen mit der UI gekoppelt werden

Der Anspruch dieser Beispiele liegt dabei nicht auf möglichst komplexen
Anwendungen, sondern auf einer klaren und nachvollziehbaren Einbindung des
generierten Codes in ein reales Zielsystem, zum Beispiel auf ESP32-Basis.
