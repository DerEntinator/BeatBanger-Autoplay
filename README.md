# BeatBanger Autoplay

Simple BeatBanger auto-player with a WPF GUI for Windows 7+. <br>
(In principle just emulating keyboard presses timed to the underlying notes.cfg)

To run the standard version of the software you might need to install a specific .net runtime. If you don't have it already you'll get a prompt to download it from Microsoft when you launch the software. <br>
If you don't want to deal with the runtime directly there is also the self-contained (_SC) release you can choose.

With V2.0 everything is prettier and better and ✨Just Works✨,... i hope...

If the exe is in the game folder when started it'll figure everything out on its own, if it's not you'll get a folder select where you have to select the game folder manually.<br>
Adding new mods to your game is handled automatically.<br>

The Reload button: hooks into the game and memory and reloads the in-game hotkeys(if you changed them)<br>
The Pause button stops the keyboard input. Useful if you want to play yourself for a bit or train a specific part of a level.<br>

EXPERIMENTAL: Speed adjustment (dropdown) between buttons. In-Game when selecting a difficulty adjust speed by pressing Key 5/6 (Q/E). Select the speed in the dropdown before starting the difficulty with the selected speed.

If everything is hooked and loaded you can just start any level in the game and watch it play itself.

Have fun :D!

Note: You'll probably get an error message when going to act 2 of the story because all the necessary files are already present but don't contain the proper values, just click the error away and go back to the other levels.

![image](https://github.com/user-attachments/assets/6c18cc1a-8235-4e21-bd32-efd94fa48028)
