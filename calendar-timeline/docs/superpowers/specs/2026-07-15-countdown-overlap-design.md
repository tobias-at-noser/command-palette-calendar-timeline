# Design: Countdown bei parallelen Terminen

## Ziel

Der Countdown soll auch bei laufenden, parallelen und aufeinanderfolgenden Terminen den naechsten bevorstehenden Start anzeigen. Termine erhalten unabhaengig davon eine rein visuelle Hervorhebung kurz vor und waehrend ihrer Laufzeit.

## Countdown-Auswahl und Sichtbarkeit

- Der Countdown zaehlt immer bis zum gueltigen sichtbaren Termin mit dem fruehesten `Start > jetzt`. Laufende oder ueberlappende Termine verhindern diese Auswahl nicht.
- Er erscheint nur, wenn das Ziel mehr als fuenf Minuten in der Zukunft liegt. Die Restzeit wird wie bisher auf fuenf Minuten gerundet.
- Bei einer Restzeit von hoechstens fuenf Minuten, ohne zukuenftigen sichtbaren Termin oder wenn der Zieltermin ausserhalb des sichtbaren Timeline-Fensters liegt, bleibt er ausgeblendet.
- All-Day-Termine und Termine mit ungueltiger Dauer werden nicht als Ziel beruecksichtigt.
- Startet der angezaehlte Termin, wird der danach frueheste zukuenftige Termin sofort zum neuen Ziel. Der Countdown erscheint wieder, wenn dessen Start mehr als fuenf Minuten entfernt ist; bei unmittelbar folgenden Terminen bleibt er dadurch ruhig verborgen.

## Termin-Hervorhebung

- Jeder gueltige sichtbare Termin ist von `Start - 5 Minuten` bis exklusiv `Ende` hervorgehoben.
- Die Regel gilt pro Termin. Mehrere parallele oder ueberlappende Termine sind gleichzeitig hervorgehoben, unabhaengig davon, ob ein Countdown sichtbar ist.
- Die Hervorhebung hat keinen Text und veraendert weder Titel noch Reihenfolge. Sie besteht aus verstaerkter Kontur und einem dezenten hellen Overlay.

## Countdown-Position und Bewegung

- Die vertikale Position bleibt fest an der Jetzt-Linie in Lane 0.
- Die Basisposition liegt rechts neben der Jetzt-Linie. Ueberdeckt sie einen laufenden Termin, verschiebt sich der Countdown ausschliesslich horizontal nach rechts, bis hinter dessen rechtem Rand. Bei mehreren laufenden Terminen gilt der rechteste kollidierende Rand.
- Der Countdown darf bei Platzmangel einen laufenden Termin teilweise ueberdecken. Er darf niemals den Zieltermin ueberdecken, dessen Start er anzeigt.
- Wenn zwischen der erforderlichen horizontalen Position und dem linken Rand des Zieltermins nicht genug Raum fuer den Countdown ist, bleibt er an der maximal moeglichen Position vor dem Zieltermin. Ein Teil des laufenden Termins kann dabei ueberdeckt werden.
- Die bestehende 3-px-Pendelbewegung bleibt in einer stabilen Position erhalten. Bei einer horizontalen Umpositionierung pausiert sie; eine rund 150 ms lange, sanft auslaufende Animation bewegt den Countdown zur neuen Basisposition. Anschliessend setzt die Pendelbewegung dort fort.

## Architektur und Datenfluss

- Die UI-freie Snapbar-Logik bestimmt Countdown-Ziel, Sichtbarkeit und den Hervorhebungszustand je Block.
- Die WPF-Schicht berechnet aus Blockgrenzen und dem Zielblock die erlaubte X-Position, setzt die visuelle Hervorhebung und fuehrt den Uebergang zwischen Basispositionen aus.
- Der bestehende Minuten-Refresh aktualisiert Countdown, Hervorhebungen und Positionen gemeinsam.

## Tests

- Countdown-Auswahl bei einem laufenden Termin, parallelen laufenden Terminen und ueberlappenden Terminen.
- Sichtbarkeit an der Fuenf-Minuten-Grenze sowie Wiedereinblendung nach dem Start eines angezaehlten Termins.
- Gleichzeitige Hervorhebung mehrerer Termine von fuenf Minuten vor Start bis zum Ende.
- Horizontale Kollisionsbehandlung mit einem oder mehreren laufenden Terminen, ohne Ueberdeckung des Zieltermins.
- Animationszustand: Pendelbewegung pausiert waehrend der Positionsanimation und wird danach fortgesetzt.

## Erfolgskriterien

- Ein laufender Termin unterdrueckt den Countdown zum naechsten Start nicht mehr.
- Alle Termine sind kurz vor und waehrend ihrer Laufzeit klar, aber ohne Text zusaetzlich hervorgehoben.
- Der Countdown bleibt lesbar, bewegt sich nur horizontal und verdeckt nie den Termin, auf den er zaehlt.
- Bei direkt aufeinanderfolgenden Terminen flackert kein Countdown kurz auf.
