# Design: Timeline-Bubbles, Lane-Reihenfolge und Kalenderfarben

## Ziel

Die WPF-Snapbar stellt Termine auch bei kurzen Zeitspannen gut lesbar dar, ordnet Ueberschneidungen chronologisch von oben nach unten an und verwendet die in Outlook gepflegten Kalender- und Kategoriedaten fuer eine eindeutige Farbgebung.

## Umfang

Enthalten sind:

- alle eigenen Kalender im konfigurierten standardmaessigen persoenlichen Outlook-Datenspeicher statt nur des Standardkalenders
- Kalender- und Kategorie-Metadaten im Snapshot
- Outlook-Kategoriefarben mit UI-/Snapbar-seitig aus der Kalenderidentitaet aufgeloesten stabilen Farb-Fallbacks
- Kalenderfarbe als Rahmen-Akzent
- chronologische Lane-Zuordnung mit Lane 0 oben
- eine Timeline mittig hinter Lane 0
- eine Jetztlinie, die symmetrisch zur Timeline nur Lane 0 ueberspannt
- besser lesbare, zweizeilige Termin-Bubbles
- Tests fuer Datenprojektion, Datenschutz, Layout und Farblogik

Nicht enthalten sind eine Kalenderauswahl, eine Farbkonfiguration oder Aenderungen an der kompakten Command-Palette-Dock-Darstellung.

## Outlook-Datenquelle

Der Outlook-Worker ermittelt alle eigenen Kalenderordner rekursiv im konfigurierten standardmaessigen persoenlichen Outlook-Datenspeicher und fragt jeden Kalender fuer das bestehende Zeitfenster ab. Termine aus allen erfolgreich gelesenen Kalendern werden zu einem Snapshot zusammengefuehrt.

Jeder Termin erhaelt zusaetzlich:

- eine stabile Kalender-ID
- den Kalendernamen
- eine aus Outlook gelesene Kalenderfarbe, sofern verfuegbar; `CalendarColor` kann im Snapshot `null` sein
- die Kategorien in Outlook-Reihenfolge
- den Outlook-Farbwert jeder aufloesbaren Kategorie

Bei mehreren Kategorien bestimmt die erste in Outlook-Reihenfolge eingetragene Kategorie mit gueltiger Outlook-Farbe die Hauptfarbe. Weitere Kategorien bleiben fuer den Tooltip erhalten.

Kann ein identifizierter Kalender nicht gelesen werden, werden die uebrigen Kalender weiterhin verarbeitet. Der Snapshot enthaelt dann einen dezenten Statushinweis. Fehler bei der Ordnerinspektion allein erzeugen keinen Teilfehler-Status. Nur wenn kein verwertbarer Kalender-Snapshot erzeugt werden kann, verwendet die UI weiterhin `Kalenderdaten nicht verfuegbar`.

## Datenmodell und Datenschutz

Kalender- und Kategorie-Metadaten werden als strukturierte, UI-neutrale Daten durch Worker, Core-Modell, Snapshot-JSON und Snapbar-ViewModel transportiert. Die konkrete WPF-Farbe wird erst in der Darstellung bestimmt.

Private und vertrauliche Termine verlieren bei der bestehenden Bereinigung zusaetzlich Kategorienamen und Kategoriefarben. Dadurch koennen sensible Kategorien keine Termininhalte verraten. Kalender-ID, Kalendername und Kalenderfarbe bleiben erhalten, damit der Termin weiterhin seinem Kalender zugeordnet und konsistent dargestellt werden kann.

## Farbregeln

Die WPF-UI bzw. Snapbar loest Farben nach folgenden Regeln auf. Wenn Outlook keine nutzbare Kalenderfarbe liefert, bleibt `CalendarColor` im Snapshot `null`; der stabile Fallback wird erst dort aus der Kalenderidentitaet abgeleitet:

1. Hat der Termin mindestens eine Kategorie mit gueltiger Outlook-Farbe, wird deren Farbe als Bubble-Fuellung verwendet.
2. Die Kalenderfarbe erscheint als Rahmen-Akzent um die gesamte Bubble.
3. Ohne Kategorie wird die Kalenderfarbe zur Fuellfarbe. Der Rahmen verwendet dann eine kontrastierende Variante derselben Farbfamilie.
4. Nicht auslesbare oder ungueltige Farben werden aus einer stabilen Palette anhand der Kalenderidentitaet abgeleitet.
5. Die Textfarbe wird anhand des Fuellfarbenkontrasts als hell oder dunkel gewaehlt.

Laufende Termine behalten keine separate feste Gruen-Fuellung, da diese die Kalender- und Kategoriefarbe verdecken wuerde. Ihr Laufstatus wird stattdessen durch einen etwas staerkeren neutralen Schatten sichtbar; Fuell- und Rahmenfarbe bleiben unveraendert.

## Lane-Layout

