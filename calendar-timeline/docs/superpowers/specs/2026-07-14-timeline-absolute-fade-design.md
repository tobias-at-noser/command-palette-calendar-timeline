# Timeline Absolute Fade Design

## Problem

Appointment blocks are projected against the complete timeline window, but the
viewport opacity mask currently uses `RelativeToBoundingBox`. In WPF, the
bounding box used for an opacity brush can depend on the rendered content. As a
result, the mask can visually follow an appointment block instead of remaining
anchored to the timeline, even though the viewport and canvas have explicit
dimensions.

The existing `.12` and `.88` gradient stops also approximate, rather than
represent, the temporal boundaries of the configured window. The timeline runs
from 30 minutes before now through four hours after now, so now is exactly
`1 / 9` of the way through the window.

## Required Behavior

- The left fade starts at zero opacity at `now - 30 minutes` and reaches full
  opacity at the Now line.
- The right fade remains fully opaque through `now + 3.5 hours` and reaches
  zero opacity at `now + 4 hours`.
- The exact normalized gradient stops are `0`, `1 / 9`, `8 / 9`, and `1`.
- The rail and appointment mask use the same shared fade-boundary constants.
- The mask coordinate system spans the full timeline width and is independent
  of appointment block position, size, count, or removal.
- Resizing the window recomputes the absolute mask endpoint from the current
  timeline width.
- Appointment projection remains unbounded and the masked viewport remains
  clipped to the timeline.
- The Now line, current-time indicator, and countdown indicator remain outside
  the masked layer and retain their current Z-order.
- Minimum block width, lane geometry, bubble styling, tooltips, click behavior,
  countdown behavior, and window-height behavior remain unchanged.

## Design

`TimelineSnapbarLayout` is the single source of truth for temporal layout
ratios. It keeps `NowRatio = 1d / 9d` and adds `FadeInEndRatio = NowRatio` plus
`FadeOutStartRatio = 1d - NowRatio`.

Both horizontal gradients bind their middle stops to those constants through
`x:Static`. This removes duplicated decimal approximations and guarantees that
the visual rail and appointment opacity mask transition at identical times.

The `BlocksViewport` retains its explicit full-timeline dimensions and
`ClipToBounds="True"`. Its opacity brush is named `TimelineFadeMask` and uses
`MappingMode="Absolute"`. `UpdateLayoutMetrics()` assigns the brush a start
point of `(0, 0)` and an endpoint of `(timelineWidth, 0)` whenever layout is
updated. Consequently, gradient offsets map to fixed positions in timeline
coordinates, regardless of the bounds of the rendered appointments.

## Testing

`TimelineSnapbarLayoutTests` verifies that the two shared ratios are exactly
`1 / 9` and `8 / 9`, and that they remain symmetric around the window.

`Task7ReviewFixTests` verifies the WPF source contract: both gradients consume
the shared constants, the appointment mask uses absolute mapping, the mask is
named, its endpoint is coupled to `timelineWidth`, and no individual canvas or
appointment receives an opacity mask. These structural tests are used because
the Linux test runtime cannot execute a WPF pixel-level rendering test.

Final automated verification runs the focused tests, the full test project,
and a WPF build. Visual acceptance on Windows checks the exact temporal fade
boundaries, resizing, centered-block opacity, and unmasked indicators.
