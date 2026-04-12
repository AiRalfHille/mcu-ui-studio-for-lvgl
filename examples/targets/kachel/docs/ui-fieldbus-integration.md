# UI, Domäne und Fieldbus

## Warum diese Notiz

Dieses Projekt ist nur dann mehr als eine Demo, wenn ein MCU-Entwickler den
generierten Code mit wenig Aufwand in eine echte Runtime einhaengen kann.

Das setzt voraus:

- Der Editor erzeugt belastbare UI-Dateien.
- Das MCU-Projekt besitzt eine klare Erwartung an deren Verwendung.
- Zwischen UI, Control und Fieldbus gibt es eine gemeinsame fachliche Referenz.

Wenn diese Referenz fehlt, entstehen schnell schwache Kopplungen:

- UI arbeitet mit Objektnamen wie `m1_btn1`
- die Steuerung arbeitet mit `m1`, `left`, `stop`
- der Fieldbus arbeitet mit Telegrammen, Bools, Stati und Fehlerbits

Dann funktioniert zwar einiges technisch, aber die Integritaet der Software
ist schwach.

## Kernidee

UI und Fieldbus sollten nicht direkt miteinander gekoppelt werden.

Beide Welten werden stattdessen an ein gemeinsames Domänenmodell gehaengt:

- Geraet
- moegliche Actions
- moegliche States
- moegliche Rueckmeldungen

Das Display zeigt dann nicht "irgendeinen Text", sondern eine Sicht auf einen
fachlichen Zustand.

## Gemeinsames Modell

Die kleinste sinnvolle gemeinsame Referenz ist:

- `device_id`
- `action`
- `state`

Beispiel:

- `device_id = m1`
- `action = left`
- `state = running_left`

Wichtig:

- `action` ist ein Wunsch oder Auftrag
- `state` ist die bestaetigte fachliche Wahrheit

Damit bleibt die Trennung sauber:

- UI fordert etwas an
- `controller` prueft und entscheidet
- `fieldbus` fuehrt aus und meldet Ergebnis
- `display` zeigt den aktuellen Zustand an

## Was im Editor definiert werden sollte

Der Editor sollte alles liefern, was zur UI-Struktur gehoert, aber nicht die
fachliche Wahrheit des MCU-Projekts besitzen.

In den Editor gehoeren:

- Screens und Seiten
- Widgets und Layout
- sichtbare Labels, Buttons, Anzeigen
- Ereignisquellen pro Widget
- Update-Ziele pro Widget
- stabile technische UI-IDs

Optional, aber sehr wertvoll:

- ein fachlicher Alias pro Widget
- ein Tag oder Binding-Schluessel, z. B. `device=m1`, `action=left`
- eine Kennzeichnung, welches Widget einen Status anzeigt

Nicht in den Editor gehoeren:

- Fieldbus-Adressen
- Protokolldetails
- Sicherheitsregeln
- Freigabelogik
- Maschinenzustandslogik

Der Editor sollte also moeglichst gute UI-Metadaten liefern, aber keine
anlagenspezifische Wahrheit erzwingen.

## Was im MCU-Projekt definiert werden sollte

Das MCU-Projekt besitzt die fachliche Hoheit.

Dort gehoeren hinein:

- die gueltigen `device_id`-Werte
- die gueltigen `action`-Werte
- die moeglichen `state`-Werte
- die Zuordnung zu echtem Fieldbus oder echter Hardware
- Regeln, Sperren, Freigaben und Fehlersituationen
- die Zuordnung, welche Rueckmeldung auf welches Anzeigeelement geht

Kurz:

- Der Editor kennt die UI.
- Das MCU-Projekt kennt die Anlage.

## Empfohlenes Integrationsmodell

### 1. UI-Ereignisse werden in fachliche Requests uebersetzt

Die generierte Event-Datei darf aus einem Widget-Event eine Runtime-Nachricht
vorbereiten, aber sie sollte nicht die eigentliche Fachlogik besitzen.

Ein guter Weg ist:

- der Editor liefert technische Widget-Information
- das MCU-Projekt mappt diese auf fachliche Requests

Beispiel:

- UI-Widget: `m1_btn_left`
- fachlicher Request: `device_id = m1`, `action = left`

### 2. `controller` bleibt die Entscheidungsinstanz

`controller` entscheidet:

- gibt es dieses Geraet?
- ist diese Action gueltig?
- darf sie aktuell ausgefuehrt werden?

Erst danach wird an `fieldbus` weitergegeben.

### 3. Fieldbus meldet fachliche States zurueck

Der Rueckweg sollte nicht wieder "nur Text" sein, sondern ein fachlicher
Status.

Beispiel:

- `device_id = m1`
- `state = running_left`

### 4. Display rendert aus State auf konkrete UI-Ziele

Das Display-Modul sollte wissen:

- welches UI-Element den Status von `m1` anzeigt
- welche Darstellung zu `running_left` gehoert
- welcher Text, welche Farbe oder welches Symbol gesetzt wird

Damit ist die Regel:

