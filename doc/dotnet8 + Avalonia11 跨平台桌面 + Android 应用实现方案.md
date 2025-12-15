# dotnet8 + Avalonia11 跨平台桌面 + Android 应用实现方案

> 以 RegistrationEasy 项目为例的实践总结

---

## 1. 整体目标与方案概览

- 使用 `.NET 8` + `Avalonia 11` 搭建一个统一 UI 的跨平台应用：
  - 桌面端：Windows / Linux / macOS
  - 移动端：Android
- 采用 MVVM 模式（`CommunityToolkit.Mvvm`）构建 UI 与业务逻辑。
- 通过 `build.sh` 脚本封装构建与部署流程。
- 使用 GitHub Actions 实现持续集成（构建 / 测试 / 产出包）。

---

## 2. 环境搭建与依赖

### 2.1 操作系统建议

- 开发环境：
  - Windows 10/11（本项目实际环境）
  - 或 Linux / macOS（均可作为开发端）
- 目标运行环境：
  - Windows / Linux / macOS 桌面
  - Android 8+ 设备或模拟器

### 2.2 安装 .NET SDK

- 推荐版本：
  - `.NET SDK 8.x`（例如：`8.0.414` 等 LTS 版本）
- 下载地址：
  - https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- 安装完成后验证：
  ```bash
  dotnet --info
  ```

### 2.3 开发工具（IDE / 编辑器）

- Visual Studio 2022（Windows）
  - 安装工作负载：
    - `.NET 桌面开发`
    - `.NET 跨平台开发`
    - `使用 .NET 的移动开发`（包含 Android 工具与 SDK）
- JetBrains Rider / VS Code（跨平台）
  - 搭配 .NET SDK 与 C# 插件即可。

### 2.4 Avalonia 相关依赖

- 核心包（示例版本均为 `11.3.9`，需保持一致）：
  - `Avalonia`
  - `Avalonia.Desktop`
  - `Avalonia.Android`
  - `Avalonia.Themes.Fluent`
  - `Avalonia.Fonts.Inter`
- MVVM：
  - `CommunityToolkit.Mvvm`（例如 `8.2.1`）

示例（共享项目 `RegistrationEasy.Common` 的依赖）：

```xml
<ItemGroup>
  <PackageReference Include="Avalonia" Version="11.3.9" />
  <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.9" />
  <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.9" />
  <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
</ItemGroup>
```

> 重点：**所有 Avalonia 相关包的版本必须统一**，否则容易出现编译 / 运行时不一致的问题（例如 XAML 预编译失败）。

### 2.5 Android 开发环境与模拟器

- 安装 Android Studio（用于 SDK 管理与模拟器管理）。
- 安装以下组件：
  - Android SDK Platform（目标 API 级别，如 33）
  - Android SDK Build-Tools
  - Android Emulator
  - 相应的系统镜像（如：Android 13 x86_64）
- 配置环境变量（Windows 示例）：
  - `ANDROID_SDK_ROOT` 指向 Android SDK 根目录
  - 将 `platform-tools`（包含 `adb`）加入 `PATH`
- 验证：
  ```bash
  adb version
  emulator -list-avds
  ```

---

## 3. 项目组织结构

以 `RegistrationEasy` 为例的推荐结构：

```text
RegistrationEasy/
  RegistrationEasy.Common/      # 共享业务逻辑 + 视图 + ViewModel
  RegistrationEasy.Desktop/     # 桌面端启动项目（Windows/Linux/macOS）
  RegistrationEasy.Android/     # Android 启动项目
  build.sh                      # 统一构建与部署脚本（*nix/bash 环境）
```

### 3.1 共享项目 `RegistrationEasy.Common`

- 目标框架：`net8.0`
- 主要职责：
  - 定义 `App`（Application 入口）
  - 注册主窗口 / 页面
  - 定义所有 XAML 视图 & ViewModel
  - 包含共享资源（图标、样式等）
- 特点：
  - 仅依赖 Avalonia + MVVM，不直接引用平台特有 API。
