# Zwischenstand: PowerToys Command Palette Kalender-Timeline

## Ziel

Ein PowerToys Command Palette Plugin zeigt Kalendertermine aus dem lokalen Outlook-Desktop-Profil als timeline-artige Dock-Ansicht für die nächsten Stunden.

## Kontext und Randbedingungen

- Das Repository ist aktuell praktisch leer und enthält noch kein Plugin-Projekt.
- Ziel ist ein MVP für die lokale Nutzung und Entwicklung.
- Der Hauptanwendungsfall ist eine schnelle Übersicht über laufende und bald kommende Termine direkt im Command Palette Dock.
- Die Firmenumgebung blockiert voraussichtlich Drittanbieterzugriff auf Outlook/Graph; deshalb ist lokale Outlook-Integration wichtiger als Cloud-Integration.

## Getroffene Entscheidungen

### Kalenderquelle

- MVP nutzt lokales Outlook Desktop per COM/MAPI.
- Microsoft Graph wird im MVP nicht verwendet.
- Outlook Desktop muss installiert und ein Profil muss konfiguriert sein.
- Das Plugin darf Outlook bei Bedarf im Hintergrund initialisieren.
- Es werden alle eigenen Kalender aus dem aktuellen Outlook-Profil angezeigt.
- Shared Calendars sind nicht Teil des MVP.

### Zielplattform und Scope

- Zielplattform ist ausschließlich PowerToys Command Palette.
- Es wird keine separate wiederverwendbare Logikschicht über den internen Bedarf hinaus gebaut.
- Es gibt im MVP keine Terminbearbeitung und keine Terminerstellung.
- Es gibt im MVP keine Settings-UI.
- Defaults sind fest verdrahtet.
- Distribution zunächst lokal/dev per Sideloading.
- Spätere GitHub-Release- oder Store-fähige Verpackung bleibt möglich, ist aber nicht Teil des MVP.

### Zeitfenster und Aktualisierung

- Angezeigter Zeitraum: `now - 30 minutes` bis `now + 4 hours`.
- Laufende und gerade vergangene Termine bleiben dadurch sichtbar.
- Outlook-Datenrefresh: jede Minute.
- UI-Animation: kontinuierlich zwischen Daten-Snapshots.
- Zeitangaben sollen primär über Timeline-Labels/Achse sichtbar sein, nicht redundant auf jeder Karte.

### Laufende und freie Zeit

- Laufende Termine werden als `Now`-Block angezeigt.
- Der `Now`-Block zeigt die verbleibende Dauer.
- Wenn aktuell kein Termin läuft, zeigt die freie Fläche einen Countdown bis zum nächsten Termin.

### Private und vertrauliche Termine

- Private oder vertrauliche Termine werden als `Privater Termin` angezeigt.
- Der Ort wird bei privaten oder vertraulichen Terminen ausgeblendet.
- Weitere Details solcher Termine werden nicht angezeigt.

### Aktionen

- Das Öffnen eines Termins in Outlook ist nicht Teil des MVP.
- Wenn ein Teams-Link erkannt wird, zeigt der Termin einen kleinen Teams-Button.
- Der Teams-Button öffnet den Teams-Link.

## Architektur

Entschieden wurde Ansatz 1: Command Palette UI plus separater Outlook-COM-Worker.

Das Plugin besteht aus zwei Teilen:

1. Plugin/UI-Prozess
   - rendert Dock-Ansicht, Timeline, Status und Teams-Buttons
   - bleibt unabhängig vom Outlook-Zugriff responsiv
   - animiert die Timeline kontinuierlich zwischen Snapshots

2. Separater Outlook-Worker-Prozess
   - liest Termine per Outlook COM/MAPI
   - normalisiert Kalenderdaten
   - anonymisiert private/vertrauliche Termine vor Übergabe
   - schützt die UI vor COM-Hängern oder Outlook-Crashes

Die lokale Kommunikation soll simpel bleiben, z. B. JSON über stdin/stdout oder Named Pipe. Die konkrete IPC-Variante ist noch offen.

## Datenfluss

Der Worker liefert minütlich ein Snapshot-Modell mit Terminen im definierten Zeitfenster.

Pro Termin werden benötigt:

- Titel
- Ort
- Startzeit
- Endzeit
- Privat-/Confidential-Flag
- Teams-Link, falls erkennbar

Die UI rendert jeweils den letzten gültigen Snapshot und berechnet Positionen relativ zur aktuellen Uhrzeit selbst weiter.

## Dock UI

Die Dock-Ansicht zeigt eine horizontale Timeline:

- feste vertikale `Jetzt`-Linie
- Zeitachse und Terminblöcke bewegen sich kontinuierlich rechts-nach-links
- beide Ränder blenden weich in Transparenz aus
- Termine werden als Blöcke auf der Timeline dargestellt
- überlappende Termine werden in mehreren Zeilen gestapelt
- Terminblöcke zeigen Titel und Ort
- Zeit wird über die Achse bzw. Labels vermittelt
- laufende Termine erscheinen als `Now`-Block mit Restdauer
- wenn kein Termin läuft, zeigt freie Fläche einen Countdown bis zum nächsten Termin
- Termine mit Teams-Link zeigen einen kleinen Teams-Button zum Öffnen des Links

Der visuelle Entwurf wurde als passend bestätigt: horizontale Timeline-Bar mit fixer `Jetzt`-Linie, bewegten Blöcken, Fade-Rändern und gestapelten Überschneidungen.

## Fehlerverhalten

- Fehler werden dezent im Dock angezeigt.
- Retry erfolgt automatisch.
- Outlook-/COM-Probleme dürfen die Command Palette UI nicht blockieren oder crashen.
- Worker-Fehler sollen lokal begrenzt bleiben; die UI kann den letzten gültigen Snapshot weiter anzeigen, bis neue Daten verfügbar sind.

## Verworfene oder zurückgestellte Alternativen

### Alles im Plugin-Prozess

Wurde verworfen, weil Outlook COM/MAPI die UI blockieren oder den Command-Palette-Prozess destabilisieren kann.

### Graph-first mit COM-Fallback

Wurde für später zurückgestellt. Graph wäre zukunftsfähig, ist in der Firmenumgebung aber wahrscheinlich blockiert und erhöht den MVP-Aufwand durch Auth, App Registration, Permissions und Consent.

Graph würde typischerweise Berechtigungen wie `Calendars.Read`, `User.Read`, `offline_access` und optional `Calendars.Read.Shared` benötigen.

## Offene Punkte

- genaue IPC-Variante festlegen: stdin/stdout JSON vs. Named Pipe
- Fehler-/Retry-Strategie konkretisieren
- Testing-Strategie festlegen
- Packaging/Sideloading-Schritte definieren
- finalen Design-Check abschließen
- danach vollständige Spec und Implementierungsplan erstellen
