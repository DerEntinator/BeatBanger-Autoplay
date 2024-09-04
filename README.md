# BeatBanger Autoplay

Simple BeatBanger auto-player with a WPF GUI for Windows 7+. <br>
(In principle just sending emulated keyboard presses to the game timed to the underlying notes.cfg)

To run the standard version of the software you might need to install a specific .net runtime. If you don't have it already you'll get a prompt to download it from Microsoft when you launch the software. <br>
If you don't want to deal with the runtime directly there is also the self-contained (_SC) release you can choose.

With V2.0 everything is prettier and better and ✨Just Works✨,... i hope...

If the exe is in the game folder when started it'll figure everything out on its own, if it's not you'll get a folder select where you have to select the game folder manually.<br>
Adding new mods to your game is handled automatically.<br>

The Reload button: hooks into the game and memory and reloads the in-game hotkeys(if you changed them)<br>
The Pause button stops the keyboard input. Useful if you want to play yourself for a bit or train a specific part of a level.<br>

**NOTE for the Alt-Version:**<br>
If the level name does not update when you switch levels in the game, a certain NTFS filesystem setting in Windows is probably set wrong.<br>
To fix this open Regedit and go to "HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\FileSystem".<br>
Then change the value of "NtfsDisableLastAccessUpdate" to 80000000 (hex) to ensure it always updates the last time a file has been  accessed.<br>
No need to restart the computer, just restart the game and restart the autoplayer and it should work.<br>
[NtfsDisableLastAccessUpdate Documentation](https://winaero.com/disable-ntfs-last-access-time-updates-in-windows-10/)

If everything is hooked and loaded you can just start any level in the game and watch it play itself.

Have fun :D!

# Alternative Version
If the standard version didn't work for you there is also an alternative, experimental version of the autoplayer that has a different method to get the currently played level.

![image](https://github.com/user-attachments/assets/8531e17c-2843-40b5-aecc-3db2c4b17865)
