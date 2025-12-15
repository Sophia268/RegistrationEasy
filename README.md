# RegistrationEasy

**RegistrationEasy** 这是一个软件注册验证的例子。 案例是跨平台（Windows / macOS / Linux）的。
使用 “帮您发发” （80fafa）网站生成注册码， 让软件变现更加容易； 旨在为您的软件提供简单、易用且安全的授权收费的解决方案。

案例完全开源，采用最宽松的 [MIT License](LICENSE)，您可以自由地将其集成到商业软件中，或将其移植到其他编程语言。

---

## 🌍 跨平台支持 / Cross-Platform

基于 **Avalonia UI** 和 **.NET 8** 构建，完美支持：

- **Windows** (x64, x86)
- **macOS** (Apple Silicon, Intel)
- **Linux** (x64, ARM)

## ✨ 主要功能 / Features

- **机器码生成**：根据硬件信息（CPU、主板、硬盘等）生成唯一的机器特征码。
- **注册码信息**：验证基于 AES 加密的注册码，确保授权安全。
- **有效期信息**：支持永久授权或按时长（月/年）授权。
- **额度信息**：注册码含有额度信息。
- **开箱即用**：无需搭建服务器，无需集成支付工具，让用户更好的购买您的软件。

## 🚀 快速开始 / Quick Start

### 运行 / Run

确保已安装 [.NET 8 SDK](https://dotnet.microsoft.com/download)，然后在项目根目录下运行：

```bash
cd RegistrationEasy
dotnet run
```

### 构建 / Build

使用提供的脚本一键构建和运行：

```bash
# Linux / macOS / Git Bash
./build.sh
```

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
