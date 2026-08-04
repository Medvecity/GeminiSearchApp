# Gemini Search App 🚀🧠

A lightweight, hotkey-activated Windows search overlay powered by Google's Gemini AI. Built with C# WPF and .NET 8, this tool allows you to search your PC, browse the web, and interact with artificial intelligence seamlessly from anywhere on your desktop.

## ✨ Features

*   **Global Hotkey (`Alt + Space`)**: Instantly bring up the search bar, no matter what app you are currently using.
*   **Integrated Gemini AI**: Ask questions and get intelligent, detailed answers directly on your screen.
*   **AI PC Control**: Let Gemini control your computer! The AI can securely open/close applications, mute volume, or shut down your PC via bracketed commands.
*   **Lightning Fast File Search**: Instantly find local apps and files using the native Windows Indexing Service.
*   **Multi-language Support**: The UI and AI instructions automatically adapt to English, German, French, or Hungarian based on your installation choice.

### 🛡️ Windows SmartScreen/Defender Warning
Since this is a brand new, independent open-source project, Windows SmartScreen might flag the installer as an "unrecognized app". **This is completely normal.** 
To install the app, simply click **"More info"** on the blue popup, and then click **"Run anyway"**. The complete source code is fully transparent and available here on GitHub for anyone to review!

## 📥 Installation

1. Go to the [Releases](../../releases) page.
2. Download the latest `GeminiInstaller.exe` file.
3. 
4. Run the installer and choose your preferred language.
5. On first run, the app will ask for a **Free Google Gemini API Key**. You can get one with a single click [here](https://aistudio.google.com/app/apikey).

## 🛡️ Administrator Privileges

When you run the application or the installer, Windows will prompt you for Administrator permissions (UAC). **This is completely normal and safe.** 

The app requires elevated privileges to perform its AI-driven system tasks, specifically:
* **Global Hotkey:** To ensure `Alt + Space` works seamlessly over any full-screen game or application.
* **Process Management:** To allow the AI to forcefully close running programs when you ask it to (e.g., *"Close Chrome"*).
* **Power Controls:** To execute system-level commands like shutting down or restarting your PC.

## 💻 Usage

**Method 1: Quick Install (Recommended)**
1. Go to the [Releases](../../releases) page on the right side.
2. Download the `Windows-aszisztens_1.0` setup file.
3. Run the downloaded file to install the app.

**Method 2: Download Full Repository**
1. Click the green **Code** button at the top of the repository and select **Download ZIP**.
2. Extract the downloaded folder to your PC.
3. Open the folder, find the `Windows-aszisztens_1.0` file, and run it to set up the app.

*(Note: On the first run, the app will ask for a **Free Google Gemini API Key**. You can get one with a single click [here](https://aistudio.google.com/app/apikey).)*

## 📜 License

This project is licensed under the MIT License - see the LICENSE file for details.

---
⚠️ **Disclaimer:** Gemini is an artificial intelligence and can make mistakes. Please use the PC control features responsibly and always verify important information.

---

### 👋 A Note from the Developer
Hi everyone! This is my very first programming project. It originally started as a Hungarian application, but I expanded it with multi-language support so more people can enjoy it (if there is a grammar mistakes in German and France sorry for that, i only learn hungarian and english). 

I have put a lot of time, effort, and passion into building this app, but I know there is always room to grow. Since I am still learning, I would absolutely love to hear your feedback! If you have any suggestions, ideas, or find something I should modify or improve, please don't hesitate to let me know (feel free to open an *Issue* here on GitHub).
Firstly I started an IT school, where I learn Network, how to programing Switches, Routers, ect. Then end of the year i got a request from my family member to make an website for a Pizza resturant. I made it and i loved the procces to make the a frontend and the a backend. Then I started to making my own YTMP3 downloader which is working pretty good. Then one day I woke up and I had na idea to make this project. I love this project, so i had an idea to put Gemini Windows Assistant to GitHub.

Thank you for checking out my work, i hope you'll enjoy it! ❤️

With Love: Medvecity