- 典型项目文件（简化示例）：

  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net8.0</TargetFramework>
      <Nullable>enable</Nullable>
      <LangVersion>latest</LangVersion>
      <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
    </PropertyGroup>

    <ItemGroup>
      <PackageReference Include="Avalonia" Version="11.3.9" />
      <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.9" />
      <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.9" />
      <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.1" />
    </ItemGroup>

    <ItemGroup>
      <AvaloniaResource Include="Assets\**" />
    </ItemGroup>
  </Project>
  ```

> 这里共享项目使用 **纯 C# 初始化 Application** 的方式，避免 XAML 入口文件带来的编译冲突（例如 Duplicate x:Class）。

### 3.2 桌面项目 `RegistrationEasy.Desktop`

- 目标框架：`net8.0`
- 主要职责：
  - 提供 `Main` 入口，调用 Avalonia AppBuilder 启动应用。
  - 配置平台相关选项（如窗口图标、窗口大小等）。
- 依赖：
  - 引用 `RegistrationEasy.Common` 项目。
  - 引用 `Avalonia.Desktop`。

### 3.3 Android 项目 `RegistrationEasy.Android`

- 目标框架：`net8.0-android`
- 主要职责：
  - 提供 `MainActivity` 作为 Android 入口。
  - 调用 Avalonia Android 入口启动共享 `App`。
- 依赖：
  - 引用 `RegistrationEasy.Common`。
  - 引用 `Avalonia.Android`。

示例关键点：

- `MainActivity` 继承自 Avalonia 的 Activity 基类，内部调用 `AppBuilder`，指向共享项目的 `App` 类型。
- `Resources/values/styles.xml` 需使用兼容的 AppCompat 主题（下文有坑点说明）。

---

## 4. 项目功能概览（示例：RegistrationEasy）

- 主界面包含：
  - 输入框：用于输入某种编码或数据。
  - `Verify code` 按钮：验证输入内容。
  - `DecodeResult` 区域：显示解析结果。
- 使用 MVVM 模式：
  - View：Avalonia XAML 页（例如 `MainView`）
  - ViewModel：`MainViewModel`，使用 `CommunityToolkit.Mvvm` 的 `ObservableObject` / `RelayCommand` 等。
- UI 设计目标：
  - 简洁、美观、大方。
  - 操作流程清晰：输入 → 验证 → 显示结果。

---

## 5. 构建步骤与流程分析

### 5.1 基本构建命令

- 还原依赖：
  ```bash
  dotnet restore
  ```
- 构建整个解决方案（Debug）：
  ```bash
  dotnet build
  ```
- 构建 Release：
  ```bash
  dotnet build -c Release
  ```

### 5.2 桌面端发布示例

- 发布 Windows 单文件（可选）：
  ```bash
  dotnet publish RegistrationEasy.Desktop -c Release -r win-x64 --self-contained false
  ```
- 发布 Linux：
  ```bash
  dotnet publish RegistrationEasy.Desktop -c Release -r linux-x64 --self-contained false
  ```
- 发布 macOS：
  ```bash
  dotnet publish RegistrationEasy.Desktop -c Release -r osx-x64 --self-contained false
  ```

> 以上均使用 Avalonia Desktop 后端，无需额外 runtime 依赖（除 .NET runtime）。

### 5.3 Android 构建与 APK 生成

- 构建 Debug APK（示例，命令可能根据项目名微调）：
  ```bash
  dotnet build RegistrationEasy.Android -c Release
  ```
- 生成可安装的 APK：
  ```bash
  dotnet publish RegistrationEasy.Android -c Release -f net8.0-android
  ```
- 发布后在 `bin/Release/net8.0-android/publish` 下可找到 `.apk` 文件。

---

## 6. `build.sh` 脚本功能与使用流程

### 6.1 脚本设计目标

- 用一个脚本统一管理：
  - 构建桌面版本（多平台）
  - 构建 Android APK
  - 自动部署到 Android 模拟器或真机
- 对开发者只暴露简单子命令，如：
  - `./build.sh 1` —— 构建桌面
  - `./build.sh 2` —— 构建 Android APK
  - `./build.sh 3` —— 构建并部署到模拟器

### 6.2 典型结构示例（伪代码）

```bash
#!/usr/bin/env bash
set -e

GREEN="\033[0;32m"
RED="\033[0;31m"
NC="\033[0m"

build_desktop() {
  echo -e "${GREEN}Building desktop targets...${NC}"
  dotnet publish RegistrationEasy.Desktop -c Release -r win-x64 --self-contained false
  # 可按需添加 linux / macOS
}

build_android() {
  echo -e "${GREEN}Building Android APK...${NC}"
  dotnet publish RegistrationEasy.Android -c Release -f net8.0-android
}

