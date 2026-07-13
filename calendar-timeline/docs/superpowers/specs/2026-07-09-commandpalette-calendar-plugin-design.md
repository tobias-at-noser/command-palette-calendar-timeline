# Design: PowerToys Command Palette Kalender-Timeline

> Superseded note: The original assumption that the PowerToys Command Palette Dock can render a custom horizontal timeline is no longer valid for implementation. The Dock implementation is now a compact agenda/status band. The graphical timeline is implemented separately as a WPF snapbar. See `2026-07-10-shared-core-dock-snapbar-design.md`.

## Ziel

Ein PowerToys Command Palette Plugin zeigt lokale Outlook-Kalendertermine als kompakte Dock-Agenda-/Status-Ansicht. Nutzer sollen laufende und bald anstehende Termine schnell sehen, ohne Outlook öffnen zu müssen. Eine separate WPF-Snapbar übernimmt die grafische Timeline-Darstellung.

## Kontext

Das Repository enthält aktuell noch kein Plugin-Projekt. Der MVP wird daher als neues PowerToys Command Palette Plugin aufgebaut.

Die Zielumgebung ist ein Firmenkontext, in dem Drittanbieterzugriff auf Outlook/Microsoft Graph wahrscheinlich blockiert ist. Deshalb nutzt der MVP lokale Outlook-Desktop-Integration per COM/MAPI statt Microsoft Graph.

## MVP-Scope

Enthalten:

- PowerToys Command Palette Plugin
- lokale Outlook-Desktop-Integration per COM/MAPI
- Anzeige aller eigenen Kalender des aktuellen Outlook-Profils
- Zeitraum von `now - 30 minutes` bis `now + 4 hours`
- minütlicher Kalenderdaten-Refresh
- kompakte Dock-Agenda und separate WPF-Snapbar
- private/vertrauliche Termine anonymisiert
- Teams-Link-Button, wenn ein Teams-Link erkannt wird
- dezente Fehleranzeige mit automatischem Retry
- lokale Dev-/Sideload-Installation

Nicht enthalten:

- Microsoft Graph
- shared calendars
- Terminbearbeitung
- Terminerstellung
- Öffnen eines Termins in Outlook
- Settings-UI
- frei konfigurierbare Kalenderauswahl
- Store-/Release-Packaging als MVP-Ziel

## Architektur

Das Plugin besteht aus zwei getrennten Prozessen.

### Plugin/UI-Prozess

Der Plugin-Prozess ist für die Command Palette Integration und die Dock-Ansicht zuständig.

Aufgaben:

- kompakte Dock-Agenda rendern
- WPF-Snapbar als separate grafische Timeline unterstützen
- letzten gültigen Kalender-Snapshot anzeigen
- Fehlerstatus dezent darstellen
- Teams-Buttons bereitstellen
- Worker starten, überwachen und abfragen

Der UI-Prozess greift nicht direkt auf Outlook COM/MAPI zu.

### Outlook-Worker-Prozess

Der Worker-Prozess isoliert alle Outlook-Zugriffe.

Aufgaben:

- Outlook Desktop per COM/MAPI initialisieren
- alle eigenen Kalender des aktuellen Outlook-Profils lesen
- Termine für das relevante Zeitfenster sammeln
- private/vertrauliche Termine anonymisieren
- Teams-Links erkennen
- normalisierte Snapshots an den UI-Prozess liefern

Diese Trennung verhindert, dass COM-Hänger, Outlook-Probleme oder Worker-Crashes die Command Palette UI blockieren oder destabilisieren.

### IPC

Die lokale Kommunikation bleibt bewusst einfach. Zwei Varianten bleiben technisch möglich:

1. JSON über stdin/stdout
2. Named Pipe mit JSON-Nachrichten

Für die Implementierungsplanung wird eine Variante ausgewählt. Beide Varianten transportieren dasselbe Snapshot-Modell.

## Datenmodell

Der Worker liefert pro Refresh einen vollständigen Snapshot.

Snapshot-Felder:

- `generatedAt`
- `windowStart`
- `windowEnd`
- `appointments`
- optionaler Status-/Fehlerhinweis

Appointment-Felder:

- `id`
- `title`
- `location`
- `start`
- `end`
- `isPrivate`
- `teamsUrl`

Private oder vertrauliche Termine werden bereits im Worker anonymisiert:

