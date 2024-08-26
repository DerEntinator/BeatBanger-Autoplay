# BeatBanger Autoplay

Simple BeatBanger auto-player with a WPF GUI for Windows 7+. <br>
(In principle just emulating keyboard presses timed to the underlying notes.cfg)

To run the standard version of the software you might need to install the specific .net runtime on the first launch if you don't have it already. When running it without the runtime you'll get a prompt to download it from Microsoft. <br>
If you don't want to deal with the runtime directly there is also a self-contained release you can choose.

After launching, pick the game folder and everything else should be self-explanatory.

The Start and Stop buttons are not really needed as you need to use the hotkeys to time the start of the game anyway. <br>
When starting a level hit the F10-Key (Play hotkey) when the timing would be perfect for the first note of the level. <br>
To stop the inputs hit the F11-Key (Stop hotkey). Stopping resets the inputs so you can't pause and resume a level right now.

The Reload button reloads all the sources if you add new mods at some point.

Pick either story levels or mods and click through the lists till you pick a difficulty. The timing sheet on the right is just for debugging and can be ignored.
Keys are defaulted for a standard qwerty layout on zxcv but can be customized. <br>
As for the polling delay: smaller values use more CPU but might help with hitting more perfects, although 10 worked perfectly for me.

![image](https://github.com/user-attachments/assets/6773d644-2e68-499c-b890-4e93d48467f5)
