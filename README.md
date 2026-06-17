# 🤖 CodeLens AI

> **Local LLM Code Analysis Extension for Visual Studio 2022**

[![Build Status](https://github.com/your-org/CodeLensAI/actions/workflows/build.yml/badge.svg)](https://github.com/your-org/CodeLensAI/actions)
[![VS Marketplace](https://img.shields.io/badge/VS%20Marketplace-CodeLens%20AI-blue)](https://marketplace.visualstudio.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)

Connect Visual Studio 2022 to your **local LLM** (Ollama, LM Studio, llama.cpp, or any OpenAI-compatible API), select code, describe your problem, and get instant AI-powered analysis — without leaving the IDE and **without sending your code to the cloud**.

---

## ✨ Features

| Feature | Description |
|---|---|
| 🔌 **Local LLM Support** | Works with Ollama, LM Studio, or any OpenAI-compatible endpoint |
| 📝 **Editor Integration** | Select code → right-click → "Analyze with CodeLens AI" |
| 💬 **Chat Panel** | Dedicated tool window with code + question inputs |
| ⚙️ **Persistent Settings** | Endpoint URL, model name, API key saved to VS Settings Store |
| 🔒 **Privacy First** | Your code never leaves your machine |
| ⚡ **Fast** | Non-blocking async calls; cancel any time |

---

## 🚀 Getting Started

### Prerequisites

- Visual Studio 2022 (v17.0 or higher)
- .NET Framework 4.7.2+
- A running local LLM server:
  - [Ollama](https://ollama.ai) — `ollama serve` (default: `http://localhost:11434`)
  - [LM Studio](https://lmstudio.ai) — Enable local server (default: `http://localhost:1234`)
  - Any OpenAI-compatible API

### Installation

1. Download `CodeLensAI.vsix` from [Releases](https://github.com/your-org/CodeLensAI/releases)
2. Close Visual Studio
3. Double-click the `.vsix` file and follow the installer
4. Reopen Visual Studio 2022

### Configuration

1. Go to **Tools → Options → CodeLens AI → LLM Connection**
2. Set your **Endpoint URL** (e.g. `http://localhost:11434/v1`)
3. Set your **Model Name** (e.g. `codellama`, `deepseek-coder:6.7b`)
4. Click **OK**

### Usage

1. **Open any code file** in Visual Studio
2. **Select the code** you want to analyze
3. Go to **Tools → Analyze with CodeLens AI** (or use the editor context menu)
4. The **CodeLens AI** panel opens with your selected code pre-filled
5. **Type your question** (e.g. "What does this do?", "Fix the bug", "Add unit tests")
6. Press **▶ Analyze** or `Ctrl+Enter`

---

## 🏗️ Architecture

```
CodeLensAI/
├── VSPackage.cs                  # AsyncPackage entry point
├── Commands/
│   └── AnalyzeCommand.cs         # Editor command (grabs selected text)
├── ToolWindows/
│   ├── ChatWindow.cs             # ToolWindowPane host
│   ├── ChatWindowControl.xaml    # WPF UI
│   └── ChatWindowControl.xaml.cs # UI logic + LLM call orchestration
├── Options/
│   └── LlmOptions.cs             # DialogPage (VS Settings Store)
├── Services/
│   └── LlmService.cs             # HttpClient → OpenAI-compat /v1/chat/completions
└── Models/
    └── ChatMessage.cs            # Request/response models (Newtonsoft.Json)
```

### Supported LLM Models (tested)

| Model | Provider | Recommended for |
|---|---|---|
| `codellama` | Ollama | General code analysis |
| `deepseek-coder:6.7b` | Ollama | Code generation & fixes |
| `qwen2.5-coder:7b` | Ollama | Multi-language support |
| `mistral` | Ollama | Explanations & docs |
| Any OpenAI-compat model | LM Studio | Flexible |

---

## 🔧 Building from Source

```bash
git clone https://github.com/your-org/CodeLensAI.git
cd CodeLensAI
nuget restore CodeLensAI.sln
msbuild CodeLensAI.sln /p:Configuration=Release
# VSIX output: CodeLensAI/bin/Release/CodeLensAI.vsix
```

### CI/CD

Push to `main` → GitHub Actions builds, runs Roslyn code quality analysis, and uploads the VSIX artifact.
Tag `v*.*.*` → additionally creates a GitHub Release with the VSIX attached.

---

## 🤝 Contributing

1. Fork the repo
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit with a descriptive message
4. Push and open a Pull Request

All PRs go through the automated build + Roslyn analysis gate.

---

## 📜 License

MIT © 2026 CodeLensAI Team
