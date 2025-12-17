# RegistrationEasy

**RegistrationEasy** 是一个完整的“软件授权 + 注册验证”示例项目，演示如何在 **跨平台（Windows / macOS / Linux / Android）** 应用中集成机器码授权逻辑。

- 使用 “帮您发发”（80fafa）网站生成注册码，让软件变现更加容易。
- 提供从 **本地构建** 到 **GitHub Actions 发布** 的完整示例流水线。

项目完全开源，采用最宽松的 [MIT License](LICENSE)，您可以自由地将其集成到商业软件中，或将其移植到其他编程语言。

---

## 🌍 跨平台支持 / Cross-Platform

基于 **Avalonia UI 11** 和 **.NET 8** 构建，当前项目结构支持：

- **Windows** (x64, x86)
- **macOS** (Apple Silicon, Intel)
- **Linux** (x64, ARM)

## ✨ 功能概览 / Features

- **机器码生成（Machine ID）**：
  - 根据 CPU、主板、硬盘等硬件信息生成稳定的机器特征码。
  - 内置格式化逻辑，生成便于人工输入和复制的机器码字符串。
- **注册码解析与验证**：
  - 使用标准 AES 加密算法对注册码进行加解密与验证。
  - 支持从注册码中解析授权信息（有效期、额度等），并与本机机器码校验。
- **授权信息模型**：
  - **有效期信息**：支持永久授权或按时长（月/年）授权。
  - **额度信息**：支持配额 / 次数等扩展字段，方便业务自定义。
- **跨平台 UI 示例**：
  - 使用 Avalonia 构建统一界面，桌面与 Android 复用相同的 View / ViewModel。
  - 提供一个完整的“输入机器码 → 输入注册码 → 验证并显示结果”的示范界面。
- **开箱即用**：
  - 无需搭建服务器，无需集成支付工具，即可将本项目的服务端逻辑与 UI 作为模板快速复用。

## 🧱 项目结构 / Project Structure

解决方案采用“共享逻辑 + 启动壳”的结构：

```text
RegistrationEasy/
  RegistrationEasy.Common/      # 共享业务逻辑 + 视图 + ViewModel（核心部分）
  RegistrationEasy/             # 桌面端启动项目（RegistrationEasy.Desktop）
  RegistrationEasy.Android/     # Android 启动项目
  build.sh                      # 本地一键构建与运行脚本（Git Bash / *nix）
```

- `RegistrationEasy.Common`：
  - 目标框架：`net8.0`
  - 包含机器码生成服务、注册码验证逻辑、视图和 ViewModel，是最值得直接复用的部分。
- `RegistrationEasy`（桌面）：
  - 目标框架：`net8.0`
  - 使用 Avalonia Desktop 启动共享 `App`，提供 Windows / macOS / Linux 桌面应用入口。
- `RegistrationEasy.Android`：
  - 目标框架：`net8.0-android`
  - 使用 `Avalonia.Android` 启动共享 `App`，提供 Android 应用入口（APK）。
- `build.sh`：
  - 统一封装桌面构建、Android 构建、模拟器启动与部署等命令，便于日常开发调试。

更多底层设计与踩坑说明可参考文档：  
`doc/dotnet10 + Avalonia11 跨平台桌面 + Android 应用实现方案.md`

## 🚀 使用方式 / Quick Start

### 环境要求 / Prerequisites

- 必备：
  - [.NET 8 SDK](https://dotnet.microsoft.com/download)
- 可选（如需构建和运行 Android）：
  - JDK 17
  - Android SDK（含 `platform-tools`、`build-tools`、`Android 13 (API 33)` 平台）
  - 至少一个可用的 Android AVD（模拟器）

### 桌面端运行 / Run Desktop

在仓库根目录下：

```bash
dotnet restore
dotnet run --project RegistrationEasy/RegistrationEasy.Desktop.csproj
```

或使用提供的脚本（推荐在 Git Bash / Linux / macOS 下）：

```bash
./build.sh 1    # 构建并运行桌面版
```

### Android 构建与调试 / Android Build & Run

在已安装 Android SDK 和 AVD 的前提下：

```bash
./build.sh 2    # 构建 Android APK（Release）
./build.sh 3    # 启动模拟器（如未启动）并安装 / 运行应用
```

APK 会生成在 `RegistrationEasy.Android/bin/Release/...` 下，具体路径可在脚本输出中看到。

### GitHub Actions 发布 / GitHub Actions Release

仓库内包含 `Build and Release` 工作流（`.github/workflows/release.yml`）：

- 当推送形如 `v1.0.0` 的 tag 时，自动：
  - 构建 Windows / macOS / Linux 三个平台的桌面发布包（zip）。
  - 构建并签名 Android APK（使用 GitHub Secrets 中的 keystore 配置）。
  - 创建 GitHub Release，并附上上述所有构建工件。

要让 Android 发布工作正常工作，需要在 GitHub 仓库中预先配置以下 Secrets：

- `ANDROID_KEYSTORE_BASE64`：Android keystore 文件的 base64 编码内容。
- `ANDROID_KEYSTORE_PASSWORD`：keystore 存储密码。
- `ANDROID_KEY_ALIAS`：用于签名的 key alias 名称。
- `ANDROID_KEY_PASSWORD`：对应 alias 的 key 密码。

推荐配置步骤（一次性操作）：

1. 在本机生成 keystore 的 base64 文本（示例）：
   ```bash
   base64 -w0 RegistrationEasy.Android/registrationeasy.keystore > keystore.b64.txt
   ```
2. 打开 GitHub 仓库 → `Settings` → `Secrets and variables` → `Actions`。
3. 依次创建以上四个 Secrets：
   - 将 `keystore.b64.txt` 的整行内容复制到 `ANDROID_KEYSTORE_BASE64`。
   - 根据本地 keystore 实际配置填写 `ANDROID_KEYSTORE_PASSWORD`、`ANDROID_KEY_ALIAS`、`ANDROID_KEY_PASSWORD`。
4. 推送 tag，例如：
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
   GitHub Actions 将自动构建桌面包和已签名的 Android APK 并创建 Release。

更详细的 CI 配置说明可参考方案文档第 8 章。

## 🛠️ 集成与适配 / Integration

本项目的核心逻辑（加密算法、机器码生成）非常独立，您可以轻松地：

1.  **直接集成**：将 `RegistrationEasy.Services` 下的代码复制到您的 .NET 项目中。
2.  **跨语言移植**：参考源码，将其适配到 **Java**, **Python**, **C++**, **Go** 等其他语言。加密算法使用的是标准的 AES，通用性极强。

## 📄 开源协议 / License

本项目采用 **MIT License** 授权。这意味着您可以：

- ✅ 商业使用
- ✅ 修改代码
- ✅ 分发代码
- ✅ 私有使用

```text
MIT License

Copyright (c) 2024 RegistrationEasy Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## 🤝 联系与支持 / Contact & Support

如有功能建议、使用问题，或者想了解更多技术细节，欢迎访问我的博客：

👉 **[80fafa.com](https://80fafa.com)**

---

_让软件授权变得简单。_
