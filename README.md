# Flow Launcher Clipboard Plugin

The Clipboard plugin for [Flow.Launcher](https://github.com/Flow-Launcher/Flow.Launcher)

Give me a star :star: if you like this project~

![images/preview.gif](https://raw.githubusercontent.com/rainyl/Flow.Launcher.Plugin.ClipboardPlus/master/Images/preview.gif)

## About

This Project is developed on the shoulders of giant:

Original Repo: [Wox.Plugin.ClipboardManager](https://github.com/Wox-launcher/Wox.Plugin.ClipboardManager)

Ported to Flow.Launcher: [Flow.Launcher.Plugin.ClipboardHistory](https://github.com/liberize/Flow.Launcher.Plugin.ClipboardHistory)

## Features

- Preview panel, support images
- Copy & delete & pin record
- Cache images supported
- Manually save images
- Persistent & Keep time settings
- Clear records in memory only or clear database
- Words count

## Installation

### Using release

1. Downlaod zip file from [Release](https://github.com/rainyl/Flow.Launcher.Plugin.ClipboardPlus/releases)
2. Place the contents of the Release zip in your %appdata%/FlowLauncher/Plugins folder and **restart**  FlowLauncher.

### Using plugin store

Now you can install it using plugin store!

1. Install: `pm install ClipboardPlus`
2. Update: `pm update ClipboardPlus`

## Usage

The default keyword is `ClipboardPlus`, you can change it in the FlowLauncher settings.

Click `Copy` or directly the `search result` to copy the current data to clipboard, click `Delete` to delete the record.

If you want to save images in your clipboard, open the `CacheImages` option in settings.

Note: It is recommended to cache images using `CacheImages` option, saving large images
via `KeepImage` to database may block query for a little while.

![settings1](./Images/settings1.png)
![settings2](./Images/settings2.png)

## Todo List

- [x] Save images manually
- [x] Persistent
- [x] Keep time
- [x] Word Count
- [ ] Cached images format definition
- [ ] Image OCR

## Acknowledgement

- [IconFont](https://www.iconfont.cn)

## License

[Apache License V2.0](LICENSE)
