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
Kodu seç  →  Soruyu yaz  →  Enter  →  Yanıtı al
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
| Model Name | Aktif model adı | `codellama`, `qwen2.5-coder:7b` |
| API Key | Varsa API anahtarı, yoksa boş bırakın | `sk-xxxx` veya boş |
| Max Tokens | Maksimum token sayısı (default: 4096) | `4096` |
| Temperature | Yanıt yaratıcılığı (0.0–1.0) | `0.2` |
| Timeout | HTTP istek zaman aşımı (saniye) | `60` |
| **Model Profiles** | Birden fazla model tanımlama — `;` veya her satıra bir model | `gpt-oss-120b;gpt-oss:20b` |

### Popüler LLM Yapılandırmaları

<details>
<summary><b>🦙 Ollama</b></summary>

```
Endpoint URL : http://localhost:11434/v1
Model Name   : codellama
API Key      : (boş)
```

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
</details>

<details>
<summary><b>🔗 LiteLLM / Proxy</b></summary>

```
Endpoint URL : https://your-litellm-server/v1
Model Name   : Qwen3-Coder-30B-A3B-Instruct
API Key      : sk-xxxx  (sk- ile başlamalı)
```
</details>

---

## 📖 Kullanım

### Arayüz (v1.7.0)

```
┌──────────────────────────────────────────────────┐
│  ● CodeLens AI        [gpt-oss-120b ▾]  ⚙  ↺    │  ← Model dropdown + ayar
├──────────────────────────────────────────────────┤
│                                                  │
│  ┌──────────────────────────────────────────┐    │
│  │ public int Add(int a, int b) {  (4 satır)│    │  ← Kod önizleme
│  └──────────────────────────────────────────┘    │
│                       Bu kodda hata var mı?  ●   │  ← Kullanıcı mesajı
│                                                  │
│  ● ─────────────────────────────────────────     │
│    Evet — return a - b yerine a + b olmali.      │  ← AI yanıtı
│                                    [Kopyala]     │
│  ─────────────────────────────────────────────   │
│  Model degistirildi: gpt-oss:20b  (italik)       │  ← Bilgi mesajı
│                                                  │
├──────────────────────────────────────────────────┤
│  Sorunuzu yazin… (Enter)                  [↑]    │
└──────────────────────────────────────────────────┘
```

### Yöntem 1 — Sağ Tık (Önerilen)

1. Editörde kodu **seçin**
2. Sağ tıklayın → **CodeLens AI: Analyze Selection**
3. Kod önizleme otomatik yüklenir (ilk satır + satır sayısı gösterilir)
4. Sorunuzu yazın → **Enter** (ya da `↑` butonu)

### Yöntem 2 — Tools Menüsü

**Tools → Analyze with CodeLens AI**

### Model Değiştirme

1. **Tools → Options → CodeLens AI → Model Profiles** alanına modelleri tanımlayın:
   ```
   gpt-oss-120b
   gpt-oss:20b
   qwen2.5-coder:7b
   ```
2. Panel başlığındaki **model adı butonuna** tıklayın → dropdown açılır → istediğiniz modeli seçin

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
├── Version.props                       # Tüm versiyon numaraları tek yerden
├── VSPackage.cs                        # AsyncPackage giriş noktası
├── Commands/
│   └── AnalyzeCommand.cs               # Editör komutu — seçili metni alır
├── ToolWindows/
│   ├── ChatWindow.cs                   # ToolWindowPane wrapper
│   ├── ChatWindowControl.xaml          # WPF sohbet arayüzü
│   └── ChatWindowControl.xaml.cs       # UI logic — dropdown, bubbles, async send
├── Options/
│   └── LlmOptions.cs                   # VS Settings Store — ModelProfiles dahil
├── Services/
│   ├── ILlmHost.cs                     # Arayüz
│   └── LlmService.cs                   # HTTP → /v1/chat/completions
└── Models/
    └── ChatMessage.cs                  # DataContractJsonSerializer modelleri
```

---

## 🔧 Kaynaktan Derleme

```powershell
git clone https://github.com/AlperenCK/CodeLensAI.git
cd CodeLensAI

# Newtonsoft.Json'ı packages klasörüne indir
& "$env:USERPROFILE\nuget.exe" restore "CodeLensAI\packages.config" -PackagesDirectory ".\packages\" -SolutionDirectory "."

# Build
msbuild CodeLensAI\CodeLensAI.csproj /p:Configuration=Release /v:minimal

# Çıktı: CodeLensAI\bin\Release\CodeLensAI.vsix
```

> **Not:** VS 2022 içinden F5/Build yaparsanız `packages.config` restore otomatik çalışır.

---

## 📋 Sürüm Geçmişi

| Versiyon | Değişiklik |
|---|---|
| v1.7.0 | Model dropdown, Enter ile gönder, temiz hata mesajları, kod önizleme satır sayısı |
| v1.6.0 | Sohbet baloncuğu UI — konuşma geçmişi, kod önizleme, model pill |
| v1.5.0 | Yeni logo |
| v1.4.0 | CI/CD pipeline stabil, otomatik VSIX release |
| v1.2.0 | VSIX build düzeltildi (non-SDK csproj) |
| v1.0.0 | İlk sürüm |

---

## 🤝 Katkı

1. Fork'layın
2. Feature branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Commit'leyin ve PR açın

---

## 📜 Lisans

MIT © 2026 CodeLensAI Team
