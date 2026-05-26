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

## Installer

Build the release installer after building the app:

```powershell
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /platform:x64 /optimize+ /win32icon:"assets\app.ico" /out:"release\DabTimerSetup-1.0.exe" /resource:"dist\Dab Timer.exe,DabTimerInstaller.Payload.Dab Timer.exe" "src\DabTimerInstaller\Program.cs"
```

The installer installs Dab Timer to:

```text
%LOCALAPPDATA%\Programs\Dab Timer
```

It also creates Start Menu and Desktop shortcuts and registers an uninstaller in
Windows Apps & Features.

Silent install and uninstall are available for testing:

```powershell
release\DabTimerSetup-1.0.exe /silent
"%LOCALAPPDATA%\Programs\Dab Timer\Uninstall Dab Timer.exe" /uninstall /silent
```

For a GitHub Release, upload:

```text
release\DabTimerSetup-1.0.exe
```

## Changelog

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
