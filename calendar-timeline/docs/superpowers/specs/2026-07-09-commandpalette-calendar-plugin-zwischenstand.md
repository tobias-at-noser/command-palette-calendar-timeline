# Zwischenstand: PowerToys Command Palette Kalender-Timeline

> Superseded: Die frühere Annahme einer grafischen horizontalen Dock-Timeline ist überholt. Das Dock ist jetzt als kompakte Agenda-/Status-Ansicht vorgesehen; die grafische Timeline liegt in der WPF-Snapbar. Siehe `2026-07-10-shared-core-dock-snapbar-design.md`.

## Ziel

Dieser Zwischenstand dokumentiert die frühe Produktidee eines PowerToys Command Palette Plugins mit grafischer Timeline. Die aktuell gültige Richtung ist jedoch ein PowerToys Command Palette Dock für kompakte Agenda-/Status-Zeilen auf Basis von Kalenderterminen aus dem lokalen Outlook-Desktop-Profil; die grafische Timeline wird separat in einer WPF-Snapbar umgesetzt.

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
- In diesem Zwischenstand war eine kontinuierliche UI-Animation zwischen Daten-Snapshots vorgesehen; in der aktuellen Architektur betrifft das nur noch die WPF-Snapbar, nicht das Dock.
- Zeitangaben sollten in der frühen Timeline-Idee primär über Achse/Labels sichtbar sein; in der aktuellen Dock-Richtung werden Zeiten kompakt über Titel/Untertitel vermittelt.

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

Entschieden wurde in diesem Zwischenstand Ansatz 1: Command Palette UI plus separater Outlook-COM-Worker.

Der historische Entwurf besteht aus zwei Teilen:

1. Plugin/UI-Prozess
   - sollte eine Dock-Ansicht mit grafischer Timeline, Status und Teams-Aktion rendern
   - bleibt unabhängig vom Outlook-Zugriff responsiv
   - sollte die Timeline kontinuierlich zwischen Snapshots animieren

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

Die UI rendert jeweils den letzten gültigen Snapshot. In der ursprünglichen Dock-Timeline-Idee berechnete sie Positionen relativ zur aktuellen Uhrzeit selbst weiter; in der aktuellen Architektur passiert grafisches Layout in der WPF-Snapbar, während das Dock kompakte Agenda-/Status-Zeilen rendert.

## Dock UI

Dieser Abschnitt beschreibt den damals favorisierten visuellen Entwurf und ist nicht mehr als aktuelle Implementierungsanforderung zu lesen.

Die frühe Dock-Idee zeigte eine horizontale Timeline mit:

- fester vertikaler `Jetzt`-Linie
- kontinuierlich rechts-nach-links bewegter Zeitachse und Terminblöcken
- weich ausblendenden Rändern
- Terminblöcken auf einer Zeitachse
- gestapelten überlappenden Terminen
- Titel und Ort im Block
- Zeitvermittlung über Achse bzw. Labels
- laufenden Terminen als `Now`-Block mit Restdauer
- freier Fläche mit Countdown bis zum nächsten Termin, wenn kein Termin läuft
- Teams-Aktion bei erkannten Teams-Links

Aktuelle Implementierungsrichtung:

- Das Command Palette Dock zeigt 1 bis 3 kompakte Agenda-/Status-Zeilen.
- Das Dock nutzt nur API-konforme Felder wie Icon, Titel, Untertitel und Command.
- Die grafische horizontale Timeline mit Achse, Lanes, `Jetzt`-Linie und Bewegungslogik gehört zur WPF-Snapbar.
- Teams-Links werden im Dock über Command-/List-Item-Aktionen ausgelöst, nicht über frei platzierte grafische Buttons.

Der visuelle Entwurf einer horizontalen Timeline-Bar mit fixer `Jetzt`-Linie, bewegten Blöcken, Fade-Rändern und gestapelten Überschneidungen bleibt als historischer Designstand relevant, ist aber durch die Dock-Agenda-/WPF-Snapbar-Architektur ersetzt.

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
- Dock-Agenda-/WPF-Snapbar-Migration in allen älteren Spezifikationen klar nachziehen
- danach vollständige Spec und Implementierungsplan auf Basis der Shared-Core-/Snapbar-Architektur erstellen