- `title`: `Privater Termin`
- `location`: leer
- keine weiteren vertraulichen Details

## Zeitfenster und Refresh

Die UI zeigt Termine im Bereich:

- Start: `now - 30 minutes`
- Ende: `now + 4 hours`

Damit bleiben laufende und gerade begonnene Termine sichtbar.

Der Worker aktualisiert die Outlook-Daten einmal pro Minute. Die Dock-Agenda nutzt den letzten Snapshot mit lokaler Zeitbasis für Reihenfolge- und Statusberechnung. Eine kontinuierliche Timeline-Animation ist nur Aufgabe der separaten WPF-Snapbar und benötigt dafür nicht in jedem Frame neue Outlook-Daten.

## Dock UI

Die Dock-Ansicht ist keine frei gerenderte horizontale Timeline. Die Command Palette Dock API wird im MVP als kompakte Agenda-/Status-Band genutzt.

Kernelemente:

- maximal 1 bis 3 sichtbare Agenda-Zeilen
- Icon, Titel, Untertitel und Command pro Zeile
- laufender Termin wird priorisiert
- nächster Termin wird als zweite Priorität angezeigt
- Status-/Fehlerzeile wird dezent angezeigt
- Teams-Link wird als Command geöffnet, wenn vorhanden

Die grafische horizontale Timeline wird in der separaten WPF-Snapbar umgesetzt.

## Teams-Link-Erkennung

Der Worker versucht, einen Teams-Link aus Outlook-Termindaten zu erkennen. Der MVP benötigt nur das Öffnen vorhandener Links.

Wenn ein Teams-Link vorhanden ist:

- Dock zeigt pro Agenda-Zeile einen Teams-bezogenen Command, wenn die API-Affordanz dies zulässt
- WPF-Snapbar zeigt eine Teams-Aktion am Terminblock oder in den Hover-Details
- Klick öffnet den Link über das Betriebssystem

Wenn kein Teams-Link vorhanden ist:

- es wird keine Teams-Aktion angezeigt

## Fehlerverhalten

Fehler sollen die Übersicht nicht aggressiv stören.

Verhalten:

- UI zeigt dezente Statusmeldung im Dock
- keine modalen Fehlerdialoge
- letzter gültiger Snapshot bleibt sichtbar, wenn vorhanden
- Worker wird automatisch neu gestartet oder erneut abgefragt
- bei dauerhaftem Fehler erscheint ein kompakter Hinweis wie `Outlook-Kalender nicht verfügbar`

Outlook-/COM-Probleme bleiben auf den Worker-Prozess begrenzt.

## Tests

Die Implementierung soll ohne echtes Outlook testbar sein.

Testansatz:

- Worker-Parsing und Normalisierung mit Fake-/Fixture-Terminen testen
- Datenschutzlogik für private/vertrauliche Termine testen
- Teams-Link-Erkennung testen
- Dock-Agenda-Projektion mit statischen Snapshots prüfen
- WPF-Snapbar-Timeline-Layout mit statischen Snapshots prüfen

Wichtige UI-Zustände:

- keine Termine
- laufender Termin
- nächster Termin
- mehrere überlappende Termine in der WPF-Snapbar
- privater Termin
- Termin mit Teams-Link
- Worker-/Outlook-Fehlerzustand

## Packaging

Der MVP wird lokal/dev per Sideloading installiert.

Spätere Optionen:

- GitHub Release
- Store-fähiges Packaging

Diese späteren Verpackungsziele beeinflussen die MVP-Architektur nicht, sollen aber nicht verbaut werden.

## Verworfene Alternativen

### Outlook COM direkt im Plugin-Prozess

Verworfen, weil Outlook COM/MAPI die UI blockieren oder den Command Palette Prozess destabilisieren kann.

### Microsoft Graph als primäre Quelle

Zurückgestellt, weil Graph in der Zielumgebung wahrscheinlich durch App Registration, Admin Consent, Conditional Access oder Drittanbieterzugriffsregeln blockiert wird.

Typische benötigte Berechtigungen wären `Calendars.Read`, `User.Read`, `offline_access` und optional `Calendars.Read.Shared`.

## Offene Implementierungsentscheidung

Die einzige bewusst offene technische Entscheidung ist die konkrete IPC-Variante zwischen UI-Prozess und Worker:

- JSON über stdin/stdout
- Named Pipe mit JSON

Diese Entscheidung wird im Implementierungsplan getroffen.