Vor der Lane-Zuordnung werden Termine stabil nach Startzeit, Endzeit und ID sortiert. Der frueheste sichtbare Termin wird Lane 0 zugeordnet. Ueberschneidende Termine belegen die naechste freie Lane darunter. Sobald eine obere Lane wieder frei ist, wird sie fuer den naechsten passenden Termin wiederverwendet.

Damit entspricht die logische Lane-Nummer direkt der visuellen Reihenfolge:

- Lane 0 liegt oben.
- Lane 1, 2 und weitere Lanes wachsen nach unten.
- Gleichzeitige Starts bleiben durch Endzeit und ID deterministisch geordnet.

## Timeline und Jetztlinie

Die horizontale Timeline verlaeuft mittig hinter Lane 0 und liegt im Z-Index unter den Termin-Bubbles. Ihre vertikale Position haengt nicht von der Anzahl weiterer Lanes ab.

Die Jetztlinie:

- kreuzt die Timeline an der bisherigen zeitlichen Jetztposition
- reicht ausschliesslich ueber die Hoehe von Lane 0
- ist oberhalb und unterhalb der Timeline symmetrisch
- waechst nicht mit zusaetzlichen Lanes nach unten
- bleibt im Z-Index sichtbar, ohne den Bubble-Text unlesbar zu machen

## Bubble-Darstellung

Die Bubbles werden leicht hoeher und verwenden zwei unabhaengig kuerzbare Textzeilen:

- erste Zeile: Titel ueber die verfuegbare volle Breite
- zweite Zeile: mindestens die kompakte Startzeit im Format `09:30`

Der Titel wird erst am rechten Rand mit Ellipse gekuerzt. Dadurch beansprucht eine lange Zeitspanne nicht denselben horizontalen Platz wie der Titel. Der Tooltip enthaelt weiterhin die vollstaendige Zeitspanne sowie, sofern datenschutzrechtlich zulaessig, Ort, Kalender und alle Kategorien.

Abgerundete Ecken, ein dezenter Verlauf und Schatten geben den Bubbles Tiefe. Der Kalender-Rahmen-Akzent bleibt davon klar unterscheidbar. Die Mindestbreite wird von 36 auf 52 DIP angehoben, damit `09:30` mit kompaktem horizontalem Innenabstand vollstaendig sichtbar bleibt. Die bestehende Begrenzung haelt die Bubble weiterhin innerhalb des sichtbaren Timeline-Bereichs.

## Fehlerverhalten

- Fehler beim Lesen eines identifizierten Kalenderordners verhindern nicht die Anzeige anderer Kalender.
- Fehlende Kategorie- oder Kalenderfarben werden in UI/Snapbar durch stabile, kalenderidentitaetsbasierte Fallbacks ersetzt.
- Unbekannte Outlook-Farbwerte verursachen keinen Darstellungsfehler.
- Ein Fehler bei der UI-Projektion setzt weiterhin den dezenten Nicht-verfuegbar-Status statt die Snapbar zu beenden.

## Tests

Die Implementierung ergaenzt automatisierte Tests fuer:

- Einlesen und Zusammenfuehren mehrerer Kalender
- Teilfehler beim Lesen einzelner identifizierter Kalender
- Uebernahme der ersten gueltigen Outlook-Kategoriefarbe in Outlook-Reihenfolge
- stabile Farb-Fallbacks und lesbaren Textkontrast
- Entfernen von Kategorie-Metadaten bei privaten und vertraulichen Terminen
- JSON-Roundtrip der neuen Metadaten
- Sortierung nach Startzeit, Endzeit und ID
- Lane 0 oben und weitere Lanes darunter
- feste Timeline-Position mittig hinter Lane 0
- auf Lane 0 begrenzte, symmetrische Jetztlinie
- kompakte Bubble-Texte und vollstaendige Tooltip-Projektion

Die bestehenden Tests fuer Zeitfenster, Teams-Link, Snapshot-Refresh und Fensterinteraktion bleiben gueltig.

## Erfolgskriterien

- Der zeitlich frueheste sichtbare Termin erscheint in Lane 0, der obersten Lane.
- Ueberschneidungen sind deterministisch von oben nach unten nach Anfangszeit angeordnet.
- Timeline und Jetztlinie bleiben bei jeder Lane-Anzahl an Lane 0 ausgerichtet.
- Auch kleine Bubbles zeigen eine Uhrzeit und so viel Titel wie moeglich.
- Kategorie bestimmt, sofern vorhanden, die Fuellfarbe; Kalender bestimmt den Rahmen-Akzent.
- Die erste gueltige Outlook-Kategoriefarbe in Outlook-Reihenfolge wird uebernommen und fehlende Farben stabil ersetzt.
- Termine aus allen eigenen, erfolgreich lesbaren Outlook-Kalendern im konfigurierten standardmaessigen persoenlichen Outlook-Datenspeicher werden angezeigt.
