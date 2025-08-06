using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace BeatBanger_Autoplay
{
    public class Keyvent
    {
        public Keys key;
        public double timestamp;
        public bool down;
    }

    public class ConfigFile
    {
        public string filepath = "";
        public string level = "";
        public string levelPack = "";
        public string type = "";
    }

    public class memSpace
    {
        public IntPtr baseAddress;
        public IntPtr endAddress;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MEMORY_BASIC_INFORMATION64
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public uint __alignment1;
        public ulong RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
        public uint __alignment2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    public partial class MainWindow : Window
    {
        int fileCount = 0;
        int fileOffset = 0;
        List<ConfigFile> fileList = new List<ConfigFile>();
        ConfigFile currentLevel = new ConfigFile();

        int levelCount = 0;
        double levelDelay = 0.0;
        List<string> difficulties = new List<string>();
        JObject notesJSON;
        List<List<Keyvent>> timesheet = new List<List<Keyvent>>();

        string gameFolder = "ERROR";
        string keybindingsPathSav = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Godot\\app_userdata\\Beat Banger\\binds.sav";
        string keybindingsPathIni = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Godot\\app_userdata\\Beat Banger\\binds.ini";
        string modPathAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Godot\\app_userdata\\Beat Banger\\mods";
        Keys key1 = Keys.A;
        Keys key2 = Keys.S;
        Keys key3 = Keys.D;
        Keys key4 = Keys.F;

        private static readonly Dictionary<int, int> godotToWindowsKeyMap = new Dictionary<int, int>    {
            // Godot Key          => Windows Virtual-Key Code (VK_...)
            // Printable keys (Note: Godot distinguishes between uppercase and lowercase, Windows VK codes generally don't)
            { 32, 0x20 },   // KEY_SPACE      => VK_SPACE
            { 33, 0x21 },   // KEY_EXCLAM     => VK_OEM_7 (with Shift) - This is an approximation
            { 34, 0xDE },   // KEY_QUOTEDBL   => VK_OEM_7 (with Shift) - This is an approximation
            { 35, 0x23 },   // KEY_NUMBERSIGN => VK_OEM_3 (with Shift) - This is an approximation
            { 36, 0x24 },   // KEY_DOLLAR     => VK_OEM_4 (with Shift) - This is an approximation
            { 37, 0x25 },   // KEY_PERCENT    => VK_OEM_5 (with Shift) - This is an approximation
            { 38, 0x26 },   // KEY_AMPERSAND  => VK_OEM_7 (with Shift) - This is an approximation
            { 39, 0xDE },   // KEY_APOSTROPHE => VK_OEM_7
            { 40, 0x28 },   // KEY_PARENLEFT  => VK_OEM_1 (with Shift) - This is an approximation
            { 41, 0x29 },   // KEY_PARENRIGHT => VK_OEM_PLUS (with Shift) - This is an approximation
            { 42, 0x2A },   // KEY_ASTERISK   => VK_OEM_8 (with Shift) - This is an approximation
            { 43, 0xBB },   // KEY_PLUS       => VK_OEM_PLUS
            { 44, 0xBC },   // KEY_COMMA      => VK_OEM_COMMA
            { 45, 0xBD },   // KEY_MINUS      => VK_OEM_MINUS
            { 46, 0xBE },   // KEY_PERIOD     => VK_OEM_PERIOD
            { 47, 0xBF },   // KEY_SLASH      => VK_OEM_2
            { 48, 0x30 },   // KEY_0          => VK_0
            { 49, 0x31 },   // KEY_1          => VK_1
            { 50, 0x32 },   // KEY_2          => VK_2
            { 51, 0x33 },   // KEY_3          => VK_3
            { 52, 0x34 },   // KEY_4          => VK_4
            { 53, 0x35 },   // KEY_5          => VK_5
            { 54, 0x36 },   // KEY_6          => VK_6
            { 55, 0x37 },   // KEY_7          => VK_7
            { 56, 0x38 },   // KEY_8          => VK_8
            { 57, 0x39 },   // KEY_9          => VK_9
            { 58, 0x3A },   // KEY_COLON      => VK_OEM_1 (with Shift) - This is an approximation
            { 59, 0xBA },   // KEY_SEMICOLON  => VK_OEM_1
            { 60, 0x3C },   // KEY_LESS       => VK_OEM_COMMA (with Shift) - This is an approximation
            { 61, 0x3D },   // KEY_EQUAL      => VK_OEM_PLUS
            { 62, 0x3E },   // KEY_GREATER    => VK_OEM_PERIOD (with Shift) - This is an approximation
            { 63, 0x3F },   // KEY_QUESTION   => VK_OEM_2 (with Shift) - This is an approximation
            { 64, 0x40 },   // KEY_AT         => VK_OEM_2 (with Shift) - This is an approximation
            { 65, 0x41 },   // KEY_A          => VK_A
            { 66, 0x42 },   // KEY_B          => VK_B
            { 67, 0x43 },   // KEY_C          => VK_C
            { 68, 0x44 },   // KEY_D          => VK_D
            { 69, 0x45 },   // KEY_E          => VK_E
            { 70, 0x46 },   // KEY_F          => VK_F
            { 71, 0x47 },   // KEY_G          => VK_G
            { 72, 0x48 },   // KEY_H          => VK_H
            { 73, 0x49 },   // KEY_I          => VK_I
            { 74, 0x4A },   // KEY_J          => VK_J
            { 75, 0x4B },   // KEY_K          => VK_K
            { 76, 0x4C },   // KEY_L          => VK_L
            { 77, 0x4D },   // KEY_M          => VK_M
            { 78, 0x4E },   // KEY_N          => VK_N
            { 79, 0x4F },   // KEY_O          => VK_O
            { 80, 0x50 },   // KEY_P          => VK_P
            { 81, 0x51 },   // KEY_Q          => VK_Q
            { 82, 0x52 },   // KEY_R          => VK_R
            { 83, 0x53 },   // KEY_S          => VK_S
            { 84, 0x54 },   // KEY_T          => VK_T
            { 85, 0x55 },   // KEY_U          => VK_U
            { 86, 0x56 },   // KEY_V          => VK_V
            { 87, 0x57 },   // KEY_W          => VK_W
            { 88, 0x58 },   // KEY_X          => VK_X
            { 89, 0x59 },   // KEY_Y          => VK_Y
            { 90, 0x5A },   // KEY_Z          => VK_Z
            { 91, 0xDB },   // KEY_BRACKETLEFT  => VK_OEM_4
            { 92, 0xDC },   // KEY_BACKSLASH    => VK_OEM_5
            { 93, 0xDD },   // KEY_BRACKETRIGHT => VK_OEM_6
            { 94, 0x5E },   // KEY_ASCIICIRCUM  => VK_OEM_6 (with Shift) - This is an approximation
            { 95, 0x5F },   // KEY_UNDERSCORE   => VK_OEM_MINUS (with Shift) - This is an approximation
            { 96, 0xC0 },   // KEY_QUOTELEFT    => VK_OEM_3
            { 123, 0xDB },  // KEY_BRACELEFT    => VK_OEM_4 (with Shift) - This is an approximation
            { 124, 0xDC },  // KEY_BAR          => VK_OEM_5 (with Shift) - This is an approximation
            { 125, 0xDD },  // KEY_BRACERIGHT   => VK_OEM_6 (with Shift) - This is an approximation
            { 126, 0xC0 },  // KEY_ASCIITILDE   => VK_OEM_3 (with Shift) - This is an approximation
            // { 165, ??? }, // KEY_YEN          => No direct equivalent - This is an approximation
            // { 167, ??? }, // KEY_SECTION      => No direct equivalent - This is an approximation

            // Non-printable keys
            { 4194305, 0x1B }, // KEY_ESCAPE     => VK_ESCAPE
            { 4194306, 0x09 }, // KEY_TAB        => VK_TAB
            { 4194307, 0x09 }, // KEY_BACKTAB    => VK_TAB (with Shift)
            { 4194308, 0x08 }, // KEY_BACKSPACE  => VK_BACK
            { 4194309, 0x0D }, // KEY_ENTER      => VK_RETURN
            { 4194310, 0x0D }, // KEY_KP_ENTER   => VK_RETURN (on numeric keypad)
            { 4194311, 0x2D }, // KEY_INSERT     => VK_INSERT
            { 4194312, 0x2E }, // KEY_DELETE     => VK_DELETE
            { 4194313, 0x13 }, // KEY_PAUSE      => VK_PAUSE
            { 4194314, 0x2C }, // KEY_PRINT      => VK_SNAPSHOT
            { 4194315, 0x2C }, // KEY_SYSREQ     => VK_SNAPSHOT (combined with Alt)
            { 4194316, 0x0C }, // KEY_CLEAR      => VK_CLEAR (rarely used)
            { 4194317, 0x24 }, // KEY_HOME       => VK_HOME
            { 4194318, 0x23 }, // KEY_END        => VK_END
            { 4194319, 0x25 }, // KEY_LEFT       => VK_LEFT
            { 4194320, 0x26 }, // KEY_UP         => VK_UP
            { 4194321, 0x27 }, // KEY_RIGHT      => VK_RIGHT
            { 4194322, 0x28 }, // KEY_DOWN       => VK_DOWN
            { 4194323, 0x21 }, // KEY_PAGEUP     => VK_PRIOR
            { 4194324, 0x22 }, // KEY_PAGEDOWN   => VK_NEXT
            { 4194325, 0x10 }, // KEY_SHIFT      => VK_SHIFT
            { 4194326, 0x11 }, // KEY_CTRL       => VK_CONTROL
            { 4194327, 0xA5 }, // KEY_META       => VK_MENU (Right Alt/AltGr) - This is an approximation
            { 4194328, 0x12 }, // KEY_ALT        => VK_MENU
            { 4194329, 0x14 }, // KEY_CAPSLOCK   => VK_CAPITAL
            { 4194330, 0x90 }, // KEY_NUMLOCK    => VK_NUMLOCK
            { 4194331, 0x91 }, // KEY_SCROLLLOCK => VK_SCROLL
            { 4194332, 0x70 }, // KEY_F1         => VK_F1
            { 4194333, 0x71 }, // KEY_F2         => VK_F2
            { 4194334, 0x72 }, // KEY_F3         => VK_F3
            { 4194335, 0x73 }, // KEY_F4         => VK_F4
            { 4194336, 0x74 }, // KEY_F5         => VK_F5
            { 4194337, 0x75 }, // KEY_F6         => VK_F6
            { 4194338, 0x76 }, // KEY_F7         => VK_F7
            { 4194339, 0x77 }, // KEY_F8         => VK_F8
            { 4194340, 0x78 }, // KEY_F9         => VK_F9
            { 4194341, 0x79 }, // KEY_F10        => VK_F10
            { 4194342, 0x7A }, // KEY_F11        => VK_F11
            { 4194343, 0x7B }, // KEY_F12        => VK_F12
            { 4194344, 0x7C }, // KEY_F13        => VK_F13
            { 4194345, 0x7D }, // KEY_F14        => VK_F14
            { 4194346, 0x7E }, // KEY_F15        => VK_F15
            { 4194347, 0x7F }, // KEY_F16        => VK_F16
            { 4194348, 0x80 }, // KEY_F17        => VK_F17
            { 4194349, 0x81 }, // KEY_F18        => VK_F18
            { 4194350, 0x82 }, // KEY_F19        => VK_F19
            { 4194351, 0x83 }, // KEY_F20        => VK_F20
            { 4194352, 0x84 }, // KEY_F21        => VK_F21
            { 4194353, 0x85 }, // KEY_F22        => VK_F22
            { 4194354, 0x86 }, // KEY_F23        => VK_F23
            { 4194355, 0x87 }, // KEY_F24        => VK_F24
            // ... (F25-F32 are not typically mapped to standard Windows VK codes)
            { 4194433, 0x6A }, // KEY_KP_MULTIPLY => VK_MULTIPLY
            { 4194434, 0x6F }, // KEY_KP_DIVIDE   => VK_DIVIDE
            { 4194435, 0x6D }, // KEY_KP_SUBTRACT => VK_SUBTRACT
            { 4194436, 0x6E }, // KEY_KP_PERIOD   => VK_DECIMAL
            { 4194437, 0x6B }, // KEY_KP_ADD      => VK_ADD
            { 4194438, 0x60 }, // KEY_KP_0        => VK_NUMPAD0
            { 4194439, 0x61 }, // KEY_KP_1        => VK_NUMPAD1
            { 4194440, 0x62 }, // KEY_KP_2        => VK_NUMPAD2
            { 4194441, 0x63 }, // KEY_KP_3        => VK_NUMPAD3
            { 4194442, 0x64 }, // KEY_KP_4        => VK_NUMPAD4
            { 4194443, 0x65 }, // KEY_KP_5        => VK_NUMPAD5
            { 4194444, 0x66 }, // KEY_KP_6        => VK_NUMPAD6
            { 4194445, 0x67 }, // KEY_KP_7        => VK_NUMPAD7
            { 4194446, 0x68 }, // KEY_KP_8        => VK_NUMPAD8
            { 4194447, 0x69 }, // KEY_KP_9        => VK_NUMPAD9
            { 4194370, 0x5D }, // KEY_MENU       => VK_APPS
            // { 4194371, ??? }, // KEY_HYPER      => No direct equivalent
            { 4194373, 0x2F }, // KEY_HELP       => VK_HELP
            { 4194376, 0xA6 }, // KEY_BACK       => VK_BROWSER_BACK
            { 4194377, 0xA7 }, // KEY_FORWARD    => VK_BROWSER_FORWARD
            { 4194378, 0xAB }, // KEY_STOP       => VK_BROWSER_STOP
            { 4194379, 0xA8 }, // KEY_REFRESH    => VK_BROWSER_REFRESH
            { 4194380, 0xAE }, // KEY_VOLUMEDOWN => VK_VOLUME_DOWN
            { 4194381, 0xAD }, // KEY_VOLUMEMUTE => VK_VOLUME_MUTE
            { 4194382, 0xAF }, // KEY_VOLUMEUP   => VK_VOLUME_UP
            { 4194388, 0xB3 }, // KEY_MEDIAPLAY  => VK_MEDIA_PLAY_PAUSE
            { 4194389, 0xB2 }, // KEY_MEDIASTOP  => VK_MEDIA_STOP
            { 4194390, 0xB1 }, // KEY_MEDIAPREVIOUS => VK_MEDIA_PREV_TRACK
            { 4194391, 0xB0 }, // KEY_MEDIANEXT  => VK_MEDIA_NEXT_TRACK
            // { 4194392, ??? }, // KEY_MEDIARECORD => No standard VK code
            { 4194393, 0xAA }, // KEY_HOMEPAGE   => VK_BROWSER_HOME
            { 4194394, 0xA9 }, // KEY_FAVORITES  => VK_BROWSER_FAVORITES
            { 4194395, 0xAC }, // KEY_SEARCH     => VK_BROWSER_SEARCH
            { 4194396, 0x5F }, // KEY_STANDBY    => VK_SLEEP
            { 4194397, 0xB5 }, // KEY_OPENURL    => VK_LAUNCH_MAIL (or similar) - This is an approximation
            { 4194398, 0xB4 }, // KEY_LAUNCHMAIL => VK_LAUNCH_MAIL
            { 4194399, 0xB5 }, // KEY_LAUNCHMEDIA => VK_LAUNCH_MEDIA_SELECT
            // ... (LAUNCH0 - LAUNCHF have no standard VK codes)
        };

        const uint WM_KEYDOWN = 0x100;
        const uint WM_KEYUP = 0x0101;

        bool levelLoaded = false;
        bool cancleRun = false;
        int pollingDelay = 1;

        const uint _PROCESS_ALL_ACCESS = 0x1fffff;
        const uint _MEM_PRIVATE = 0x20000;
        const uint _MEM_COMMIT = 0x1000;
        private static readonly string[] ProcessNames = { "beatbanger" };
        int oldPID = 0;
        private static IntPtr _processHandle;
        private static IntPtr _windowHandle;
        List<memSpace> memAddresses = new List<memSpace>();
        private static IntPtr _timeAddress;
        private static IntPtr _dataAddress;
        bool paused = false;

        public MainWindow()
        {
            InitializeComponent();

            Reload();
            Task.Run(() =>
            {
                getLevel();
            });
        }

        #region UI-Functions
        private void Reload_Click(object sender, RoutedEventArgs e)
        { try { Reload(); } catch (Exception ex) { errorMessage(ex); } }

        private void Pause_Click(object sender, RoutedEventArgs e)
        { try { paused = true; State_Display.Background = System.Windows.Media.Brushes.IndianRed; } catch (Exception ex) { errorMessage(ex); } }
        private void Pause_UnClick(object sender, RoutedEventArgs e)
        { try { paused = false; State_Display.Background = System.Windows.Media.Brushes.DarkSeaGreen; } catch (Exception ex) { errorMessage(ex); } }
        private void SpeedPick_Changer(object sender, SelectionChangedEventArgs e)
        { try { LoadNotes().Wait(); } catch (Exception ex) { errorMessage(ex); } }
        #endregion

        #region General-Functions
        private bool loadFolder()
        {
            if (!File.Exists(gameFolder + "\\beatbanger.exe"))
            {
                if (File.Exists(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\beatbanger.exe"))
                {
                    gameFolder = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName); return true;
                }
                else
                {
                    CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                    dialog.Multiselect = false;
                    dialog.Title = "Select game executable!";
                    ///Ask user for File selection///
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        if (Path.GetFileName(dialog.FileName) != "beatbanger.exe")
                        {
                            gameFolder = "ERROR";
                            Level_Textblock.Text = "ERROR! - game exe not selected - reload";
                            return false;
                        }
                        else
                        {
                            gameFolder = Path.GetDirectoryName(dialog.FileName);
                            Level_Textblock.Text = "";
                        }
                        return true;
                    }
                    else
                    { gameFolder = "ERROR"; Level_Textblock.Text = "ERROR! - nothing selected - reload"; return false; }
                }
            }
            else { return true; }
        }

        private void Reload()
        {
            Notes_Textblock.Text = "";

            ReloadKeys();
            loadFolder();

            if (ProcessNames.Count() != 0)
                foreach (string processName in ProcessNames.ToArray())
                {
                    if (Process.GetProcessesByName(processName).Length == 0) { Notes_Textblock.Text = "Game is not launched!"; return; }

                    if (oldPID == 0 || oldPID != Process.GetProcessesByName(processName)[0].Id || _processHandle == IntPtr.Zero || _windowHandle == IntPtr.Zero || _timeAddress == IntPtr.Zero || _dataAddress == IntPtr.Zero)
                    {
                        oldPID = Process.GetProcessesByName(processName)[0].Id;
                        // try to get process handle
                        if (!Connect()) { Notes_Textblock.Text += "Failed to get Handle for Process or Window!\n"; }
                        else LoadAddresses();
                    }
                    else
                    {
                        if (_processHandle == IntPtr.Zero || _windowHandle == IntPtr.Zero)
                            Notes_Textblock.Text += "Failed to get Handle for Process or Window!\n";
                        else
                            Notes_Textblock.Text += "Game connected! \nHandles: P:" + _processHandle.ToString("X8") + " W:" + _windowHandle.ToString("X8") + "\n";

                        if (_timeAddress != IntPtr.Zero)
                            Notes_Textblock.Text += "Time hooked! \nAddress: " + _timeAddress.ToString("X8") + "\n";
                        else
                            Notes_Textblock.Text += "Time not found \n";

                        if (_dataAddress != IntPtr.Zero)
                            Notes_Textblock.Text += "Data hooked! \nAddress: " + _dataAddress.ToString("X8") + "\n";
                        else
                            Notes_Textblock.Text += "Data not found - try restarting the game if this happenes again after pressing reload\n";

                    }
                }
            clearMemory();
        }

        private void ReloadKeys()
        {
            try
            {
                bool savExists = File.Exists(keybindingsPathSav);
                bool iniExists = File.Exists(keybindingsPathIni);

                if (!savExists && !iniExists)
                {
                    // Neither file exists, use defaults and show an error.
                    key1 = Keys.A; key2 = Keys.S; key3 = Keys.D; key4 = Keys.F;
                    errorMessage(new FileNotFoundException("Could not find 'binds.sav' or 'binds.ini'. Using default keys (A, S, D, F)."));
                }
                else if (iniExists && (!savExists || File.GetLastAccessTimeUtc(keybindingsPathIni) > File.GetLastAccessTimeUtc(keybindingsPathSav)))
                {
                    // New .ini file is newer or is the only one that exists.
                    ParseNewKeybinds(keybindingsPathIni);
                }
                else
                {
                    // Old .sav file is newer or is the only one that exists.
                    ParseOldKeybinds(keybindingsPathSav);
                }

                LoadNotes().Wait();
            }
            catch (Exception ex) { errorMessage(ex); }
        }

        private void ParseOldKeybinds(string filePath)
        {
            string bindingsText = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
            bindingsText = bindingsText.Replace("\n", "").Replace("\r", "");
            bindingsText = "{\"registered_keys\":" + bindingsText.Remove(0, bindingsText.IndexOf("{") - 1);
            bindingsText = bindingsText.Remove(bindingsText.LastIndexOf("}") + 2) + "}";
            JObject bindingsObj = JObject.Parse(bindingsText);
            var binds = bindingsObj["registered_keys"].Children().ToList();

            // Helper to find a keycode for a specific action, with a fallback.
            int GetKeycodeForAction(string actionName, int defaultKey)
            {
                var binding = binds.FirstOrDefault(b => b["action"]?.Value<string>() == actionName);
                return binding?["keycode"]?.Value<int>() ?? defaultKey;
            }

            // Map actions to keys, providing Godot keycodes for defaults (A, S, D, F)
            int k1_godot = GetKeycodeForAction("action_0", 65); // Default A
            int k2_godot = GetKeycodeForAction("action_1", 83); // Default S
            int k3_godot = GetKeycodeForAction("action_2", 68); // Default D
            int k4_godot = GetKeycodeForAction("action_3", 70); // Default F

            key1 = (Keys)godotToWindowsKeyMap[k1_godot];
            key2 = (Keys)godotToWindowsKeyMap[k2_godot];
            key3 = (Keys)godotToWindowsKeyMap[k3_godot];
            key4 = (Keys)godotToWindowsKeyMap[k4_godot];
        }

        private void ParseNewKeybinds(string filePath)
        {
            string content = File.ReadAllText(filePath);

            // Helper function to find the keycode for a specific "game_note_X" action
            int FindKeycodeForAction(string actionName, int defaultKey)
            {
                // The action name we are looking for, e.g., "action": &"game_note_0"
                string searchTerm = $"\"action\": &\"{actionName}\"";
                int actionPos = content.IndexOf(searchTerm);
                if (actionPos == -1) return defaultKey;

                // Find the "InputEventKey" object within this action's scope to ensure we get a keyboard key
                int eventKeyPos = content.IndexOf("Object(InputEventKey,", actionPos);
                if (eventKeyPos == -1) return defaultKey;

                // Find the end of this action block to avoid reading into the next one
                int nextActionPos = content.IndexOf("}, {", actionPos);
                if (nextActionPos == -1) nextActionPos = content.Length;

                // Ensure the InputEventKey we found belongs to *this* action block
                if (eventKeyPos > nextActionPos) return defaultKey;

                // Find the keycode label and extract its value
                int keycodeLabelPos = content.IndexOf("\"keycode\":", eventKeyPos);
                if (keycodeLabelPos == -1 || keycodeLabelPos > nextActionPos) return defaultKey;

                int keycodeStart = keycodeLabelPos + "\"keycode\":".Length;
                int keycodeEnd = content.IndexOfAny(new[] { ',', '}' }, keycodeStart);

                if (int.TryParse(content.Substring(keycodeStart, keycodeEnd - keycodeStart).Trim(), out int godotKey))
                {
                    return godotKey;
                }
                return defaultKey;
            }
            ;

            // Find Godot keycodes for the 4 main game notes, providing defaults
            int k1_godot = FindKeycodeForAction("game_note_0", 65); // Default A
            int k2_godot = FindKeycodeForAction("game_note_1", 83); // Default S
            int k3_godot = FindKeycodeForAction("game_note_2", 68); // Default D
            int k4_godot = FindKeycodeForAction("game_note_3", 70); // Default F

            // Map the found Godot keycodes to Windows Keys, using the map
            key1 = godotToWindowsKeyMap.ContainsKey(k1_godot) ? (Keys)godotToWindowsKeyMap[k1_godot] : Keys.A;
            key2 = godotToWindowsKeyMap.ContainsKey(k2_godot) ? (Keys)godotToWindowsKeyMap[k2_godot] : Keys.S;
            key3 = godotToWindowsKeyMap.ContainsKey(k3_godot) ? (Keys)godotToWindowsKeyMap[k3_godot] : Keys.D;
            key4 = godotToWindowsKeyMap.ContainsKey(k4_godot) ? (Keys)godotToWindowsKeyMap[k4_godot] : Keys.F;
        }

        private async Task getLevel()
        {
            while (true)
            {
                try
                {
                    if (gameFolder != "ERROR" && _processHandle != IntPtr.Zero && _windowHandle != IntPtr.Zero && _timeAddress != IntPtr.Zero && _dataAddress != IntPtr.Zero)
                    {
                        List<string> tempList = Directory.GetFiles(gameFolder, "notes.cfg", SearchOption.AllDirectories).ToList();
                        tempList.AddRange(Directory.GetFiles(modPathAppData, "notes.cfg", SearchOption.AllDirectories).ToList());
                        tempList.AddRange(Directory.GetFiles(gameFolder + "\\mods", "notes.cfg", SearchOption.AllDirectories).ToList());

                        if (levelCount != tempList.Count)
                        {
                            fileList.Clear();
                            foreach (string file in tempList)
                            {
                                ConfigFile temp = new ConfigFile();
                                temp.filepath = file;
                                temp.level = GetLevelName(file);
                                temp.levelPack = GetLevelPackName(file);
                                temp.type = GetTypeName(file);
                                fileList.Add(temp);
                            }
                            levelCount = tempList.Count;
                        }

                        string oldLastAccess = currentLevel.filepath;

                        DateTime lastAccess = DateTime.MinValue;
                        foreach (var file in fileList)
                        {
                            if (lastAccess < File.GetLastAccessTimeUtc(file.filepath))
                            {
                                currentLevel = file;
                                lastAccess = File.GetLastAccessTimeUtc(file.filepath);
                            }
                        }

                        if (levelLoaded == false || oldLastAccess != currentLevel.filepath)
                        {
                            cancleRun = true;

                            string config = File.ReadAllText(currentLevel.filepath, System.Text.Encoding.UTF8);
                            if (config.Contains("charts"))
                            {
                                string settings = File.ReadAllText(GetSettingsPath(currentLevel.filepath), System.Text.Encoding.UTF8);
                                settings = settings.Replace("\n", "").Replace("\r", "");
                                settings = "{\"data\":" + settings.Remove(0, settings.IndexOf("{"));
                                settings = settings.Remove(settings.LastIndexOf("}") + 1) + "}";
                                JObject settingsObj = JObject.Parse(settings);

                                levelDelay = settingsObj["data"]["song_offset"].Value<double>() * 1000.0;

                                string meta = File.ReadAllText(GetMetaPath(currentLevel.filepath), System.Text.Encoding.UTF8);
                                // format file
                                meta = meta.Replace("\n", "").Replace("\r", "");
                                meta = "{\"data\":" + meta.Remove(0, meta.IndexOf("{"));
                                meta = meta.Remove(meta.LastIndexOf("}") + 1) + "}";
                                // fix new color attribute
                                meta = meta.Replace("Color(", "[").Replace(")", "]");
                                JObject metaObj = JObject.Parse(meta);

                                currentLevel.level = metaObj["data"]["level_name"].Value<string>();
                                Dispatcher.BeginInvoke(() => Level_Textblock.Text = "Level: " + currentLevel.level);

                                // Jsonify
                                config = config.Replace("\n", "").Replace("\r", "");
                                config = "{\"data\":" + config.Remove(0, config.IndexOf("{"));
                                config = config.Remove(config.LastIndexOf("}") + 1) + "}";

                                notesJSON = JObject.Parse(config);
                                difficulties.Clear();

                                difficulties = notesJSON["data"]["charts"].Children().Values<string>("name").ToList();

                                await LoadNotes();

                                cancleRun = false;
                                levelLoaded = true;
                                Task.Run(() =>
                                {
                                    run();
                                });
                            }
                        }

                    }
                }
                catch (Exception ex) { levelLoaded = false; /*errorMessage(ex);*/ }
                await Task.Delay(100);
            }
        }

        private async Task LoadNotes()
        {
            timesheet.Clear();
            if (notesJSON != null)
            {
                if (difficulties.Count() != 0)
                    for (int i = 0; i < difficulties.Count; i++)
                    {
                        List<JToken> rawNotes = notesJSON["data"]["charts"][i]["notes"].ToList();
                        List<Keyvent> keys = new List<Keyvent>();
                        if (rawNotes.Count() != 0)
                            foreach (JToken note in rawNotes.ToArray())
                            {
                                if (note.Value<int>("note_modifier") != 1)                                   //handle Autoplay notes
                                {
                                    Keyvent pressKey = new Keyvent() { down = true };
                                    Keyvent holdRelease = new Keyvent() { down = false };

                                    switch (note.Value<int>("input_type"))
                                    {
                                        case 0:
                                            Dispatcher.Invoke(() => { pressKey.key = key1; });
                                            Dispatcher.Invoke(() => { holdRelease.key = key1; });
                                            break;
                                        case 1:
                                            Dispatcher.Invoke(() => { pressKey.key = key2; });
                                            Dispatcher.Invoke(() => { holdRelease.key = key2; });
                                            break;
                                        case 2:
                                            Dispatcher.Invoke(() => { pressKey.key = key3; });
                                            Dispatcher.Invoke(() => { holdRelease.key = key3; });
                                            break;
                                        case 3:
                                            Dispatcher.Invoke(() => { pressKey.key = key4; });
                                            Dispatcher.Invoke(() => { holdRelease.key = key4; });
                                            break;
                                    }

                                    pressKey.timestamp = levelDelay + (note.Value<double>("timestamp") * 1000.0);
                                    if (note.Value<double>("hold_end_timestamp") != 0.0)
                                        holdRelease.timestamp = levelDelay + (note.Value<double>("hold_end_timestamp") * 1000.0);
                                    else
                                        holdRelease.timestamp = levelDelay + (note.Value<double>("timestamp") * 1000.0 + 30.0);

                                    keys.Add(pressKey);
                                    keys.Add(holdRelease);
                                }
                            }

                        timesheet.Add(keys.OrderBy(s => s.timestamp).ThenBy(s => s.down).ToList());
                    }
            }
            clearMemory();
        }

        private async Task run()
        {
            byte[] timeBuffer = new byte[8];
            double timeRead = 0.0;
            byte[] dataBuffer = new byte[22 * 8];
            byte currentDifficulty = 0;

            while (true)
            {
                try
                {
                    if (_processHandle != IntPtr.Zero && _windowHandle != IntPtr.Zero && _timeAddress != IntPtr.Zero && _dataAddress != IntPtr.Zero)
                    {
                        ReadProcessMemory(_processHandle, _timeAddress, timeBuffer, sizeof(double), out _);
                        timeRead = BitConverter.ToDouble(timeBuffer) * 1000.0;
                        if (timeRead > 0 && timesheet.Count != 0 && timeRead < timesheet[0].Last().timestamp)
                        {
                            ReadProcessMemory(_processHandle, _dataAddress, dataBuffer, 22 * 8 * sizeof(byte), out _);
                            if (dataBuffer[0 * 8] == 0)
                                currentDifficulty = dataBuffer[9 * 8];
                            else currentDifficulty = dataBuffer[21 * 8];

                            Keys buffer;
                            if (timesheet[currentDifficulty].Count != 0 && timeRead < timesheet[currentDifficulty][0].timestamp)
                                for (int n = 0; n < timesheet[currentDifficulty].Count; n++)
                                {
                                    buffer = timesheet[currentDifficulty][n].key;
                                    do
                                    {
                                        if (timeRead == 0.0)
                                            goto restartLevel;
                                        await Task.Delay(pollingDelay);
                                        ReadProcessMemory(_processHandle, _timeAddress, timeBuffer, sizeof(double), out _);
                                        timeRead = BitConverter.ToDouble(timeBuffer) * 1000.0;
                                    } while (timeRead + (pollingDelay / 2.0) < timesheet[currentDifficulty][n].timestamp);

                                    if (!paused)
                                    {
                                        if (timesheet[currentDifficulty][n].down)
                                            PostMessage(_windowHandle, WM_KEYDOWN, (IntPtr)(buffer), IntPtr.Zero);
                                        else
                                            PostMessage(_windowHandle, WM_KEYUP, (IntPtr)(buffer), IntPtr.Zero);
                                    }
                                }
                            restartLevel:
                            PostMessage(_windowHandle, WM_KEYUP, (IntPtr)(key1), IntPtr.Zero);
                            PostMessage(_windowHandle, WM_KEYUP, (IntPtr)(key2), IntPtr.Zero);
                            PostMessage(_windowHandle, WM_KEYUP, (IntPtr)(key3), IntPtr.Zero);
                            PostMessage(_windowHandle, WM_KEYUP, (IntPtr)(key4), IntPtr.Zero);
                        }
                        if (cancleRun)
                            goto runCanceled;
                        await Task.Delay(pollingDelay);
                    }
                }
                catch (Exception ex) { errorMessage(ex); }
            }
        runCanceled:;
        }

        private void clearMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        #endregion

        #region Mem-Functions
        private bool Connect()
        {
            if (ProcessNames.Count() != 0)
                foreach (string processName in ProcessNames.ToArray())
                {
                    _processHandle = OpenProcess(_PROCESS_ALL_ACCESS, false, Process.GetProcessesByName(processName)[0].Id);
                    _windowHandle = Process.GetProcessesByName(processName)[0].MainWindowHandle;
                    if (_processHandle != IntPtr.Zero && _windowHandle != IntPtr.Zero)
                    {
                        Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Game connected! \nHandles: P:" + _processHandle.ToString("X8") + " W:" + _windowHandle.ToString("X8") + "\n").Wait();
                        memAddresses.Clear();

                        SYSTEM_INFO SI;
                        GetSystemInfo(out SI);
                        IntPtr MaxAddress = SI.lpMaximumApplicationAddress;
                        IntPtr currentAddress = SI.lpMinimumApplicationAddress;

                        do
                        {
                            MEMORY_BASIC_INFORMATION64 mbi;
                            int result = VirtualQueryEx(_processHandle, (IntPtr)currentAddress, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64)));
                            if (currentAddress == (IntPtr)((ulong)mbi.BaseAddress + mbi.RegionSize))
                                break;
                            currentAddress = (IntPtr)((ulong)mbi.BaseAddress + mbi.RegionSize);
                            if ((mbi.Type & _MEM_PRIVATE) != 0 && (mbi.State & _MEM_COMMIT) != 0)
                            {
                                memSpace temp = new memSpace();
                                temp.baseAddress = (IntPtr)mbi.BaseAddress;
                                temp.endAddress = (IntPtr)currentAddress;
                                memAddresses.Add(temp);
                            }

                        } while ((ulong)currentAddress <= (ulong)MaxAddress);
                        return true;
                    }
                }
            return false;
        }

        private bool LoadAddresses()
        {
            _timeAddress = IntPtr.Zero;
            _dataAddress = IntPtr.Zero;

            string timePattern =
                "?? ?? ?? ?? ?? ?? ?? ?? " +
                "00 00 00 00 00 00 00 00 " +
                "03 00 00 00 ?? ?? ?? ?? " +
                "?? ?? ?? ?? ?? ?? ?? ?? " +
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? ?? 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 " +
                "03 00 00 00 ?? ?? ?? ??";

            _timeAddress = ScanMemoryBMH(timePattern);

            if (_timeAddress != IntPtr.Zero) { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Time hooked! \nAddress: " + _timeAddress.ToString("X8") + "\n").Wait(); }
            else { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Time not found \n").Wait(); }

            string dataPattern =
                "?? 00 00 00 00 00 00 00 " +        //Selector Story/Mod  0/1
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? 00 00 00 00 00 00 00 " +        //Selector StoryLevelPack(Ark 1-4)/ModLevelPack
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? 00 00 00 00 00 00 00 " +        //Selector StoryLevel
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? 00 00 00 00 00 00 00 " +        //Selector StoryDifficulty
                "00 00 00 00 00 00 00 00 " +
                "01 00 00 00 ?? ?? ?? ?? " +
                "00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? 00 00 00 00 00 00 00 " +        //Selector ModLevelPack
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? 00 00 00 00 00 00 00 " +        //Selector ModLevel
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? 00 00 00 00 00 00 00";          //Selector ModDifficulty
            _dataAddress = ScanMemoryBMH(dataPattern);

            if (_dataAddress != IntPtr.Zero) { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Data hooked! \nAddress: " + _dataAddress.ToString("X8") + "\n").Wait(); }
            else { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Data not found - try restarting the game if this happenes again after pressing reload\n").Wait(); }

            if (_timeAddress == IntPtr.Zero + 192 && _dataAddress == IntPtr.Zero)
            {
                Notes_Textblock.Text += "Failed to find any mem-address!\n";
                return false;
            }

            return true;
        }

        private IntPtr ScanMemoryBMH(string pattern)
        {
            IntPtr Found = IntPtr.Zero;
            ParallelLoopResult result = Parallel.ForEach(memAddresses, (range, state) =>
            {
                IntPtr Address = range.baseAddress;
                byte[] buffer = new byte[(UInt64)range.endAddress - (UInt64)range.baseAddress];
                ReadProcessMemory(_processHandle, Address, buffer, buffer.Length, out _);

                List<int> Adresses = Algos.BoyerMooreHorspool.SearchPattern(buffer, pattern, 0);
                if (Adresses.Count != 0)
                {
                    Found = range.baseAddress + Adresses[0];
                    state.Stop();
                    return;
                }
                if (state.IsStopped)
                    return;
            });
            if (!result.IsCompleted)
                return Found;
            else
                return IntPtr.Zero;
        }
        #endregion

        #region Helper functions
        private string GetTypeName(string filePath)
        {
            return Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(filePath)))));
        }
        private string GetLevelPackName(string filePath)
        {
            return Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(filePath))));
        }
        private string GetLevelName(string filePath)
        {
            return Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(filePath)));
        }
        private string GetSettingsPath(string filePath)
        {
            return Path.GetDirectoryName(filePath) + "\\settings.cfg";
        }
        private string GetMetaPath(string filePath)
        {
            return Path.GetDirectoryName(filePath) + "\\meta.cfg";
        }
        private string GetActPath(string filePath)
        {
            return Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(filePath))) + "\\act.cfg";
        }
        #endregion

        #region Dll-imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void GetSystemInfo(out SYSTEM_INFO Info);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        #endregion

        private void errorMessage(Exception ex)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Something has gone wrong." + "\n\n" + "Details:" + "\n" + ex,
                                 "An error has occured!",
                                 MessageBoxButton.OK,
                                 MessageBoxImage.Error);
        }
        //private void Message(string message)
        //{
        //    MessageBoxResult result = System.Windows.MessageBox.Show(message, "Info!", MessageBoxButton.OK, MessageBoxImage.Information);
        //}
    }
}

