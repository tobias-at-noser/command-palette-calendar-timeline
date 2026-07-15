# All-Day Event Tags Design

## Problem

Outlook all-day appointments are currently indistinguishable from timed
appointments. Their day-long duration makes them occupy a normal timeline lane
and render as oversized bubbles, even though the compact Command Palette dock
has no vertical space for a separate all-day row.

## Required Behavior

- Outlook's `AllDayEvent` value flows through the Worker, Core snapshot,
  visual projection, and Snapbar view model.
- All-day appointments do not produce normal `TimelineBlock` values and never
  consume a timed-event lane. Timed appointment ordering and lane allocation
  remain unchanged.
- The Snapbar renders at most one all-day tag. It uses the earliest all-day
  appointment ordered by start, end, then ordinal ID; remaining appointments
  are represented as `+N` on that tag.
- The tag is compact, approximately as tall as the 10-pixel time indicators,
  and is bottom-aligned within lane zero. Its title is one line with character
  ellipsis.
- The tag shares `BlocksViewport` and `TimelineFadeMask` with normal bubbles,
  so the existing temporal fade clips it identically. It does not affect window
  height, the rail, the Now line, current-time indicator, or countdown.
- The tag is non-interactive: it does not open Teams and does not change window
  dragging behavior.
- Its native tooltip is a structured vertical list containing only the full,
  sanitized titles of all represented all-day appointments. It contains no
  times, location, calendar, category, duration, or Teams data.
- Existing privacy and confidentiality sanitization applies before title
  selection and tooltip construction.

## Motion Model

`AllDayTagLayout.GetBounds` receives timeline width, current-time ratio, event
start/end ratios, fixed tag width, and a gap from the Now line. It returns an
unclamped canvas left coordinate and the fixed width. The opacity mask clips
entry and exit rather than the layout function.

The tag always parks to the right of the Now line. With `parkedLeft =
nowX + gap` and `parkedRight = parkedLeft + tagWidth`:

- Before the start reaches the Now line, `left = startX`; its left edge follows
  the start timestamp toward the Now line.
- While the start has reached Now and `endX >= parkedRight`, `left =
  parkedLeft`; it stays parked to the right of Now and leaves the countdown
  above Now unobstructed.
- Once `endX < parkedRight`, `left = endX - tagWidth`; its right edge follows
  the end timestamp leftward. The transition begins exactly when `endX ==
  nowX + gap + tagWidth`, so no visual jump occurs.

The model uses `<=`/`>=` at its boundaries to make behavior deterministic at
the exact start and end transition points.

## Architecture

`Appointment.IsAllDayEvent` and `OutlookAppointmentData.IsAllDayEvent` are
optional trailing constructor parameters to preserve existing call sites.
`OutlookCalendarSnapshotSource` reads Outlook COM's `AllDayEvent`, and
`OutlookAppointmentMapper` transfers it while preserving existing sanitization.

`TimelineLayout.Arrange` filters all-day appointments before lane allocation.
`TimelineVisualProjector.ProjectAllDayTags` separately sanitizes and orders
all-day appointments into display-ready Core projections. The Snapbar maps the
first projection and its complete title list into `AllDayTagViewModel`; it
publishes normal blocks independently.

`AllDayTagLayout` is a pure Snapbar layout class that owns tag geometry,
height, gap, and motion. `MainWindow` uses it during every layout update to
draw one non-button `Border` inside the existing masked canvas.

## Testing

- Mapper and source-structure tests verify `AllDayEvent` is read and preserved.
- Core tests verify all-day appointments do not occupy normal lanes, are
  sanitized, and are deterministically ordered for the tag projection.
- Snapbar view-model tests verify one primary tag, `+N`, and title-only tooltip
  data.
- Pure layout tests cover entry, parked, exact handoff, exit, and lane-zero
  bottom alignment.
- WPF source-structure tests verify the tag is rendered in `BlocksCanvas`,
  retains the shared mask, uses `CharacterEllipsis`, creates the title-only
  structured tooltip, and has no click handler.
- Final automated verification runs focused tests, the complete solution test
  project, and the Windows-targeted WPF build. Visual acceptance occurs on
  Windows by checking entry from the right, stationary right-side parking,
  continuous exit to the left, fade behavior, multiple-event `+N`, and
  unobstructed countdown.
