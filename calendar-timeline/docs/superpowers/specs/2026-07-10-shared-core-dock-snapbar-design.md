# Design: Calendar Timeline Shared Core, Dock Agenda und WPF Snapbar

## Ziel

Die Kalender-Timeline wird von einer reinen PowerToys-Command-Palette-Dock-Idee zu einer gemeinsamen Calendar-Timeline-Plattform umgebaut.

Das System hat künftig einen gemeinsamen Core und zwei Visualisierungen:

1. eine kompakte PowerToys Command Palette Dock-Band als Agenda-/Status-Ansicht
2. eine grafische WPF-Snapbar oben am Bildschirmrand als echte Timeline

Die Outlook-Datenquelle läuft in einem langlebigen lokalen Tray-/Host-Prozess. Dock-Plugin und WPF-Snapbar sprechen per Named Pipe mit diesem Host.

## Ausgangslage

Die bisherige Spezifikation beschreibt eine horizontale grafische Timeline im Command Palette Dock. Die verifizierten PowerToys-Command-Palette-Dock-APIs und Beispiele zeigen dafür keine freie Zeichen-/Rendering-Fläche. Dock-Bands bestehen praktisch aus Command-/List-Items mit Icon, Titel, Untertitel und Aktion.

Die grafische Timeline bleibt fachlich gewünscht, wird aber nicht mehr im Dock umgesetzt. Das Dock wird bewusst zu einer API-konformen Kurzansicht. Die grafische Timeline wird in eine separate WPF-Oberfläche verschoben.

## Produktentscheidung

Empfohlene Zielarchitektur:

- `CalendarTimeline.Core` bleibt UI-frei und enthält Domain-, Layout-, Privacy-, Teams- und ViewModel-/Formatierungslogik.
- Ein neuer langlebiger Host-/Tray-Prozess ist die zentrale lokale Datenquelle.
- Die Kommunikation zwischen Host und Clients erfolgt über Named Pipes.
- Das Command Palette Plugin ist ein dünner Client und zeigt eine kompakte Agenda-Band.
- Die WPF-App ist die eigentliche grafische Timeline als Always-on-top-Snapbar am oberen Bildschirmrand.
- Autostart ist optional und standardmäßig deaktiviert.

## Scope

Enthalten:

- gemeinsame Core-Modelle und UI-unabhängige Projektionsmodelle
- Named-Pipe-IPC für lokale Kommunikation pro Benutzer
- langlebiger Host-/Tray-Prozess als Datenhub
- optionaler Windows-Login-Autostart, standardmäßig aus
- Command Palette Dock-Band als kompakte Agenda-/Status-Ansicht
- WPF-Snapbar oben am Bildschirmrand
- Hover-Details in der WPF-Snapbar
- Klickaktionen für Teams-Link und später Outlook-Termin
- Fake-/Debug-Datenpfad ohne echtes Outlook
- Tests für Core, Formatter, IPC-Verträge und Host-Fake-Modus

Nicht enthalten im ersten Umbau:

- frei gerenderte Timeline im Command Palette Dock
- Windows Widgets
- Taskbar-Deskband oder Taskbar-Embedding
- Microsoft Graph
- Store-Release
- Terminbearbeitung oder Terminerstellung
- vollständige Settings-UI über alle Optionen
- komplexe Multi-Monitor-Konfiguration über einfache Top-Screen-Positionierung hinaus

## Architektur

### CalendarTimeline.Core

Der Core bleibt unabhängig von WPF, Command Palette, Windows-Tray und IPC.

Verantwortung:

- `Appointment`, `CalendarSnapshot`, `TimelineBlock`, `CalendarWindow`
- Datenschutzlogik für private/vertrauliche Termine
- Teams-Link-Erkennung
- Overlap-/Lane-Layout
- UI-unabhängige Projektionen für Dock und Timeline
- Textformatierung für deutschsprachige Kurztexte

Der Core darf keine Abhängigkeit auf WPF, PowerToys SDK, Named Pipes, Outlook COM oder Tray APIs haben.

### CalendarTimeline.Ipc

Ein neues gemeinsames IPC-Projekt enthält Named-Pipe-Verträge und Client-/Server-Basisklassen.

Verantwortung:

- Request-/Response-Records
- JSON-Serialisierung über `System.Text.Json`
- Named-Pipe-Name pro Benutzer
- `GetSnapshot`
- `RefreshSnapshot`
- später `Subscribe` oder Polling-Erweiterung
- Fehlerantworten ohne sensitive Outlook-Details

Der IPC-Vertrag transportiert nur Core-Datenmodelle und einfache Aktionsbefehle.

### CalendarTimeline.Host

Der Host ersetzt den bisherigen kurzlebigen Worker als zentrale Datenquelle. Technisch kann vorhandene Worker-Logik übernommen werden, fachlich ist der Host aber langlebig.

Verantwortung:

- Tray-Icon und minimales Tray-Menü
- Outlook-Daten lesen oder Fake-Daten liefern
- Snapshot-Cache halten
- regelmäßiger Refresh
- Named-Pipe-Server bereitstellen
- WPF-Snapbar starten/anzeigen/verbergen
- optionalen Autostart konfigurieren
- Fehlerstatus in Snapshots abbilden

