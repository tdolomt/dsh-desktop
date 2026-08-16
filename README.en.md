# dsh-desktop

English | [中文](README.md)

DSH Desktop — portable desktop build of DeepSeek Harness (unofficial)

Bundles DeepSeek Harness with everything it needs (Node.js, Electron, all
dependencies) into a **self-contained desktop app**: on a fresh Windows 10/11
64-bit machine, no prerequisites of any kind — unpack, install, use.

> ⚠️ This project is an unofficial integration by an individual and has no
> affiliation with, or endorsement from, DeepSeek.
> Please read the [Disclaimer](docs/免责声明.txt) before use.

## Features

- **Standalone app window** (Electron shell) — no browser needed
- Bundled Node.js / Electron / all dsh dependencies — **zero prerequisites**
- Encrypted payload (`payload.dat`, AES-256) — contents are not visible
  without installing
- Install wizard: custom install path (default `D:\Program Files\DSH`),
  progress bar, one-click launch
- Close-to-tray background running; full exit via the tray menu
- One-click uninstall (`uninstall.cmd`, auto-elevates, removes shortcuts and data)
- Data stored under the install dir `data\` by default — no registry writes,
  no system pollution
- Bundled plugins: Task Board, Live Token Stats, Whale Pet, Skin Center
  (10 skins)

## Usage

Download the release: `DSH-Desktop-1.0.0.zip` (see the Releases page)

```
Unzip → run DSH-Installer-Stub.exe → choose install location → done
```

Full guide: [docs/安装说明.txt](docs/安装说明.txt)
(install / update dsh / update Node / plugin management / uninstall / FAQ).

## Daily maintenance (three one-click scripts in the install dir)

After installation, the install directory ships three maintenance scripts
(also kept under `scripts/` in this repository):

| Script | Purpose |
|---|---|
| `安装插件.cmd` | Install a new plugin (enter the package name when prompted) |
| `卸载插件.cmd` | Uninstall a plugin (cleans the bundle references too) |
| `更新插件.cmd` | Update **all** Web UI plugins |
| `更新DSH.cmd` | Update the dsh engine (your data is not touched) |
| `导出数据.cmd` | Export credentials/sessions/settings/plugin config to a zip (desktop) |
| `恢复数据.cmd` | Restore user data from a backup zip after (re)installing |

After any script: exit DSH Web from the tray, then start it again.
Plugins live under the install dir `data\profiles\web\`.
Tip: installing over an existing directory keeps sessions and credentials
automatically.

## Building from source

### Layout

```
dsh-desktop/
  src/             Install wizard (Installer.cs) & encrypted packer (Packer.cs)
  docs/            Install guide & disclaimer
  DSH.ico          App and installer icon
  rebuild.cmd      One-click rebuild script
  DSH-Portable/    Build input (assembled manually, see below)
```

### Requirements

- Windows 10/11 x64
- 7-Zip (detected automatically in common install locations)
- .NET Framework 4.x (built into Windows)
- Build input directory `DSH-Portable/` (the portable kit: `app/`,
  `electron/`, `node/`, `global/`, `cache/`, `data/`, … assembled from a
  working dsh environment — not distributed in this repository)

### Build

```
rebuild.cmd
```


Output: `DSH-Desktop-1.0.0.zip` (installer + encrypted payload + docs).

## Repository contents

- Source code and build scripts only; large artifacts (payload, zip) are
  published as **GitHub Release assets** 
- `rebuild.cmd` uses relative paths — clone anywhere and run
- License: this repository is **MIT Licensed** (see LICENSE); bundled
  components belong to their respective owners (DeepSeek Harness: MIT;
  bundled plugins: Apache-2.0)

## Credits & plugin sources

- Bundled Web UI plugins (Task Board, Live Token Stats, Whale Pet,
  Skin Center, Web UI plugin host) come from the third-party open-source
  project [zhu1090093659/dsh-web-ui](https://github.com/zhu1090093659/dsh-web-ui)
  (Apache-2.0), published on npm under the `@linxin666/*` scope.
  Copyright belongs to their original authors.
- [DeepSeek Harness](https://github.com/deepseek-ai/deepseek-harness)
- [Electron](https://www.electronjs.org/) / [Node.js](https://nodejs.org/)
