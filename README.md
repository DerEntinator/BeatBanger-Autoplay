# BeatBanger Autoplay

Simple BeatBanger auto-player with a WPF GUI for Windows 7+. <br>
(In principle just sending emulated keyboard presses to the game timed to the underlying notes.cfg)

## Versions
To run the standard version of the software you might need to install a specific .net runtime. If you don't have it already you'll get a prompt to download it from Microsoft when you launch the software. <br>
If you don't want to deal with the runtime directly there is also the self-contained (_SC) release you can choose.<br>

## Using the App
With V2.0 everything is prettier and better and ✨Just Works✨,... i hope...

If the exe is in the game folder when started it'll figure everything out on its own, if it's not you'll get a file select where you have to select the game's exe manually.<br>
Adding new mods to your game is handled automatically.<br>

The Reload button: hooks into the game and memory and reloads the in-game hotkeys(if you changed them after starting the app)<br>
The Pause button stops the keyboard input. Useful if you want to play yourself for a bit or train a specific part of a level.<br>

**NOTE:**<br>
If the level name does not update when you switch levels in the game, a certain NTFS filesystem setting in Windows is probably set wrong.<br>
To fix this open Regedit and go to "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem".<br>
Then change the value of "NtfsDisableLastAccessUpdate" to 80000000 (hex) to ensure it always updates the last time a file has been  accessed.<br>
No need to restart the computer, just restart the game and restart the autoplayer and it should work.<br>
[NtfsDisableLastAccessUpdate Documentation](https://winaero.com/disable-ntfs-last-access-time-updates-in-windows-10/)

If everything is hooked and loaded you can just start any level in the game and watch it play itself.

Have fun :D!

![image](https://github.com/user-attachments/assets/7f450f9e-43cb-4c30-aad7-43738df87fa6)