- `controller` und `fieldbus` sprechen fachlich
- `display` spricht in UI-Targets

## Referenzielle Integritaet

Die zentrale Frage ist:

"Wie bleibt klar, welches UI-Element zu welchem Geraet und zu welcher
Rueckmeldung gehoert?"

Dafuer braucht es mindestens eine verbindliche Zuordnungstabelle.

Beispielhaft:

```c
typedef struct
{
    const char *device_id;
    ui_start_update_target_t status_text_target;
} display_binding_t;
```

Oder fachlicher:

```c
typedef struct
{
    device_id_t device;
    ui_start_update_target_t status_target;
} display_binding_t;
```

Dann gibt es fuer jedes Geraet genau eine bekannte Beziehung:

- `m1` zeigt seinen Status auf `UI_START_UPDATE_TARGET_MS_LBLSTATUSLINKS_TEXT`

Noch besser ist eine zweite Zuordnung fuer UI-Actions:

```c
typedef struct
{
    const char *widget_id;
    const char *device_id;
    action_t action;
} ui_action_binding_t;
```

Dann wird der Weg sauber:

- Widget `m1_btn_left`
- wird zu `device_id = m1`, `action = left`
- `controller` prueft
- Fieldbus meldet `state`
- Display-Binding kennt das Ziel fuer `m1`

## Mehrere Seiten auf dem Display

Mehrere Seiten sind auf Embedded-UIs absolut normal.

Typische Faelle:

- Startseite + Diagnose
- Bedienseite + Service
- Uebersicht + Detailseite
- Alarmseite + Einstellungen

Das ist also kein Sonderfall, sondern sollte im Modell vorgesehen sein.

Wichtig ist dabei:

- Ein Geraet kann auf mehreren Seiten vorkommen.
- Unterschiedliche Seiten koennen denselben Zustand verschieden darstellen.
- Die fachliche Wahrheit bleibt trotzdem einmalig.

Deshalb sollte die Seitennavigation nicht die fachliche Identitaet bestimmen.

Falsch waere:

- "auf Seite 2 ist das ein anderes M1"

Richtig ist:

- `m1` bleibt fachlich dasselbe Geraet
- Seite A zeigt Bedienelemente
- Seite B zeigt Diagnose

Das spricht fuer zwei Ebenen der Zuordnung:

- fachlich: `device_id`, `action`, `state`
- UI-seitig: welches Screen-Element repraesentiert diesen Zustand auf dieser Seite

## Was MCU-Entwickler in der Praxis erwarten

Ein MCU-Entwickler erwartet in der Regel:

- stabile generierte Dateien
- keine Pflicht zu manuellen Generator-Reparaturen
- eine klare Stelle fuer Projektlogik
- keine versteckte Kopplung von UI-Code und Feldbus-Code
- moeglichst tabellenartige, nachvollziehbare Zuordnung

Was meist gut ankommt:

- generierter Code fuer Layout, Events und Update-Ziele
- handgeschriebener Code fuer Domänenmodell, Mapping und Logik
- klare API-Grenzen

Was meist schlecht ankommt:

- Widgetnamen muessen direkt als Fachlogik herhalten
- die Runtime muss Generatorfehler manuell ausbuegeln
- dieselbe Zuordnung steht verstreut in Event-Code, Control und Display

## Konkrete Empfehlung fuer dieses Projekt

### Editor-Seite

Der Editor sollte liefern:

- stabile technische IDs
- Event-Bindings
- Update-Targets
- optional fachliche Metadaten pro Widget

Wenn moeglich, sollte der Editor spaeter pro interaktivem Widget exportieren:

- `widget_id`
- `device_ref`
- `action_ref`

Und pro Anzeigeelement:

- `widget_id`
- `device_ref`
- `feedback_ref` oder `state_ref`

### MCU-Seite

Das MCU-Projekt sollte besitzen:

- eine Liste gueltiger Geraete
- eine Liste gueltiger Actions
- eine Liste gueltiger States
- eine Binding-Tabelle fuer UI-Request-Mapping
- eine Binding-Tabelle fuer Display-Feedback-Mapping

Das ist vermutlich der Punkt, an dem die aktuelle Runtime noch zu schwach ist.

## Minimaler Zielzustand

Ein belastbarer erster Zielzustand waere:

1. UI-Button wird zu `device_id + action`
2. `controller` validiert
3. `fieldbus` meldet `device_id + state`
4. `display` mappt `device_id + state` auf ein UI-Target

Dann ist die Referenzkette geschlossen.

## Fazit

Der eigentliche Mehrwert eines solchen Editors ist nicht nur "erzeugt C-Dateien".

Der Mehrwert entsteht erst dann wirklich, wenn klar ist:

- was der Editor sicher liefert
- was das MCU-Projekt bewusst ergaenzt
- und wo die gemeinsame fachliche Referenz lebt

Wenn diese Trennung sauber gelingt, ist das fuer MCU-Entwickler ein echter
Produktivitaetsgewinn.

Wenn nicht, bleibt schnell nur Marketing uebrig.