Tray-Menü im MVP:

- `Timeline anzeigen`
- `Timeline verbergen`
- `Jetzt aktualisieren`
- `Autostart aktivieren/deaktivieren`
- `Beenden`

Autostart ist optional und standardmäßig deaktiviert.

### CalendarTimeline.CommandPalette

Das Command Palette Plugin wird ein dünner IPC-Client.

Verantwortung:

- Host über Named Pipe abfragen
- falls Host nicht läuft: kompakte Statusmeldung anzeigen
- 1 bis 3 Agenda-Zeilen im Dock bereitstellen
- Teams-Link öffnen, wenn vorhanden
- keine direkte Outlook-Integration
- keine grafische Timeline vortäuschen

Dock-Zeilen:

- laufender Termin: `Jetzt · Daily`, Untertitel `12 Min. verbleibend · Teams`
- nächster Termin: `Als Nächstes · Planning`, Untertitel `14:30–15:00`
- Statuszeile: `Aktualisiert 09:42` oder `Kalenderdaten nicht verfügbar`

Das Dock nutzt nur die API-konformen Felder Icon, Titel, Untertitel und Command.

### CalendarTimeline.Wpf

Die WPF-App ist die echte grafische Timeline.

Verantwortung:

- Always-on-top-Fenster
- Snapbar oben am Bildschirmrand
- horizontale Zeitachse
- feste Jetzt-Linie
- Terminblöcke relativ zur Zeitachse
- Overlap-Lanes
- Hover-Details
- Klick auf Teams-Link oder Outlook-Ziel
- dezente Fehler-/Offline-Zustände

MVP-Positionierung:

- primär oben am Hauptbildschirm
- randnah/snapbar-artig
- nicht frei schwebend als Standard
- spätere Erweiterung um gespeicherte Position möglich

## Datenfluss

1. Host startet manuell oder per optionalem Autostart.
2. Host lädt Kalenderdaten aus Outlook oder Fake-Quelle.
3. Host normalisiert und sanitisiert Daten über Core-Logik.
4. Host speichert letzten gültigen `CalendarSnapshot` im Speicher.
5. Host stellt Snapshot über Named Pipe bereit.
6. Command Palette Dock fragt Snapshot ab und rendert kompakte Agenda-Zeilen.
7. WPF-Snapbar fragt Snapshot ab und rendert grafische Timeline.
8. Bei Outlook-/Host-Fehlern bleibt der letzte gültige Snapshot sichtbar, ergänzt um Statushinweis.

## IPC-Vertrag

MVP-Requests:

- `GetSnapshot`
- `RefreshSnapshot`
- `Ping`

MVP-Responses:

- `SnapshotResponse`
- `StatusResponse`
- `ErrorResponse`

Fehlerregeln:

- Clients dürfen bei Pipe-Fehlern nicht crashen.
- Dock zeigt kompakte Statuszeile.
- WPF zeigt dezente Offline-/Fehlerfläche.
- Host darf keine vertraulichen Outlook-Rohdaten in Fehlertexten zurückgeben.

## UI-Verhalten

### Dock

Das Dock ist eine Kurzansicht, kein Timeline-Renderer.

Priorisierung:

1. laufender Termin
2. nächster Termin
3. Status/Refresh-Zeit oder Fehlerhinweis

Wenn mehrere Termine laufen, werden maximal zwei angezeigt, danach ein kompakter Hinweis wie `+1 weiterer Termin`.

### WPF-Snapbar

Die Snapbar ist die Timeline-Hauptansicht.

Visuelles Verhalten:

- oben am Bildschirmrand
- immer im Vordergrund
- horizontale Zeitachse von `now - 30 minutes` bis `now + 4 hours`
- feste Jetzt-Linie
- Terminblöcke in Lanes
- laufende Termine visuell hervorgehoben
- private Termine anonymisiert
- Hover zeigt Details
- Klick öffnet Teams-Link, falls vorhanden

## Einstellungen

MVP-Einstellungen:

- Autostart aktiv/inaktiv, default inaktiv
- Snapbar anzeigen/verbergen
- letzter gewünschter Sichtbarkeitsstatus

Persistenz kann einfach lokal pro Benutzer erfolgen. Eine vollständige Settings-UI ist nicht Teil dieses Schritts.

## Tests

Testschwerpunkte:

- Core-Projektionen für Dock-Zeilen
- Core-Projektionen für Timeline-Blöcke
- Datenschutzsanitizing
- Teams-Link-Erkennung
- Named-Pipe-Vertragsserialisierung
- Host-Fake-Snapshot-Modus
- CommandPalette-Verhalten bei Host nicht erreichbar
- WPF-ViewModel-Verhalten mit Fake-Snapshot

Die Implementierung muss ohne echtes Outlook testbar bleiben.

## Migrationsentscheidung

Die bestehende Command-Palette-Spezifikation wird fachlich ersetzt:

- alte Annahme: Dock rendert horizontale Timeline
- neue Entscheidung: Dock zeigt kompakte Agenda-Band
- neue Timeline-Position: WPF-Snapbar oben am Bildschirmrand

Bestehende Core- und Testlogik soll erhalten und erweitert werden, nicht verworfen.
