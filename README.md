# Flow Launcher ClipboardPlus Plugin

## About

This plugin is a clipboard manager for [Flow Launcher](https://github.com/Flow-Launcher/Flow.Launcher).

It can help you manage your clipboard history with support for text, images, and files.

## Features

- Preview panel, support images
- Copy & delete & pin record
- Cache images supported
- Manually save images
- Persistent & Keep time settings
- Clear records in list or database
- Words count

## Screenshots

![screenshot1](./images/screenshot1.png)
![screenshot2](./images/screenshot2.png)
![screenshot3](./images/screenshot3.png)
![screenshot4](./images/screenshot4.png)
![screenshot5](./images/screenshot5.png)
![screenshot5](./images/screenshot6.png)

## Installation

### Manually Build

1. Clone the repository
2. Run `build.ps1` or `build.sh` to publish the plugin in `.dist` folder
3. Unzip the release zip file
4. Place the released contents in your `%appdata%/FlowLauncher/Plugins` folder and **restart** Flow Launcher

### Manually Release

1. Downlaod zip file from [Release](https://github.com/Jack251970/Flow.Launcher.Plugin.ClipboardPlus/releases)
2. Unzip the release zip file
3. Place the released contents in your `%appdata%/FlowLauncher/Plugins` folder and **restart** Flow Launcher

### Plugin Store

Sorry, the plugin store is not available yet.

## Usage

![settings](./images/plugin_settings.png)

The default action keyword is `cbp`, you can change it in the FlowLauncher.

Click `Copy` or directly the `search result` to copy the current data to clipboard, click `Delete` to delete the record.

If you want to save images in your clipboard, open the `CacheImages` option in settings.

This will automatically save the images to the cache folder, and you can view them in the folder `Plugin Dictionary/CachedImages`.

If you want to keep the text, images or files in the database, open the `KeepText`, `KeepImage` or `KeepFile` option in settings.

This will save the data to the database, and you won't lose them when you exit the Flow Launcher.

> Note: It is recommended to cache images using `CacheImages` option, 
saving large images via `KeepImage` to database may block query for a little while.

## Todo

- [ ] Text Type Classification
- [ ] Image Type Classification
- [ ] File Type Classification
- [X] Light / Dark Support
- [X] Multi-language support
- [ ] Cached images format definition

## Reference

- [ICONS](https://icons8.com/icons)
- [ClipboardR](https://github.com/rainyl/Flow.Launcher.Plugin.ClipboardR)

## License

[Apache License V2.0](LICENSE)
