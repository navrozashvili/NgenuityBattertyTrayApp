# NgenuityBattertyTrayApp

Small Windows tray app that displays the battery percentage (and charging state) for supported **HyperX / NGENUITY wireless devices**, by talking to NGENUITY’s internal “communication hub” over local IPC.

## Important note about provenance

This project was created as a **single one-shot AI-generated implementation** based on **decompiled NGENUITY source/artifacts** to reproduce the IPC protocol and message shapes. It is **not** an official HP/HyperX project, and it may break when NGENUITY changes.

## How it works (high level)

- **Startup**: `Program.cs` starts a WinForms `ApplicationContext` (`TrayAppContext`) and creates a `NotifyIcon` with a context menu.
- **Connecting to NGENUITY**: `NgenuityHubClient` connects to the local NGENUITY “communication hub” using:
  - a **REQ/REP** socket for request/response commands (ports `6890-6899`)
  - a **SUB** socket for notifications/events (ports `7890-7899`)
- **Device discovery**:
  - The app queries a device list, then asks for device info for each entry.
  - Only **wireless products** are shown (battery info is under “Wireless RF” in NGENUITY).
  - A small rule mirrors NGENUITY behavior for headsets (hides non-dongle headset entries).
- **Battery + charging state**:
  - Battery percentage is updated via a mix of polling and events.
  - Charging state is primarily driven by NGENUITY notifications (events).
- **Tray rendering**: `BatteryTrayIconRenderer` draws the tray icon for the current percentage and charging flag, and the tooltip text is kept within Windows’ ~63 character limit.

## Why charging can look “wrong” when you first start the app

NGENUITY models **charging / not charging as events** (notifications). That means:

- If your device **is already charging** and **this tray app was not running** at the time charging started, you may **not** see a charging indicator immediately after launching the app.
- The app will update the charging icon once NGENUITY emits a `BatteryChargingNotification` (for example, after unplug/replug, docking changes, or any action that causes NGENUITY to publish a new charging event).

The app does poll battery info, but it intentionally treats charging state as “event-first” because some devices may report `NoCharging` in polled data even while charging.

## Requirements

- **Windows 10/11**
- **NGENUITY must be running** (the app connects to its local communication hub)
- A supported **wireless HyperX device**

If NGENUITY isn’t running, the tray app will still start, but it won’t be able to connect to the IPC hub; battery will show as unavailable until NGENUITY starts and the app reconnects.

## Build & run

- Open `NgenuityBattertyTrayApp.slnx` in Visual Studio (or open the project folder).
- Build and run the `NgenuityBattertyTrayApp` project.

Once running, look for the tray icon. Right-click it to:

- select a device
- refresh devices
- toggle “Start with Windows”
- open the log folder
- exit

## Logs / troubleshooting

- Logs are written under the app’s logs directory (see `AppPaths.cs` and the “Open log folder” menu item).
- If you see connection failures, confirm:
  - NGENUITY is running
  - local firewall/security software isn’t blocking localhost ports in the `6890-6899` / `7890-7899` ranges

## Legal / disclaimer

- This is an **unofficial** community project.
- NGENUITY, HyperX, and related names are trademarks of their respective owners.