namespace Algos
{
    /// <summary>
    /// Pattern Scanning Implementation using the Boyer Moore Horspool Algorithm https://gist.github.com/LeagueRaINi/64449336c3c1d6003c94a1d04cc3dd0b
    /// </summary>
    public static class BoyerMooreHorspool
    {
        /// <summary>
        /// Creates the Skip Table
        /// </summary>
        /// <param name="patternTuple">Tuple containing the Pattern Bytes and the Pattern Wildcard as Bool Array</param>
        /// <returns>Skip Table Array</returns>
        private static int[] CreateMatchingsTable((byte, bool)[] patternTuple)
        {
            var skipTable = new int[256];
            var wildcards = patternTuple.Select(x => x.Item2).ToArray();
            var lastIndex = patternTuple.Length - 1;

            var diff = lastIndex - Math.Max(Array.LastIndexOf(wildcards, false), 0);
            if (diff == 0)
            {
                diff = 1;
            }

            for (var i = 0; i < skipTable.Length; i++)
            {
                skipTable[i] = diff;
            }

            for (var i = lastIndex - diff; i < lastIndex; i++)
            {
                skipTable[patternTuple[i].Item1] = lastIndex - i;
            }

            return skipTable;
        }

        /// <summary>
        /// Searches for a Pattern in a Byte Array
        /// </summary>
        /// <param name="data">Our Haystack</param>
        /// <param name="pattern">Pattern in the Code format (24 50 53 31 ?? 00)</param>
        /// <param name="offset">Offset that gets add to the Addresses</param>
        /// <returns>List of Addresses it found</returns>
        public static List<int> SearchPattern(byte[] data, string pattern, int offset = 0x0)
        {
            if (!data.Any() || string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Data or Pattern is empty");
            }

            var patternTuple = pattern.Split(' ')
                .Select(hex => hex.Contains('?')
                    ? (byte.MinValue, false)
                    : (Convert.ToByte(hex, 16), true))
                .ToArray();

            if (data.Length < pattern.Length)
            {
                throw new ArgumentException("Data cannot be smaller than the Pattern");
            }

            var lastPatternIndex = patternTuple.Length - 1;
            var skipTable = CreateMatchingsTable(patternTuple);
            var adressList = new List<int>();

            for (var i = 0; i <= data.Length - patternTuple.Length; i += Math.Max(skipTable[data[i + lastPatternIndex] & 0xFF], 1))
            {
                for (var j = lastPatternIndex; !patternTuple[j].Item2 || data[i + j] == patternTuple[j].Item1; --j)
                {
                    if (j == 0)
                    {
                        adressList.Add(i + offset);
                        break;
                    }
                }
            }

            return adressList;
        }
    }
}
