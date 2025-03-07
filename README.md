<p align="center">
  <img src="./src/Flow.Launcher.Plugin.ClipboardPlus/Images/clipboard.png" width="90">
</p>

# Flow Launcher ClipboardPlus Plugin

**This plugin is a clipboard manager for [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher). It can help you manage your clipboard history and Windows Clipboard History with support for text, images, and files.**

## ⭐ Features

- Copy & delete & pin record
- Cache images to folder
- Manage records in list and database
- Preview panel for text, images, and files
- Persistent keep records in database
- Words count for text
- Customize copy actions
- Clean clipboard
- Copy files by sorting names
- Manage Windows clipboard history (Windows 10, 11)
- Sync with Windows clipboard history (Windows 10, 11)

## 🖼️ Screenshots

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./images/screenshot1_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="./images/screenshot1_light.png">
  <img alt="Screenshot 1" src="./images/screenshot1_light.png">
</picture>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./images/screenshot2_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="./images/screenshot2_light.png">
  <img alt="Screenshot 2" src="./images/screenshot2_light.png">
</picture>

## 🚀 Installation

* Plugin Store (Recommended)

  1. Search `Clipboard+` in Flow Launcher Plugin Store and install

* Manually Release

  1. Downlaod zip file from [Release](https://github.com/Jack251970/Flow.Launcher.Plugin.ClipboardPlus/releases)
  2. Unzip the release zip file
  3. Place the released contents in your `%appdata%/FlowLauncher/Plugins` folder and **restart** Flow Launcher

* Manually Build

  1. Clone the repository
  2. Run `build.ps1` or `build.sh` to publish the plugin in `.dist` folder
  3. Unzip the release zip file
  4. Place the released contents in your `%appdata%/FlowLauncher/Plugins` folder and **restart** Flow Launcher

## 📝 Usage

### 0. Set Action Keyword

The default action keyword is `cbp`, you can change it in the Flow Launcher.

### 1. Copy Clipboard Records

Click `Copy` or directly the `search result` to copy the data of the current record to clipboard with default options, which you can set in settings.

Click `Pin` to pin the current record to the topmost the query list.

Click `Save` to save the current record to the database.

Click `Delete` to delete the record from list and database.

### 2. Cache Clipboard Images

If you want to automatically save images in your clipboard to a local cache folder, open the `Cache clipboard images in folder` option in settings.

Click `Open cache folder` to view them in the folder `Plugin Settings Directory\CachedImages`.

### 3. Save Records to Database

If you want to keep the text, images or files in the database, open the `Keep text`, `Keep images` or `Keep files` option in settings.

This will automatically save the clipboard data of these formats to the database, and you won't lose them when you exit the Flow Launcher or shutdown your computer.

> Note: It is recommended to cache images using `Cache clipboard images in folder` option, saving large images via `Keep images` to database may affect Flow Launcher performance.

### 4. Windows Clipboard History

For Windows 10 and Windows 11, if you want to merge clipboard history records from Windows Clipboard History, you can enable `Merge Windows Clipboard history into data source` option.

If you want to make query records fully match the Windows clipboard history, you can enable `Use only Windows clipboard history as data source option`. Under this mode, records of files format will not shown in the list. Records from the database will no longer be loaded, and records cannot be saved or pinned to the database.

## 📚 Reference

- [ICONS](https://icons8.com/icons)
- [ClipboardR](https://github.com/rainyl/Flow.Launcher.Plugin.ClipboardR)
- [SharpClipboard](https://github.com/Willy-Kimura/SharpClipboard)

## 📄 License

[Apache License V2.0](LICENSE)
