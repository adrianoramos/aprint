# Aprint

Aprint is a Windows application for capturing, annotating, redacting, and sharing screenshots. It stays in the system tray and is designed for people who repeat this workflow for documentation, support, training, and software development: capture, highlight what matters, review, then copy or save without opening a separate editor for every image.

## Features

- Capture all monitors, the monitor under the cursor, or a selected rectangular region.
- Annotate while selecting a region or afterward in a dedicated editing window.
- Freehand drawing, lines, arrows, rectangles, circles, translucent highlighting, formatted text, and numbered markers.
- Solid or pixelated masks, applied manually or to text regions suggested by Windows OCR.
- Cropping, undo/redo, and tool preferences remembered between captures.
- Configurable workflows to copy, save, open in the editor, or copy and save; global shortcuts, notifications, and startup with Windows.

## Getting Started

After starting Aprint, look for its icon in the system tray. The application does not need to keep a main window open.

To start a capture, press `Ctrl+Shift+P` (the default shortcut), double-click the tray icon, or choose **Capture** from its menu. You can also configure `Print Screen` as the shortcut. The tray menu lets you switch between the configured capture mode and region selection. By default, Aprint starts in region mode and copies the completed capture to the clipboard.

Under **Settings > Capture**, choose a mode:

- **All screens** captures the combined virtual area of all monitors.
- **Screen under cursor** captures the monitor where the pointer is located.
- **Select region** lets you drag a rectangle across the virtual screen area. With multiple monitors, the bar at the top also lets you capture all monitors, the monitor under the cursor, or a specific monitor.

When selecting a region, drag to define the rectangle. You can choose what happens when you click outside the selection: start a new selection, exit capture mode, or ignore the click. Press `Esc` to cancel.

## Annotate While Capturing

Once a region is selected, use the floating toolbar to mark it before completing the capture. Select a tool and drag over the region; open its dropdown to adjust the options. This workflow includes freehand drawing, lines, arrows, rectangles, circles, counters, text, highlighting, and masks. Rectangles and circles can be filled. Color, stroke width, size, and text settings are shared with the editor.

Choose an action from the floating toolbar or use:

| Shortcut | Action |
| --- | --- |
| `Enter` | Complete the capture using the configured actions |
| `Ctrl+Enter` | Send the image to the editor |
| `Ctrl+C` | Copy the image to the clipboard |
| `Ctrl+S` | Save the image |
| `Esc` | Cancel the selection |

## Edit the Capture

Choose **Send to editor** on the floating toolbar or enable **Open editor after capture** in Settings. In the editor window, select a tool from the toolbar and drag over the image. The new capture button starts another capture without requiring you to return to the tray menu.

- **Drawing and shapes:** freehand drawing, lines, and arrows; rectangles and circles with or without fill. Adjust the stroke color and width.
- **Highlighting:** mark areas with a translucent stroke; adjust its color and width.
- **Text:** add text and choose its font, size, color, and styles such as bold, italic, underline, and strikethrough.
- **Counter:** add numbered markers to indicate steps; adjust the marker size and the number and circle colors.
- **Masks:** cover an area with a solid color or pixelate it. Choose the mask color and pixel size. Press `F8` to switch mask modes.
- **Crop:** activate the tool, drag the handles to adjust the framing, and press `Enter` to apply; `Esc` cancels the crop.
- **Review:** use undo/redo to move through recent changes.

Tool appearance settings are saved and reused for future captures. To copy or save, use the toolbar buttons or press `Ctrl+C` and `Ctrl+S`. Automatic saving uses the configured folder; **Save without confirmation** controls whether the editor opens a save dialog. The dialog supports PNG, JPEG, and BMP. PNG and JPEG files include metadata identifying Aprint.

### Editor Shortcuts

| Shortcut | Action |
| --- | --- |
| `F2` | Freehand drawing |
| `F3` / `F4` | Line / arrow |
| `F5` / `F6` | Rectangle / circle |
| `F7` / `F8` | Highlight / mask |
| `F9` / `F10` | Counter / text |
| `,` / `F11` | Activate crop |
| `Ctrl+F8` | Start automatic masking |
| `Ctrl+Z` / `Ctrl+Y` | Undo / redo |
| `Ctrl+C` / `Ctrl+S` | Copy / save |
| `Ctrl+N` | Start a new capture |

