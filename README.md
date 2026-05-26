# Dab Timer

Dab Timer is a compact native Windows desktop timer built for a 90-second dab
flow.

## Features

- 90-second main countdown with a 1-second start delay.
- Heating, cooling, dropping dab, and dabbing phases.
- Sound cues for heat completion and timer completion.
- Borderless custom window with a top-right close button.
- Drag-to-move support from the background and large timer text.
- WAV WRLD app icon embedded into the executable and taskbar window.
- Custom gradient background.
- Bottom-left `+` button for opening gradient color controls.
- RGB sliders for both gradient colors.
- Saved color preferences at `%APPDATA%\DabTimer\settings.txt`.

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

## Changelog

### 1.0

- Ported the timer from a Python launcher script to a native Windows Forms app.
- Removed batch-file launching and built a standalone `Dab Timer.exe`.
- Embedded the WAV WRLD icon into the executable and app window.
- Added a borderless window with custom close control and drag-to-move behavior.
- Added a custom gradient background with persistent RGB color controls.
- Added updated display typography and shadowed timer text.
- Embedded timer audio cues into the executable.