run_android() {
  echo -e "${GREEN}Checking for Android Emulator...${NC}"
  # 1. 检测 emulator 是否可用
  # 2. 启动指定 AVD（如果未启动）
  # 3. 使用 adb 安装 & 启动 APK
}

case "$1" in
  1) build_desktop ;;
  2) build_android ;;
  3) build_android && run_android ;;
  *) echo "Usage: $0 {1|2|3}" ;;
esac
```

### 6.3 使用流程示例

1. 确认已安装 .NET 8、Android SDK、adb 等。
2. 在项目根目录给予脚本执行权限：
   ```bash
   chmod +x build.sh
   ```
3. 构建桌面：
   ```bash
   ./build.sh 1
   ```
4. 构建 Android APK：
   ```bash
   ./build.sh 2
   ```
5. 构建并尝试安装到模拟器：
   ```bash
   ./build.sh 3
   ```

> 若 `./build.sh 3` 报错，多数与 **模拟器/adb 环境** 有关，而非项目代码本身（例如：AVD 未创建、模拟器未启动、设备未连接等）。

---

## 7. 实际踩坑与解决方案

本节结合 RegistrationEasy 项目实践中的典型问题。

### 7.1 AVLN2002 Duplicate x:Class 错误

**现象：**

- 编译时出现类似错误：

  > AVLN2002 Duplicate x:Class ...

**原因：**

- Avalonia 的 XAML 编译器在处理 `App.axaml` / `Window.axaml` 等时，发现同一个 `x:Class` 对应多个定义。
- 在本项目中，主要是：
  - 既有 XAML 定义的 `App.axaml`，又有纯 C# 定义的 `App` 类型。
  - 或者 `.csproj` 中对 Avalonia 资源的配置重复、过度复杂。

**解决思路：**

1. **选择单一方式初始化 Application**：
   - 在 RegistrationEasy 中，最终采用 **纯 C# 初始化 `App`**：
     - 保留 `App.axaml.cs`（或纯 C# `App` 类）
     - 删除 `App.axaml` XAML 文件，避免 XAML 编译器处理重复类。
2. **简化 `RegistrationEasy.Common.csproj` 配置**：
   - 保留必须的 `AvaloniaResource` 项，如：
     ```xml
     <ItemGroup>
       <AvaloniaResource Include="Assets\**" />
     </ItemGroup>
     ```
   - 移除多余的 `Compile` / `AvaloniaResource` 手动配置，让 SDK 默认规则接管。
3. **确保每个 XAML 文件的 `x:Class` 唯一且不与 C# 手写类冲突**。

### 7.2 “No precompiled XAML found” 崩溃

**现象：**

- 运行时或启动时出现类似错误：
  > No precompiled XAML found for ...

**常见原因：**

- Avalonia 预编译 XAML 功能未正常工作，可能原因包括：
  - Avalonia 包版本不一致（核心库、主题库、字体库版本不统一）。
  - `.csproj` 配置异常，导致未触发 XAML 编译。
  - 资源路径或命名空间配置有误。

**解决方案：**

1. **统一版本**：
   - 将所有 Avalonia 包版本统一为 `11.3.9`（或其他同版本）。
2. **使用标准 `.csproj` 模板**：
   - 避免大量手工配置 `AvaloniaResource`、`Compile` 等，除非确有必要。
   - 使用官方模板生成的项目作为参考，逐项对比。
3. **清理旧 XAML 文件**：
   - 若已改用纯 C# 初始化 `App`，则删除对应的 `App.axaml`，避免半残 XAML 入口。
4. **确保 `EnableDefaultAvaloniaItems` 等保持默认或与模板一致**（如未显式关闭）。

### 7.3 Android 主题错误与样式问题

**现象：**

- Android 项目在构建或运行时出现主题相关错误：
  - 如找不到某主题、样式不兼容、崩溃等。

**本项目中的关键修改：**

- 编辑 `RegistrationEasy.Android/Resources/values/styles.xml`：
  - 使用 `Theme.AppCompat` 系列主题，确保与 Avalonia.Android 兼容。

示例：

```xml
<resources>
  <style name="MyTheme.NoActionBar" parent="Theme.AppCompat.Light.NoActionBar">
    <item name="windowNoTitle">true</item>
    <item name="windowActionBar">false</item>
    <item name="android:windowFullscreen">true</item>
    <item name="android:windowContentOverlay">@null</item>
  </style>
