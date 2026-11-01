# Rotating glass lighting — 2.17.0

Two soft theme-derived colors occupy opposite sides of the card, revolving clockwise around its center once every 12 seconds. The background and rounded conic border sample the same angular texture through the same rotation transform, so their hues stay aligned throughout the revolution. A radial diffusion mask softens the center and lets the underlying glass show through. The starting phase places cool light toward the upper left and warm light toward the lower right.

The color texture is generated once per theme change. Only the brush transform animates; the card shape remains fixed. Card resizing recomputes a centered square viewport large enough to cover the diagonal at every angle. Hidden cards and reduced-motion settings stop the clock. Lighting remains outside the pointer-input path and retains the existing live card projection.

Validation includes rendered color samples at 0, 90, 180 and 270 degrees in both themes, border/background alignment, transparent center and rounded corners, full-height coverage, resize centering, continuous rotation and clock removal. `test-rotating-field.ps1` adds 39 checks to the desktop suite. Existing glass, launcher input and movement checks also apply.

On the 2560 × 1600 / 240 Hz display, DPI 1.5 / WPF tier 2, enforced measurements passed: tilt 99.3 updates/s (median 9.61 ms, P95 13.48 ms); drag 108.5 updates/s (median 8.74 ms, P95 12.60 ms). These are application/render updates, not independent GPU presentation measurements.

All 4,683 data and desktop assertions passed. The host-switch fixture uses a bounded state wait for ownership reassignment; its final 27 checks passed. Source artifacts contain the release log, native window follow log, phase snapshots and motion measurement reports.
