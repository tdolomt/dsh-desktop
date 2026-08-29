# dsh-desktop

[English](README.en.md) | 中文

DSH Desktop —— DeepSeek Harness 便携桌面版(非官方整合)

把 DeepSeek Harness 与其运行所需的一切打包为一个**桌面应用**：无需任何预装环境,解压安装即可使用。

> ⚠️ 本项目为个人整合产物,与 DeepSeek 官方无隶属、授权或合作关系。
> 使用前请阅读 [免责声明](docs/免责声明.txt)。

## 特性

- **独立应用窗口**
- 内置 Node.js / Electron / dsh 全部依赖,**零前置环境**
- 安装向导:自定义安装位置(默认 `D:\Program Files\DSH`)
- 数据默认保存在安装目录 `data\` 下,不写注册表
- 内置插件:任务看板、实时令牌统计、鲸鱼娘宠物、皮肤中心、梁神模式、插件市场(`dshmarket`)、右侧边栏(`dsh-better-sidebar`)、上下文管理(`dsh-context`)、多智能体(`@nanmicoder/dsh-agent-teams`)
- 插件依赖的 pnpm store/cache/state 默认放在 **`D:\ProgramData\DSH Desktop`**(通过安装目录下 `pnpmstore.ini` 配置),不写 C 盘,卸载重装后仍可复用
- 内置插件来源见 [致谢与插件来源](#致谢与插件来源)。

## 使用

下载发行版:见 Releases

详细使用说明见 [docs/安装说明.txt](docs/安装说明.txt)

## 日常维护

安装完成后,安装目录里自带六个维护脚本(仓库 `scripts/` 目录同步保存):

| 脚本 | 用途 |
|---|---|
| `安装插件.cmd` | 双击后输入插件包名,安装新插件 |
| `卸载插件.cmd` | 双击后输入插件包名,卸载插件 |
| `更新插件.cmd` | 一键更新**全部** Web UI 插件 |
| `更新DSH.cmd` | 一键更新 dsh 引擎|
| `导出数据.cmd` | 把凭证/会话/设置/插件配置打包成 zip(桌面),重装前导出 |
| `恢复数据.cmd` | 重装后从备份 zip 恢复数据 |

所有脚本执行后:重启应用生效。
插件安装位置:安装目录 `data\profiles\web\`。

可以直接覆盖更新

- pnpm 插件依赖的 store/cache/state 由安装目录下 `pnpmstore.ini` 控制(默认 `D:\ProgramData\DSH Desktop`),修改后重启应用生效。

## 从源码构建

### 目录结构

```
dsh-desktop/
  src/             安装向导(Installer.cs)与加密打包器(Packer.cs)源码
  docs/            安装说明与免责声明
  DSH.ico          应用与安装器图标
  rebuild.cmd      一键重建脚本
  DSH-Portable/    构建输入(需自行准备,见下)
```

### 构建要求

- Windows 10/11 x64
- 7-Zip
- .NET Framework 4.x
- 构建输入目录 `DSH-Portable/`(即"便携包源"):
  `app/`、`electron/`、`node/`、`global/`、`cache/`、`data/` 等
  (由已安装好的 dsh 环境组装,不在仓库内分发)

### 构建步骤

```
rebuild.cmd
```


产物:`DSH-Desktop-2.1.0.zip`(安装程序 + 加密数据包 + 文档)。

## 仓库内容说明

- 本仓库只含**源码与构建脚本**;巨型产物(payload、zip)以
  GitHub Releases 附件形式发布
- `rebuild.cmd` 使用相对路径,clone 到任意位置即可运行
- 许可:本仓库代码采用 **MIT License**(见 LICENSE);
  内置组件版权归各自所有者(DeepSeek Harness:MIT;内置插件:Apache-2.0)

## 致谢与插件来源

- 内置 Web UI 插件(任务看板、实时令牌统计、鲸鱼娘宠物、皮肤中心、梁神模式、Web UI 插件宿主)
  来源于第三方开源项目
  [zhu1090093659/dsh-web-ui](https://github.com/zhu1090093659/dsh-web-ui)
  (Apache-2.0),经 npm 以 `@linxin666/*` 作用域发布,版权归其原作者所有。
- 插件市场 `dshmarket` 来源于 [dsh-market/dsh-market](https://github.com/dsh-market/dsh-market)。
- 右侧边栏 `dsh-better-sidebar` 来源于 [omdsh-dev/DSH-better-sidebar](https://github.com/omdsh-dev/DSH-better-sidebar)。
- 上下文管理 `dsh-context` 来源于 [bowenliang123/dsh-context](https://github.com/bowenliang123/dsh-context)。
- 多智能体 `@nanmicoder/dsh-agent-teams` 来源于 [NanmiCoder/dsh-agent-teams](https://github.com/NanmiCoder/dsh-agent-teams)。
- [DeepSeek Harness](https://github.com/deepseek-ai/deepseek-harness)
- [Electron](https://www.electronjs.org/) / [Node.js](https://nodejs.org/)
