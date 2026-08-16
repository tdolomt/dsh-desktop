# dsh-web-portable

[English](README.en.md) | 中文

DeepSeek Harness Web 便携版 —— 第三方整合打包项目(非官方)

把 DeepSeek Harness 与其运行所需的一切(Node.js、Electron、全部依赖)
打包为一个**自包含桌面应用**:一台全新 Windows 10/11 64 位电脑,
无需任何预装环境,解压安装即可使用。

> ⚠️ 本项目为个人整合产物,与 DeepSeek 官方无隶属、授权或合作关系。
> 使用前请阅读 [免责声明](docs/免责声明.txt)。

## 特性

- **独立应用窗口**(Electron 外壳),无需浏览器
- 内置 Node.js / Electron / dsh 全部依赖,**零前置环境**
- 加密数据包(`payload.dat`,AES-256),不安装无法查看内容
- 安装向导:自定义安装位置(默认 `D:\Program Files\DSH`)、进度条、一键启动
- 关窗后台运行(托盘常驻),托盘菜单完全退出
- 一键卸载(`uninstall.cmd`,自动提权,清除快捷方式与数据)
- 数据默认保存在安装目录 `data\` 下,不写注册表、不污染系统
- 内置插件:任务看板、实时令牌统计、鲸鱼娘宠物、皮肤中心(10 款皮肤)

## 使用

下载发行版:`DSH-Web-1.0.0.zip`(见 GitHub Releases 页)

```
解压 → 双击 DSH-Installer-Stub.exe → 选择安装位置 → 完成
```

详细使用说明见 [docs/安装说明.txt](docs/安装说明.txt)
(安装/更新 dsh/更新 Node/插件管理/卸载/排障)。

## 从源码构建

### 目录结构

```
dsh-web-portable/
  src/             安装向导(Installer.cs)与加密打包器(Packer.cs)源码
  docs/            安装说明与免责声明
  DSH.ico          应用与安装器图标
  rebuild.cmd      一键重建脚本
  DSH-Portable/    构建输入(需自行准备,见下)
```

### 构建要求

- Windows 10/11 x64
- 7-Zip(自解压打包用;任意安装位置均可,脚本自动探测)
- .NET Framework 4.x(Windows 自带,无需安装)
- 构建输入目录 `DSH-Portable/`(即"便携包源"):
  `app/`、`electron/`、`node/`、`global/`、`cache/`、`data/` 等
  (由已安装好的 dsh 环境组装,不在仓库内分发)

### 构建步骤

```
rebuild.cmd
```

产物:`DSH-Web-1.0.0.zip`(安装程序 + 加密数据包 + 文档)。

## 仓库内容说明

- 本仓库只含**源码与构建脚本**;巨型产物(payload、zip)以
  GitHub Releases 附件形式发布(单文件上限 2GB)
- `rebuild.cmd` 使用相对路径,clone 到任意位置即可运行
- 许可:Apache-2.0(与 DeepSeek Harness 一致;内置组件版权归各自所有者)

## 致谢与插件来源

- 内置 Web UI 插件(任务看板、实时令牌统计、鲸鱼娘宠物、皮肤中心、Web UI 插件宿主)
  来源于第三方开源项目
  [zhu1090093659/dsh-web-ui](https://github.com/zhu1090093659/dsh-web-ui)
  (Apache-2.0),经 npm 以 `@linxin666/*` 作用域发布,版权归其原作者所有。
- [DeepSeek Harness](https://github.com/deepseek-ai/deepseek-harness)
- [Electron](https://www.electronjs.org/) / [Node.js](https://nodejs.org/)
