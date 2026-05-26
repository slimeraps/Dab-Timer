# Dab Timer

Dab Timer is a compact native Windows desktop timer built for a 90-second dab
flow.

## Features

- 90-second main countdown with a 1-second start delay.
- HEAT, COOLING, GET READY, DROP DAB, and DAB TIME phases.
- Sound cues for heat completion and timer completion.
- Borderless custom window with a small top-right close button.
- Drag-to-move support from the background and large timer text.
- WAV WRLD app icon embedded into the executable and taskbar window.
- Custom gradient background.
- Default gradient starts as dark gray and blue.
- Bottom-left `+` button for opening gradient color controls.
- Larger transparent RGB sliders for both gradient colors.
- Hex color fields for typing, selecting, or copying the current gradient colors.
- Bottom-right speaker button for opening sound volume controls.
- Vertical sound volume slider with a 30% default.
- Saved color and volume preferences at `%APPDATA%\DabTimer\settings.txt`.

## Build

This project builds with the Windows .NET Framework C# compiler that ships with
Windows:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /platform:x64 /optimize+ /win32icon:"assets\app.ico" /out:"dist\Dab Timer.exe" /resource:"assets\app.ico,DabTimer.Assets.app.ico" /resource:"assets\heat_finished.wav,DabTimer.Assets.heat_finished.wav" /resource:"assets\timer_finished.wav,DabTimer.Assets.timer_finished.wav" "src\DabTimer\Program.cs"
```

The finished executable is:

```text
dist\Dab Timer.exe
```

## Installer

Build the release installer after building the app:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /platform:x64 /optimize+ /win32icon:"assets\app.ico" /out:"release\DabTimerSetup-1.0.4.exe" /resource:"dist\Dab Timer.exe,DabTimerInstaller.Payload.Dab Timer.exe" "src\DabTimerInstaller\Program.cs"
```

The installer installs Dab Timer to:

```text
%LOCALAPPDATA%\Programs\Dab Timer
```

It also creates Start Menu and Desktop shortcuts and registers an uninstaller in
Windows Apps & Features.

Silent install and uninstall are available for testing:

```powershell
release\DabTimerSetup-1.0.4.exe /silent
"%LOCALAPPDATA%\Programs\Dab Timer\Uninstall Dab Timer.exe" /uninstall /silent
```

For a GitHub Release, upload:

```text
release\DabTimerSetup-1.0.4.exe
```

## Changelog

### 1.0.4

- Added a bottom-right speaker icon for sound controls.
- Added a transparent vertical volume slider for timer sounds.
- Set default sound volume to 30%.
- Saved sound volume with the existing app preferences.
- Changed default gradient colors to dark gray and blue.
- Updated installer metadata and release artifact for `1.0.4`.

### 1.0.3

- Renamed timer phase labels to all-caps display text.
- Changed `heating` to `HEAT`, `dropping dab` to `DROP DAB`, and `dabbing` to `DAB TIME`.
- Added a `GET READY` phase at 20 seconds remaining.
- Extended the post-finish flow to close at 2:00 total timer time.
- Updated installer metadata and release artifact for `1.0.3`.

### 1.0.2

- Changed the borderless close control to a smaller top-right `X`.
- Expanded the gradient settings panel and made the color sliders larger.
- Replaced stock slider controls with transparent custom-drawn sliders.
- Added hex color inputs that can be typed into or selected for copying.
- Updated installer metadata and release artifact for `1.0.2`.

### 1.0.1

- Added a native Windows setup executable for GitHub Releases.
- Installer copies the app into `%LOCALAPPDATA%\Programs\Dab Timer`.
- Installer creates Start Menu and Desktop shortcuts.
- Installer registers a Windows Apps & Features uninstaller.

### 1.0

- Ported the timer from a Python launcher script to a native Windows Forms app.
- Removed batch-file launching and built a standalone `Dab Timer.exe`.
- Embedded the WAV WRLD icon into the executable and app window.
- Added a borderless window with custom close control and drag-to-move behavior.
- Added a custom gradient background with persistent RGB color controls.
- Added updated display typography and shadowed timer text.
- Embedded timer audio cues into the executable.
