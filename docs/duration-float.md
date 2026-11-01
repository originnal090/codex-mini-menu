# Duration floating panel

The work and rest duration islands each expand into a floating glass editor from their own screen position. Opening takes 380 ms with an ease-out curve. The timer page blur increases continuously from 0 to 14 DIP; editor controls remain sharp and fade in after the shell starts expanding. Dismissal returns to the source island over 260 ms and removes the blur.

The editor uses one transition progress value for its geometry, material, backdrop and text, so dismissing during opening continues from the current visual state. The background cannot receive pointer or keyboard navigation while editing. Apply saves once; cancel leaves both durations unchanged. Escape, the close button and the surrounding shade dismiss the editor. Page navigation, hiding and unloading clear all temporary effects. Disabled system or menu animations use the final state immediately.

Implementation starts from the 2.14.1 source snapshot. Timer scheduling, statistics and the work/rest data model retain their existing behavior. Build and test through `dev.ps1`; run `source/scripts/test-duration-float.ps1` for animation and lifecycle regression checks and captured examples.