## Automatic Masking

In the editor, choose **Automatic mask** from the mask tool menu or press `Ctrl+F8`. During region capture, the same option is available in the floating toolbar's mask menu.

Aprint uses Windows OCR locally to suggest rectangles around lines of text. Wait for detection, click each region to include or exclude its mask, and drag to add areas manually. Hold `Space` to temporarily reveal the original image, then release it to hide the image again. Press `Enter` or choose **Apply masks** to confirm; `Esc` cancels.

This feature applies visual masks; it does not extract or validate detected text, and detection may not find every sensitive item. Review the suggestions and add manual masks before sharing. Recognition runs locally and depends on the OCR languages installed in Windows. If the required engine or language is unavailable, use manual masks.

## Settings

Open **Settings** from the tray menu. Options include:

- **Shortcut:** choose a global modifier-and-key combination or use `Print Screen`.
- **Capture:** default mode, action on completion (clipboard, disk, or both), destination folder, and what happens when you click outside the selected region.
- **Editor:** automatic opening, initial window state, default or last-used tool, tool labels, saving without a dialog, closing after copying/saving, and closing after inactivity (30 seconds, 1, 3, or 5 minutes).
- **General:** notifications and click behavior (do nothing, open the folder, open the file, or open the capture in the editor), capture sound, language, and startup with Windows.

The language can follow Windows or be set to Brazilian Portuguese, U.S. English, or Spanish. Changes in the Settings window are applied when you save.

## What Makes Aprint Useful

Aprint brings capture, annotation, and image delivery together in one continuous workflow. A selected region can be annotated before completing the capture or sent to the editor for review, cropping, and adjustments; the image can then be copied, saved, or routed according to your preferences.

The workflow is customizable for recurring tasks: choose a global shortcut, default mode and destination, starting tools, and visual preferences that are reused between captures. Numbered markers help document steps, while solid and pixelated masks and local OCR suggestions help redact information before sharing.

As a tray application, Aprint stays available without keeping a main window open. Captures, edits, and text recognition are processed locally; images and recognized text are not sent to external services.

## Privacy and Processing

Capturing, editing, and text detection happen locally on your computer; Aprint does not send images or recognized text to external services. Depending on your selected actions, an image can be copied to the clipboard or saved to disk. Automatic masking is an aid, not a substitute for manual review, and temporarily revealing the image with `Space` displays its original content on screen.

## Requirements

- Windows 10 version 1809 or later.
- .NET 8 SDK for development.
- .NET 8 Desktop Runtime to run a framework-dependent publish.
- Inno Setup 6 to build the installer.

## Project Structure

```text
src/Aprint/          WinForms application and product features
tests/Aprint.Tests/  Automated tests
installer/scripts/   Inno Setup script and release automation
artifacts/           Local publish and build outputs
```

## Development

Restore dependencies and build the solution:

```powershell
dotnet restore Aprint.sln
dotnet build Aprint.sln
```

Run the tests:

```powershell
dotnet test Aprint.sln
```

Run the application:

```powershell
dotnet run --project src/Aprint/Aprint.csproj
```

## Publishing and Installer

Publish the application to `artifacts/publish`:

```powershell
$Version = '1.0.0'
dotnet publish src/Aprint/Aprint.csproj --configuration Release --output artifacts/publish "-p:Version=$Version"
```

Build the publish output and installer in one step:

```powershell
.\installer\scripts\build-installer.ps1 -Version 1.0.0
```

The script uses the version provided in the command for both the application and installer. The GitHub workflow also calculates the version and applies it to the build, publish output, and installer. There is no default version stored in a shared MSBuild file.

The installer is framework-dependent and requires the .NET 8 Desktop Runtime on the target computer. The outputs in `artifacts/`, `bin/`, and `obj/` are generated locally and should not be committed.

User settings are loaded and saved by `SettingsStore`. The application is Windows-only and uses Windows APIs for the system tray, global shortcuts, clipboard, screen capture, and OCR.