</resources>
```

**注意：**

- 若继承不兼容的主题（如某些 Material3 变体），可能导致 Avalonia Host 初始化失败。
- 保持主题配置与 Avalonia 官方 Android 模板接近是最稳妥做法。

### 7.4 `build.sh 3` 运行失败的原因分析

**现象：**

- `./build.sh 3` 在某些环境下失败，但构建本身通过。
- 日志显示问题集中在 `emulator` 或 `adb` 阶段。

**结论：**

- 失败根因不在项目代码，而在 **Android 模拟器环境**：
  - 未创建 AVD
  - 模拟器未启动
  - `adb` 未识别到任何设备
- 验证方式：
  ```bash
  adb devices
  emulator -list-avds
  ```

**解决建议：**

- 使用 Android Studio 创建并手动启动一个 AVD。
- 确保 `adb` 能列出该设备后，再执行 `./build.sh 3`。
- 若仍有问题，可在脚本中增加更多日志输出（如 `adb devices` 结果）。

---

## 8. GitHub Actions 持续集成方案

### 8.1 目标

- 在代码推送或 PR 时自动执行：
  - 还原依赖
  - 编译项目（至少 Debug / Release 之一）
  - 可选：运行测试
  - 可选：产出可下载的构建工件（如桌面可执行文件 / APK）

### 8.2 基本工作流示例（伪代码）

```yaml
name: CI

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 8
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "8.0.x"

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build -c Release

      # 可选：运行测试
      # - name: Test
      #   run: dotnet test

      # 可选：发布桌面或 Android
      # - name: Publish Desktop
      #   run: dotnet publish RegistrationEasy.Desktop -c Release -r win-x64 --self-contained false
```

> 说明：
>
> - Android 构建可在 Linux Runner 上使用 `dotnet publish` 完成，但若要运行模拟器测试则需更复杂的环境配置。
> - 实际项目中可以根据需要扩展为多个 Job，分别负责桌面和 Android。

### 8.3 与本地 `build.sh` 的关系

- CI 中可直接调用 `dotnet` 命令；也可在 Runner 上调用 `./build.sh`（前提是 Runner 的环境支持 bash/Android SDK）。
- 建议：
  - **本地开发**：以 `build.sh` 为主，快速统一构建体验。
  - **CI 中**：直接使用 `dotnet` 命令，保持配置可读性与可控性，必要时参考 `build.sh` 中的命令。

---

## 9. 当前项目状态与后续建议

### 9.1 当前技术状态（RegistrationEasy）

- `RegistrationEasy.Common`：
  - 已解决 AVLN2002 Duplicate x:Class 问题。
  - 已解决 “No precompiled XAML” 崩溃的根因。
  - 使用纯 C# 初始化 `App`，XAML 资源配置精简且稳定。
- Android 项目：
  - 能够成功构建 APK。
  - 样式主题问题已通过 `Theme.AppCompat.Light.NoActionBar` 方案解决。
- `build.sh`：
  - 能正常完成构建流程。
  - `./build.sh 3` 失败时，主要是由于本机模拟器环境问题，而非项目本身。

### 9.2 实践经验总结

- **优先保持模板一致性**：从 Avalonia 官方模板创建项目，再逐步修改，是避免配置坑的关键。
- **共享项目尽量纯净**：`RegistrationEasy.Common` 只做 UI 与业务，不掺杂任何具体平台依赖。
- **XAML 入口统一策略**：要么使用 XAML `App.axaml`，要么使用纯 C# `App`，**不要混搭**。
- **版本统一**：所有 Avalonia 包、.NET SDK 均应统一在兼容版本范围内。
- **脚本化构建**：`build.sh` 把复杂的构建和部署命令收拢成简单入口，大幅降低日常开发成本。
- **CI 与本地一致性**：GitHub Actions 流程应尽量模拟本地的构建命令，减少“只在我电脑上能跑”的情况。

---

## 10. 可以继续优化的方向

- 为桌面和 Android 分别增加 UI 自动化测试 / 单元测试。
- 在 GitHub Actions 中增加：
  - 产出 APK / 桌面包作为 artifact。
  - 针对关键平台（如 Windows / Linux）进行发布前构建验证。
- 优化 UI 布局与视觉设计（例如 `Verify code` 按钮位置、`DecodeResult` 区域布局等），确保在手机与桌面上均有良好体验。

---

如需，我可以基于本 MD 文档，再输出一个适合直接放到仓库 `README` 的精简版本，或者按你的要求拆成「环境搭建篇」「项目结构篇」「CI 篇」等子文档。
