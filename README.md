# VATNZ NZ Extension Plugin

A lightweight vatSys plugin for New Zealand controllers that automatically generates clean, comma-separated sector extension lines based on active VSCS frequencies.  
Works with any VSCS naming convention — no renaming required.

---

## Features

- **Frequency-only matching**  
  Uses the frequency itself, not the VSCS label, so any naming convention works.

- **Skips your own frequency**  
  Prevents incorrect self-extensions like `Extending OCR 123.9` when logged in on that frequency.

- **Always comma-separated output**  
  Never uses "and".  
  Example:
  ```text
  Extending OCR 123.9, BAY 119.5, NAK 123.7
---

## Auto-clears when disconnected

The extension line disappears as soon as you disconnect from VATSIM.

## Updates every 2 seconds

Ensures the extension line is always accurate.

## Compatible with C# 7.3

No modern language features required.

---

## How It Works

The plugin scans your active VSCS frequencies and compares them against a built-in NZ mapping:

| Frequency | Sector |
|----------|--------|
| 123.9    | OCR    |
| 119.5    | BAY    |
| 123.7    | NAK    |
| 126.2    | OHA    |
| 129.3    | STH    |
| 129.4    | KAI    |

If a frequency is transmitting and not your own, it is added to the extension list.

Example output:

Extending BAY 119.5, NAK 123.7, OHA 126.2

---

## Installation

1. Download the Zip
2. Extract the Zip and move the folder into
   Documents\vatSys Files\Profiles\New Zealand
3. Start vatSys.
4. The plugin will begin updating automatically.

---

## Debug Logging

Logs are written to:

Documents\vatSysPluginDebug\vatnz_plugin_debug.txt

The log includes:

- Plugin load confirmation
- VSCS frequency dumps
- Skipped own frequency events
- Extension line updates
- Disconnect detection

---

## Compatibility

- vatSys plugin API
- Windows 10/11
- C# 7.3 or later
- No external dependencies

---

## Future Enhancements

Possible future additions:

- Custom NZ ordering (OCR → BAY → NAK → OHA → KAI → STH)
  
---

## Contributions

Pull requests are welcome, especially for:

- Additional NZ sectors
- Improved mapping logic
- Performance tweaks
- Documentation updates

---

