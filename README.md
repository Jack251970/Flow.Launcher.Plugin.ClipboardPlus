<p align="center">
  <img src="./src/Flow.Launcher.Plugin.ClipboardPlus/Images/clipboard.png" width="90">
</p>

<h1>
	Flow Launcher ClipboardPlus Plugin
</h1>

**This plugin is a clipboard manager for [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher). It can help you manage your clipboard history and customizing copy rules with support for text, images, and files.**

## ⭐ Features

- Copy & delete & pin record
- Cache images to folder
- Manage records in list and database
- Preview panel for text, images, and files
- Persistent keep records in database
- Words count for text
- Customize copy rules
- Clean clipboard
- Copy files by sorting names

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
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./images/screenshot3_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="./images/screenshot3_light.png">
  <img alt="Screenshot 3" src="./images/screenshot3_light.png">
</picture>
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./images/screenshot4_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="./images/screenshot4_light.png">
  <img alt="Screenshot 4" src="./images/screenshot4_light.png">
</picture>
<picture>
  <source media="(prefers-color-scheme: dark)" srcset="./images/screenshot5_dark.png">
  <source media="(prefers-color-scheme: light)" srcset="./images/screenshot5_light.png">
  <img alt="Screenshot 5" src="./images/screenshot5_light.png">
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

## 🪧 Tips

The default action keyword is `cbp`, you can change it in the FlowLauncher.

Click `Copy` or directly the `search result` to copy the current data to clipboard, click `Delete` to delete the record.

If you want to save images in your clipboard, open the `CacheImages` option in settings.

This will automatically save the images to the cache folder, and you can view them in the folder `Plugin Dictionary/CachedImages`.

If you want to keep the text, images or files in the database, open the `KeepText`, `KeepImage` or `KeepFile` option in settings.

This will save the data to the database, and you won't lose them when you exit the Flow Launcher.

> Note: It is recommended to cache images using `CacheImages` option, 
saving large images via `KeepImage` to database may block query for a little while.

## 📚 Reference

- [ICONS](https://icons8.com/icons)
- [ClipboardR](https://github.com/rainyl/Flow.Launcher.Plugin.ClipboardR)
- [SharpClipboard](https://github.com/Willy-Kimura/SharpClipboard)

## 📄 License

[Apache License V2.0](LICENSE)