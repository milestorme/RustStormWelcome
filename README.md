# 🌩️ RustStorm Welcome UI v1.9.1

A premium welcome UI plugin for Rust servers featuring a branded popup, dynamic wipe timer, and fully configurable server information.

---

## 🚀 Highlights

- 🖼 **Banner Integration (URL-based)**
- 🎨 **Premium Rust-style UI layout**
- ⏱ **Live Wipe Timer (auto-updating every 30s)**
- 📊 **Config-driven server info**
- 📜 **Clean rules display (no duplication bugs)**
- ⚡ **Fully customizable commands list**
- 🔁 **/info command to reopen UI anytime**

---

## 🆕 What’s New in v1.9.1

- ✅ Live wipe timer updates in UI  
- ✅ Simplified timezone config (`GMT+8`, `Oceania`, etc.)  
- ✅ Fixed wipe timer not displaying issue  
- ✅ Improved banner scaling (no stretch)  
- ✅ Cleaned up UI layout and spacing  
- ✅ Proper timer cleanup (no background leaks)  

---

## ⚙️ Configuration

### 🔥 Recommended Wipe Timer Setup

```json
"WipeTimer": {
  "EnableDynamicWipeTimer": true,
  "TimeZone": "Oceania",
  "WipeDay": "Friday",
  "WipeHour24": 3,
  "WipeMinute": 0,
  "ShowExactLocalTime": true
}
```

---

### 🌍 Supported TimeZone Values

- Oceania (recommended)
- GMT+8
- UTC+8
- Perth

---

### 🖼 Banner

```json
"BannerUrl": "https://your-image-url.png"
```

---

## 📦 Installation

1. Place `RustStormWelcome.cs` in:
   ```
   /oxide/plugins/
   ```
2. Run:
   ```
   oxide.reload RustStormWelcome
   ```
3. Edit config:
   ```
   /oxide/config/RustStormWelcome.json
   ```

---

## 🎮 Commands

| Command | Description |
|--------|------------|
| `/info` | Opens the welcome UI |

---

## 🧠 Notes

- No external dependencies
- Safe config handling (no duplication issues)
- UI timers auto-clean (no leaks)
- Designed for performance and stability

---

## 💡 Known Behavior

- Wipe timer updates every 30 seconds while UI is open
- UI auto-closes after configured duration

---

## 🔮 Future Ideas

- Color-coded wipe timer  
- First-join vs returning player UI  
- Permission-based command visibility  
- Advanced UI effects  

---

## 👑 Credits

Milestorme
