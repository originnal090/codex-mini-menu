# Silver glass card

The expanded mini menu is one silver glass card. All existing islands, text and controls belong to that card. The user selected silver material combined with the existing glass appearance, with right-button hold and mouse movement to tilt the whole card and release to return to its normal pose.

## Appearance and interaction

Use neutral silver and white reflections, a finely brushed surface, glass translucency, a polished edge and a directional moving highlight. Retain the existing theme and transparency settings and protect text readability. At rest the material is subtle. While held, highlights and edge lighting react to the card angle. The entire card and its islands share the same perspective transformation. Small, bounded tilts (builder default: 12 degrees per axis) keep the content recognizable. Return smoothly in approximately 280 ms, or immediately when system animations are disabled.

Pressing the right button inside the expanded card starts the gesture; the cursor displacement controls pitch and yaw. Release outside the card, capture loss, Escape, hiding or closing restore a usable normal view. Existing left-click actions, editing, navigation and timers continue to work after return. Right-drag must not trigger these actions. The countdown remains live while the card is held.

## Approach

Retain WPF and the existing controls, using its native 3D visual host during the gesture and ordinary 2D presentation at rest. Add a silver reflection layer and project the actual window contour with the card, preserving its launcher cutout. This keeps the existing control state, timing and accessibility after return. Porting all controls to a web renderer would substantially expand this upgrade; a frozen card screenshot would stop visible countdown updates. The native live surface is the implementation choice.

The reference project provides the idea of view-dependent highlights: https://github.com/HRuiCcc/RuiC-card-skill/blob/main/assets/web-template/app.js . The WPF implementation is written for this plugin and introduces no new runtime dependency.

## Constraints and validation

Keep the current 2.12.0 functionality, including the context module. Work in the existing isolated codex/menu-islands worktree. Preserve preferences and user timer data during the local update. Check the installed binary before replacing it to detect concurrent releases.

Use live WPF visuals, not screen capture. The projected card must fit within the popup at all bounded poses and DPI values. The native blur/hit-test contour follows the projection. Idle material does not run an animation loop. The renderer, gesture state machine, clipping, live timer refresh, return path and existing feature regressions require tests. Inspect rendered dark/light and tilted frames before packaging and installing the next version.

Builder decisions: exact reflection gradients and damping; rendering-class boundaries; neutral brightness adapting to dark/light themes; fitting scale for perspective. Initial delivery applies to the expanded mini menu across all pages, with the floating launcher retaining its role as the position anchor.

Builder review resolved: the launcher cutout remains in screen coordinates, aligned with the independently anchored launcher. Project the outer card contour, then subtract that fixed screen-space hole.
