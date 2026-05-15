# 🎥 OBS Stream Deck (.NET MAUI)

A professional-grade, cross-platform remote control system for **OBS Studio**. This project replaces expensive hardware with a custom **.NET MAUI** mobile application and a high-performance **.NET 9 Web API** bridge.

---

## 🛠️ System Architecture

The system operates as a distributed network architecture:
1. **The Controller (.NET MAUI):** An Android/Windows app providing a "Dark Classical" UI for live control.
2. **The Bridge (.NET API):** A Windows Service that listens for LAN commands and communicates with OBS via WebSocket 5.x.
3. **The Target (OBS Studio):** The media engine that executes scene switches and source automation.

---

## ✨ Features

### 1. Smart Automation & Game Hooking
* **CS2 Auto-Targeting:** When switching to the "Gameplay" scene, the API automatically hooks the `[cs2.exe]` window for the "CS" source.
* **Context-Aware Logic:** Executes specific source adjustments based on the active scene.

### 2. Cross-Platform Mobile UI
* **Real-Time Status:** Visual "Live" indicators and connection heartbeat.
* **Dynamic Scene Sync:** Automatically fetches your latest OBS scenes directly to your phone.
* **Minimalist Aesthetic:** Designed with a Teal-and-Charcoal theme for high-stress environments.

### 3. Professional Infrastructure
* **Windows Service Integration:** The API runs as `OBSDeckService` in the background (starts on boot).
* **LAN Connectivity:** Binds to `0.0.0.0:5018` for seamless control from any device on your Wi-Fi.
* **Low Latency:** Built on OBS WebSocket 5.x for near-instant execution.

---

## 📂 Project Structure

```text
.
├── Android/
│   └── StreamDeckApp      # .NET MAUI Client (Android/iOS/Windows)
├── Api/
│   └── StreamDeckApi      # ASP.NET Core Web API (The Bridge)
├── .gitignore             # Standard .NET ignores
├── LICENSE                # Apache 2.0 License
└── README.md              # Documentation
🚀 Getting Started
Prerequisites
OBS Studio with WebSocket 5.x enabled (Tools > WebSocket Server Settings).

.NET 9 SDK installed on your development machine.

Installation
Clone the repo:

Bash
git clone [https://github.com/qraza1100/OBS-Stream-Deck.git](https://github.com/qraza1100/OBS-Stream-Deck.git)
Configure the API:
Update the OBS password and IP in Api/StreamDeckApi/appsettings.json.

Deploy the Service:
Publish the API and register it as a Windows Service.

Launch the App:
Open the MAUI solution in Visual Studio and deploy to your Android device.

⚖️ License
Distributed under the Apache License 2.0. See LICENSE for more information.


---

### ⚖️ LICENSE (Apache 2.0)
Create a file named `LICENSE` in your root directory and paste this:

```text
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   Copyright 2026 qraza1100

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
