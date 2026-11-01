# Rendering the showcase

The README's rotating light, card tilt and floating editor demos use actual application controls with fixture data and a controlled dark backdrop. No personal chat, desktop capture, usage history or user profile is included.

Run `source/scripts/capture-showcase.ps1` on Windows with .NET Framework 4.8. It compiles `source/tests/ShowcaseCapture.cs` and writes frames to `source/artifacts/showcase/`:

- `rotation/`: 240 angular samples covering a full 12-second revolution at 20 samples/s.
- `tilt/`: 150 frames of the live presenter following a scripted gesture and returning to rest.
- `editor/`: 100 frames of the work/rest sheets opening and closing.
- `home.png`, `timer-dark.png`, `timer-light.png`: current interface stills.

The rotation GIF uses 150 frames with an 80 ms delay; the other GIFs use 50 ms delays. Frames are resized to 320 pixels wide with a shared 256-color palette per animation. These are visual demonstrations, not frame-rate benchmarks. See the [2.17.0 measurements](rotating-glass-2.17.0.md).
