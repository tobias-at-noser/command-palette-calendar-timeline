# Design: Timeline-Politur und Zeitinformationen

## Ziel

Die WPF-Snapbar stellt die Timeline an den Außenkanten konsistent ausgeblendet dar, nutzt breite Termin-Bubbles besser aus und zeigt verständliche Zeitinformationen an der Jetzt-Linie. Die Fensterhöhe schrumpft wieder, wenn automatisch benötigte Lanes entfallen, ohne eine manuell gewählte größere Höhe zu überschreiben.

## Umfang

Enthalten sind:

- eine horizontale Verlaufsmaske für Termin-Bubbles entsprechend der bestehenden Timeline-Schiene
- einzeilige, linksbündige Bubble-Beschriftungen im Format `HH:mm · Titel`
- ein runder Hover- und Fokuszustand ohne sichtbare eckige Button-Chrome
- hybrides Verkleinern der Fensterhöhe, wenn automatisch hinzugefügte Lanes verschwinden
- eine verbleibende Dauer bis zum nächsten Termin
- aktuelle Uhrzeit und ein Datums-Tooltip an der Jetzt-Linie
- automatisierte Tests für Layout- und Zeitanzeigelogik

Nicht enthalten sind Änderungen an der Outlook-Datenabfrage, dem Zeitfenster der Timeline, der Farblogik oder manueller Größenänderung durch den Nutzer.

## Bubble-Darstellung

`BlocksCanvas` verwendet dieselbe horizontale Opazitätsverteilung wie `TimelineRail`: Die Darstellung wird an beiden Außenkanten transparent und ist im zentralen Bereich vollständig sichtbar. Die Jetzt-Linie und ihre Beschriftungen liegen darüber und werden nicht maskiert.

Jede Bubble erhält eine eigene Button-Vorlage. Die Vorlage ist transparent, verzichtet auf die Standard-Button-Chrome und rendert Hover, Fokus und gedrückten Zustand innerhalb einer abgerundeten Fläche mit demselben Radius wie die Bubble. Dadurch bleiben die Kanten auch bei Interaktion rund, und es entstehen keine eckigen Artefakte außerhalb der Bubble.

Die Bubble enthält eine einzelne, linksbündige Textzeile:

```text
01:00 · Test
```

Die Startzeit und der Trenner bleiben sichtbar. Der Titel erhält den verbleibenden Platz und wird am rechten Rand mit Ellipse gekürzt. Der bestehende Tooltip behält vollständige Termin- und Metadaten bei. Die bestehende Mindestbreite von 52 DIP bleibt bestehen.

## Jetzt-Linie und Countdown

Die Jetzt-Linie zeigt dauerhaft die lokale Uhrzeit im Format `HH:mm` an.

Der Hover-Bereich der Linie zeigt einen Tooltip mit dem lokalen Datum im deutschen Langformat, beispielsweise `Montag, 13.07.2026`.

Wenn kein sichtbarer Termin läuft und ein späterer sichtbarer Termin vorhanden ist, erscheint an der Jetzt-Linie zusätzlich die verbleibende Dauer bis zu dessen Beginn:

- Berechnung: `nextStart - now`
- Rundung: mathematisch auf das nächste Vielfache von fünf Minuten
- Ausgabe: `HH:mm`, beispielsweise `01:25`

Während eines laufenden sichtbaren Termins oder ohne späteren sichtbaren Termin wird kein Countdown angezeigt. Eine Anzeige von `00:00` unmittelbar vor dem Start ist zulässig, sofern die gerundete Differenz null ergibt.

Der bestehende Minuten-Timer aktualisiert Uhrzeit, Tooltip-Datum und Countdown zusammen mit dem bisherigen Refresh. Es wird keine zusätzliche ViewModel-Oberfläche eingeführt; die Anzeigen verbleiben in der vorhandenen Fenster-Renderlogik.

## Fensterhöhe

Die automatische Lanes-Höhe und die vom Nutzer gewählte Höhe werden getrennt behandelt:

- Benötigen zusätzliche Lanes mehr Raum, wächst das Fenster wie bisher.
- Verschwinden diese Lanes, wird nur die zuvor automatisch hinzugefügte Höhe entfernt.
- Eine Höhe, die der Nutzer selbst über dieses automatische Minimum hinaus eingestellt hat, bleibt unverändert.

Die Layout-Logik verwaltet deshalb eine separate automatische Mindesthöhe beziehungsweise einen äquivalenten Auto-Höhenanteil. Sie setzt die resultierende Höhe nie unter die zuletzt vom Nutzer vorgegebene Mindesthöhe und nie unter die für die aktuell sichtbaren Lanes erforderliche Höhe.

## Fehlerverhalten

- Fehlen sichtbare Termine, bleiben Bubble-Maske und Jetzt-Linie funktionsfähig; der Countdown ist ausgeblendet.
- Ein ungültiger oder nicht berechenbarer nächster Termin führt nicht zu einer Countdown-Anzeige.
- Lange Titel können weder Zeit noch Trenner verdrängen und brechen die Bubble-Geometrie nicht.
- Die Zeitinformationen verwenden lokale Zeit und bleiben bei jeder Timer-Aktualisierung konsistent.

## Tests

Die Implementierung ergänzt oder ersetzt Tests für:

- Rücknahme automatisch hinzugefügter Lane-Höhe bei sinkender Lane-Anzahl
- Erhalt einer größeren manuellen Fensterhöhe beim gleichen Vorgang
- einzeiliges Bubble-Markup mit `HH:mm · Titel`, Ellipse und vollständigem Tooltip
- Verlaufsmaske auf den Bubbles sowie nicht maskierte Jetzt-Linie
- eigenen, abgerundeten Button-Hover ohne Standard-Chrome
- Formatierung der aktuellen Uhrzeit und des deutschen Langdatums
- Countdown-Sichtbarkeit bei keinem, laufendem und künftigem Termin
- Fünf-Minuten-Rundung und `HH:mm`-Formatierung der Restdauer

Bestehende Tests für Farben, Lane-Zuordnung, Zeitfenster, Teams-Link und Snapshot-Refresh bleiben unverändert gültig.

## Erfolgskriterien

- Schiene und Termin-Bubbles blenden an denselben horizontalen Außenkanten aus; die Jetzt-Linie bleibt klar sichtbar.
- Ein breiter Bubble-Text erscheint linksbündig als `HH:mm · Titel` und nutzt die verfügbare Breite.
- Hover und Fokus bleiben innerhalb der abgerundeten Bubble.
- Nach dem Wegfall zusätzlicher Lanes wird nur automatisch ergänzte Höhe entfernt.
- Die Jetzt-Linie zeigt lokale Uhrzeit, beim Hover `Montag, 13.07.2026`, und bei Leerlauf eine auf fünf Minuten gerundete Restzeit zum nächsten Termin.
