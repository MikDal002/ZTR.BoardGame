Location: `src/ZtrBoardGame.RaspberryPi/HardwareAccess/SystemConfigurers/BootConfigSystemConfigurer.cs`

What is wrong and why:
This file reads and writes directly to `/boot/firmware/config.txt`. While this is the correct path for newer Raspberry Pi OS versions (Bookworm), older versions use `/boot/config.txt`. Hardcoding `/boot/firmware/config.txt` will fail on older systems.
More importantly, rewriting a critical system file using `File.WriteAllLinesAsync` without any backup mechanism or atomic writes is extremely dangerous. A crash, power loss, or exception thrown during this operation will result in a truncated or corrupted boot configuration, potentially bricking the device and requiring the user to manually mount and fix the SD card on another computer.

Proposed solution:
1. Detect the OS version or dynamically resolve the correct config path (check if `/boot/firmware/config.txt` exists before falling back to `/boot/config.txt`).
2. Implement a backup mechanism: copy the original file to `config.txt.bak` before modifying it.
3. Use atomic writes: write the new content to a temporary file first, then use `File.Move` (or `mv` command) to replace the original file safely.
