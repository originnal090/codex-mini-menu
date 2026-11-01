# Launcher input repair — 2.16.1

Opening the menu must leave the circular launcher draggable and clickable. In 2.16.0 the glass card filled the launcher's former native cutout, so activating the card put its window in the mouse-input path. The launcher's fully transparent pixels also passed through native layered-window hit testing. Internal BeginDrag tests did not exercise either condition.

The card now returns HTTRANSPARENT only inside the visible, interactive launcher ellipse while flat. Windows continues hit testing to the independent launcher on the same UI thread. The launcher's docked fill retains alpha 1/255, enough for its entire circular face to receive native input while transmitting the glass beneath. During tilt, the card retains its projected input region and the launcher remains suppressed until its normal handoff.

`source/scripts/test-launcher-input.ps1` uses real desktop mouse events, including empty space within the circle, to verify drag start, launcher movement, card movement, drag release without a click, click-to-close, reopening, timer navigation and post-tilt input. Before the fix, 13 of its 17 assertions failed and the native hit target was the menu. After the fix, all 17 pass and the native hit target is the launcher. The test is part of TestUI and restores the cursor afterward.

The glass, angular border, breathing light and 90+ movement target remain in place. Run the full desktop suite and both enforced motion measurements before installation.

Release verification: all 4,644 assertions across the data and desktop suites passed. The visual refinement fixture now parks and restores the cursor: a stationary cursor over its programmatically tilted card had triggered real mouse movement with no right button held, releasing the simulated gesture; the unmodified 2.16.0 baseline reproduced the same failure. The host-switch fixture also required an isolated rerun after its fixed-delay ownership assertion failed; all 27 host-follow checks passed on that rerun.

On the 2560 × 1600 / 240 Hz display, DPI 1.5 and WPF tier 2, enforced measurements passed: tilt 110.1 updates/s (median 8.66 ms, P95 11.38 ms), drag 119.8 updates/s (median 8.25 ms, P95 9.48 ms). These measure application/render updates, not independent GPU-presented frames.
