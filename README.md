<div align="center">

<img src="https://raw.githubusercontent.com/AlperenCK/CodeLensAI/main/CodeLensAI/Resources/icon.png" width="80" height="80" alt="CodeLens AI Logo"/>

# CodeLens AI

**Visual Studio 2022 için Yerel LLM Kod Asistanı**

Kodunuzu buluta göndermeden, kendi LLM'inizle analiz edin.

[![Build](https://github.com/AlperenCK/CodeLensAI/actions/workflows/build.yml/badge.svg)](https://github.com/AlperenCK/CodeLensAI/actions)
[![Release](https://img.shields.io/github/v/release/AlperenCK/CodeLensAI)](https://github.com/AlperenCK/CodeLensAI/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)
[![VS 2022](https://img.shields.io/badge/Visual%20Studio-2022-purple)](https://visualstudio.microsoft.com/)

</div>

---

## ✨ Ne Yapar?

Visual Studio 2022'ye entegre olan CodeLens AI, editördeki seçili kodu alıp yerel LLM'inize göndererek anında analiz, hata tespiti, refactoring önerileri ve açıklama üretir — **internet bağlantısı ve bulut servisi gerekmez.**

```
Kodu seç  →  Soruyu yaz  →  Analiz Et  →  Yanıtı al
```

---

## 🚀 Kurulum

### Gereksinimler

| Gereksinim | Versiyon |
|---|---|
| Visual Studio | 2022 (v17.0+) Community / Professional / Enterprise |
| Workload | Visual Studio extension development |
| .NET Framework | 4.7.2+ |
| LLM Sunucu | Ollama, LM Studio veya OpenAI-uyumlu herhangi bir API |

### Adımlar

1. [Releases](https://github.com/AlperenCK/CodeLensAI/releases/latest) sayfasından `CodeLensAI.vsix` dosyasını indirin
2. Visual Studio'yu **kapatın**
3. `CodeLensAI.vsix` dosyasına çift tıklayın ve yükleyin
4. Visual Studio'yu tekrar açın

---

## ⚙️ Yapılandırma

**Tools → Options → CodeLens AI → LLM Connection**

| Alan | Açıklama | Örnek |
|---|---|---|
| Endpoint URL | LLM sunucunuzun base URL'i (`/chat/completions` **eklemeyin**) | `http://localhost:11434/v1` |
| Model Name | Kullanmak istediğiniz model adı | `codellama`, `qwen2.5-coder:7b` |
| API Key | Varsa API anahtarı, yoksa boş bırakın | `sk-xxxx` veya boş |
| Max Tokens | Maksimum token sayısı | `2048` |
| Temperature | Yanıt yaratıcılığı (0.0–1.0, kod için düşük önerilir) | `0.2` |
| Timeout | HTTP istek zaman aşımı (saniye) | `60` |

### Popüler LLM Yapılandırmaları

<details>
<summary><b>🦙 Ollama</b></summary>

```
Endpoint URL : http://localhost:11434/v1
Model Name   : codellama          (veya: llama3, deepseek-coder, qwen2.5-coder:7b)
API Key      : (boş)
```

Ollama'da model yüklemek için:
```bash
ollama pull codellama
ollama pull qwen2.5-coder:7b
```
</details>

<details>
<summary><b>🎬 LM Studio</b></summary>

```
Endpoint URL : http://localhost:1234/v1
Model Name   : (LM Studio'da yüklü modelin adı)
API Key      : (boş)
```

LM Studio'da: **Local Server** sekmesine geçip sunucuyu başlatın.
</details>

<details>
<summary><b>🔗 LiteLLM / Proxy</b></summary>

```
Endpoint URL : https://your-litellm-server/v1
Model Name   : Qwen3-Coder-30B-A3B-Instruct  (veya proxy'deki model adı)
API Key      : sk-xxxx  (LiteLLM virtual key — sk- ile başlamalı)
```
</details>

---

## 📖 Kullanım

### Arayüz

v1.6.0 ile birlikte CodeLens AI modern bir **sohbet baloncuğu arayüzüne** kavuştu:

```
┌─────────────────────────────────────────────┐
│  ● CodeLens AI          qwen3-coder  ⚙  ↺  │
├─────────────────────────────────────────────┤
│                                             │
│         ●                                  │
│     CodeLens AI                            │
│   Kodu seçin, sorunuzu yazın.              │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │ public int Add(int a, int b) {       │  │  ← Kod önizleme
│  │   return a - b; // bug               │  │
│  └──────────────────────────────────────┘  │
│                         Bu kodda hata var mı? │  ← Kullanıcı
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │ Evet — `a - b` yerine `a + b` olmalı │  │  ← AI yanıtı
│  │                          [Kopyala]   │  │
│  └──────────────────────────────────────┘  │
│                                             │
├─────────────────────────────────────────────┤
│  Sorunuzu yazın… (Ctrl+Enter)     [↑]      │
└─────────────────────────────────────────────┘
```

### Yöntem 1 — Sağ Tık (Önerilen)

1. Editörde analiz etmek istediğiniz kodu **seçin**
2. Sağ tıklayın → **CodeLens AI: Analyze Selection**
3. Kod önizleme balonu otomatik dolar
4. Sorunuzu yazın → `Ctrl+Enter` veya `↑`

### Yöntem 2 — Tools Menüsü

**Tools → Analyze with CodeLens AI**

### Yöntem 3 — Manuel

Paneli açın, sorunuzu doğrudan yazın (kod olmadan da kullanabilirsiniz).

---

## 💡 Örnek Kullanım Senaryoları

| Senaryo | Soru Örneği |
|---|---|
| Hata tespiti | `Bu kodda bug var mı?` |
| Kod açıklama | `Bu metod ne yapıyor, satır satır açıkla` |
| Refactoring | `Bu kodu daha okunabilir hale getir` |
| Unit test | `Bu metod için xUnit test yaz` |
| Performans | `Bu sorguyu nasıl optimize ederim?` |
| Güvenlik | `Bu kodda güvenlik açığı var mı?` |
| Çeviri | `Bu kodu C#'dan Python'a çevir` |

---

## 🏗️ Mimari

```
CodeLensAI/
├── VSPackage.cs                    # AsyncPackage giriş noktası
├── Commands/
│   └── AnalyzeCommand.cs           # Editör komutu — seçili metni alır
├── ToolWindows/
│   ├── ChatWindow.cs               # ToolWindowPane wrapper
│   ├── ChatWindowControl.xaml      # WPF sohbet arayüzü (v1.6.0)
│   └── ChatWindowControl.xaml.cs  # UI logic + async LLM çağrısı
├── Options/
│   └── LlmOptions.cs               # VS Settings Store kalıcı ayarlar
├── Services/
│   ├── ILlmHost.cs                 # Arayüz (test edilebilirlik)
│   └── LlmService.cs               # HTTP istemcisi → /v1/chat/completions
└── Models/
    └── ChatMessage.cs              # Request/response modelleri
```

---

## 🔧 Kaynaktan Derleme

```powershell
git clone https://github.com/AlperenCK/CodeLensAI.git
cd CodeLensAI
nuget restore -PackagesDirectory ".\packages\" CodeLensAI.sln
msbuild CodeLensAI\CodeLensAI.csproj /p:Configuration=Release /v:minimal
# Çıktı: CodeLensAI\bin\Release\CodeLensAI.vsix
```

**Gereksinimler:**
- Visual Studio 2022 (Visual Studio extension development workload)
- NuGet CLI

---

## 📋 Sürüm Geçmişi

| Versiyon | Değişiklik |
|---|---|
| v1.6.0 | Sohbet baloncuğu UI redesign — konuşma geçmişi, kod önizleme, model pill |
| v1.5.0 | Yeni logo tasarımı |
| v1.4.0 | CI/CD pipeline stabil, VSIX artifact otomatik release |
| v1.2.0 | VSIX build düzeltildi (non-SDK csproj, VS 2022 Pro/Ent/Com) |
| v1.1.0 | Constructor injection, WPF compat, unit test altyapısı |
| v1.0.0 | İlk sürüm |

---

## 🤝 Katkı

1. Fork'layın
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Commit'leyin
4. PR açın

---

## 📜 Lisans

MIT © 2026 CodeLensAI Team
