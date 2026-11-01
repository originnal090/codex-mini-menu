# Development requirements

- Start from the most recent committed and installed feature version; retain ongoing timer data and user preferences.
- Moving interactions target at least 90 updates per second on hardware/display modes that support it. Aim higher when practical. Measure right-button tilt and launcher-position dragging with all shipped visual effects enabled.
- Run `source/scripts/measure-motion.ps1 -Mode tilt -EnforceBudget` and `-Mode drag -EnforceBudget` before releasing movement or visual-effect changes. Report mean update rate, median/P95 intervals, DPI and display refresh. Do not describe WPF rendering callbacks as independently measured GPU presentation FPS.
- Investigate any result below 90 updates/second; do not hide a regression behind faster animation durations. Lower-refresh displays must be identified explicitly when interpreting measurements.
- Launcher material must remain visually continuous with the open glass card through rest, tilt, return and dragging.
- The outer border uses an angular (conic) color gradient and the lower diffuse light has a visibly changing breath cycle. Respect application and system animation preferences and stop animation while hidden.
- Run the complete data and desktop UI suite before packaging; verify installed and plugin-cache binary hashes.
