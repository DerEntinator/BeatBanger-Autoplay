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
using System.Windows.Input;
using WindowsInput;
using WindowsInput.Native;

namespace BeatBanger_Autoplay
{
    public class Keyvent
    {
        public VirtualKeyCode key;
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
        InputSimulator InputSimulator = new InputSimulator();

        List<ConfigFile> fileList = new List<ConfigFile>();
        string currentLevel = "";
        double levelDelay = 0.0;
        List<string> difficulties = new List<string>();
        JObject notesJSON;
        List<List<Keyvent>> timesheet = new List<List<Keyvent>>();

        string gameFolder = "";
        string keybindingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Godot\\app_userdata\\Beat Banger\\binds.sav";
        VirtualKeyCode key1 = VirtualKeyCode.VK_Z;
        VirtualKeyCode key2 = VirtualKeyCode.VK_X;
        VirtualKeyCode key3 = VirtualKeyCode.VK_C;
        VirtualKeyCode key4 = VirtualKeyCode.VK_V;
        VirtualKeyCode key5 = VirtualKeyCode.VK_Q;
        VirtualKeyCode key6 = VirtualKeyCode.VK_E;

        int pollingDelay = 1;

        const uint _PROCESS_ALL_ACCESS = 0x1fffff;
        const uint _MEM_PRIVATE = 0x20000;
        const uint _MEM_COMMIT = 0x1000;
        private static readonly string[] ProcessNames = { "beatbanger" };
        int oldPID = 0;
        private static IntPtr _processHandle;
        List<memSpace> memAddresses = new List<memSpace>();
        private static IntPtr _timeAddress;
        private static IntPtr _dataAddress;

        public MainWindow()
        {
            InitializeComponent();

            ReloadKeys();
            try
            {
                Reload().Wait();
            }
            catch { }

            Task.Run(() =>
            {
                getLevel();
            });

            Task.Run(() =>
            {
                run();
            });

        }

        #region UI-Functions
        private void FolderSelect_Click(object sender, MouseButtonEventArgs e)
        { try { loadFolder(); } catch { } }

        private void Reload_Click(object sender, RoutedEventArgs e)
        { try { Reload().Wait(); } catch { } }
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
                    dialog.IsFolderPicker = true;
                    dialog.Multiselect = false;
                    dialog.Title = "Select game folder!";
                    ///Ask user for File selection///
                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    { gameFolder = dialog.FileName; return true; }
                    else
                    { gameFolder = "ERROR! - double-click me"; return false; }
                }
            }
            else { return true; }
        }

        private async Task Reload()
        {
            Notes_Textblock.Text = "";

            ReloadKeys();
            if (!loadFolder())
                Notes_Textblock.Text = "Game folder not found - Reload again!";

            fileList.Clear();
            List<string> tempList = Directory.GetFiles(gameFolder, "notes.cfg", SearchOption.AllDirectories).ToList();

            foreach (string file in tempList)
            {
                ConfigFile temp = new ConfigFile();
                temp.filepath = file;
                temp.level = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(file)));
                temp.levelPack = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file))));
                temp.type = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file)))));
                fileList.Add(temp);
            }



            foreach (string processName in ProcessNames)
            {
                if (Process.GetProcessesByName(processName).Length == 0) { Notes_Textblock.Text = "Game is not launched!"; return; }

                if (oldPID == 0 || oldPID != Process.GetProcessesByName(processName)[0].Id || _processHandle == IntPtr.Zero || _timeAddress == IntPtr.Zero || _dataAddress == IntPtr.Zero)
                {
                    oldPID = Process.GetProcessesByName(processName)[0].Id;
                    // try to get process handle
                    if (!Connect()) { Notes_Textblock.Text += "Failed to get Handle!\n"; }
                    else LoadAddresses();
                }
                else Notes_Textblock.Text += "Everything is connected and hooked :)\n";
            }
        }

        private void ReloadKeys()
        {
            try
            {
                string bindings = "";
                bindings = File.ReadAllText(keybindingsPath);
                bindings = bindings.Replace("\n", "").Replace("\r", "");
                bindings = "{\"registered_keys\":" + bindings.Remove(0, bindings.IndexOf("{") - 1);
                bindings = bindings.Remove(bindings.LastIndexOf("}") + 2) + "}";
                JObject bindingsObj = JObject.Parse(bindings);
                var binds = bindingsObj["registered_keys"].Children().ToList();

                key1 = (VirtualKeyCode)binds[0]["keycode"].Value<int>();
                key2 = (VirtualKeyCode)binds[1]["keycode"].Value<int>();
                key3 = (VirtualKeyCode)binds[2]["keycode"].Value<int>();
                key4 = (VirtualKeyCode)binds[3]["keycode"].Value<int>();
                key5 = (VirtualKeyCode)binds[4]["keycode"].Value<int>();
                key6 = (VirtualKeyCode)binds[5]["keycode"].Value<int>();

                LoadNotes();
            }
            catch { }
        }

        private async Task getLevel()
        {
            while (true)
            {
                try
                {
                    string oldLastAccess = currentLevel;

                    DateTime lastAccess = DateTime.MinValue;
                    foreach (var file in fileList)
                    {
                        if (lastAccess < File.GetLastAccessTimeUtc(file.filepath))
                        {
                            currentLevel = file.filepath;
                            lastAccess = File.GetLastAccessTimeUtc(file.filepath);
                        }
                    }

                    Dispatcher.BeginInvoke(() => Level_Textblock.Text = "Level: " + Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(currentLevel))));
                    if (oldLastAccess != currentLevel)
                    {
                        string config = File.ReadAllText(currentLevel);

                        // Jsonify
                        config = config.Replace("\n", "").Replace("\r", "").Replace("[main]data=", "{\"data\":") + "}";

                        notesJSON = JObject.Parse(config);
                        difficulties.Clear();
                        difficulties = notesJSON["data"]["charts"].Children().Values<string>("name").ToList();

                        // Get level starting offset
                        string settings = currentLevel.Replace("notes.cfg", "settings.cfg");
                        settings = File.ReadAllText(settings);
                        settings = settings.Replace("\n", "").Replace("\r", "");
                        settings = "{\"data\":" + settings.Remove(0, settings.IndexOf("{"));
                        settings = settings.Remove(settings.LastIndexOf("}") + 1) + "}";
                        JObject settingsObj = JObject.Parse(settings);
                        levelDelay = settingsObj["data"]["song_offset"].Value<double>();

                        await LoadNotes();

                    }
                    await Task.Delay(100);
                }
                catch { }
            }
        }

        private async Task LoadNotes()
        {
            timesheet.Clear();
            //Dispatcher.Invoke(() => { Notes_Textblock.Text = ""; });
            if (notesJSON != null)
            {
                for (int i = 0; i < difficulties.Count; i++)
                {
                    List<JToken> rawNotes = notesJSON["data"]["charts"][i]["notes"].ToList();
                    List<Keyvent> keys = new List<Keyvent>();
                    foreach (JToken note in rawNotes)
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

                        pressKey.timestamp = ((note.Value<double>("timestamp") + levelDelay) * 1000.0);
                        if (note.Value<double>("hold_end_timestamp") != 0.0)
                            holdRelease.timestamp = ((note.Value<double>("hold_end_timestamp") + levelDelay) * 1000.0);
                        else
                            holdRelease.timestamp = ((note.Value<double>("timestamp") + levelDelay) * 1000.0 + 30.0);

                        keys.Add(pressKey);
                        keys.Add(holdRelease);
                    }

                    keys.Sort((s1, s2) => s1.timestamp.CompareTo(s2.timestamp));
                    string outputText = "Difficulty: " + difficulties[i] + "\n";
                    for (int n = 0; n < 10; n++)
                    {
                        Keyvent kvent = keys[n];
                        outputText += "Key:" + kvent.key + " Down:" + kvent.down + " Ts:" + Math.Round(kvent.timestamp, 0) + "\n";
                    }
                    //Dispatcher.Invoke(() => { Notes_Textblock.Text += outputText; });
                    timesheet.Add(keys);
                }
            }
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
                    ReadProcessMemory(_processHandle, _timeAddress, timeBuffer, sizeof(double), out _);
                    timeRead = BitConverter.ToDouble(timeBuffer) * 1000.0;

                    if (timeRead > 0 && timesheet.Count != 0 && timeRead < timesheet[0].Last().timestamp)
                    {
                        ReadProcessMemory(_processHandle, _dataAddress, dataBuffer, 22 * 8 * sizeof(byte), out _);

                        if (dataBuffer[0 * 8] == 0)
                            currentDifficulty = dataBuffer[9 * 8];
                        else currentDifficulty = dataBuffer[21 * 8];

                        VirtualKeyCode buffer = VirtualKeyCode.CANCEL;
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

                            if (timesheet[currentDifficulty][n].down)
                                InputSimulator.Keyboard.KeyDown(buffer);
                            else
                                InputSimulator.Keyboard.KeyUp(buffer);
                        }
                    restartLevel:
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_Y);
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_X);
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_C);
                        InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_V);
                    }

                    await Task.Delay(pollingDelay);
                }
                catch { }
            }
        }
        #endregion

        #region Mem-Functions
        private bool Connect()
        {
            foreach (string processName in ProcessNames)
            {
                _processHandle = OpenProcess(_PROCESS_ALL_ACCESS, false, Process.GetProcessesByName(processName)[0].Id);
                if (_processHandle != IntPtr.Zero)
                {
                    Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Game connected! \nHandle: " + _processHandle.ToString("X8") + "\n");
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

            string timePattern = "?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 ?? ?? ?? ?? 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 1C 00 00 00 ?? ?? ?? ??";
            _timeAddress = ScanMemoryBMH(timePattern) - 192;

            if (_timeAddress != IntPtr.Zero) { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Time hooked! \nAddress: " + _timeAddress.ToString("X8") + "\n"); }
            else { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Time not found \n"); }

            string dataPattern =
                "?? 00 00 00 00 00 00 00 " +        //Selector Story/Mod  0/1
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? ?? ?? " +
                "?? 00 00 00 00 00 00 00 " +        //Selector StoryLevelPack(Ark 1-4)/ModLevelPack
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? 00 00 00 " +
                "?? 00 00 00 00 00 00 00 " +        //Selector StoryLevel
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? 00 00 00 " +
                "?? 00 00 00 00 00 00 00 " +        //Selector StoryDifficulty
                "00 00 00 00 00 00 00 00 " +
                "01 00 00 00 ?? 00 00 ?? " +
                "00 00 00 00 00 00 00 00 " +
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? 00 00 ?? " +
                "?? 00 00 00 00 00 00 00 " +        //Selector ModLevelPack
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? ?? 00 00 " +
                "?? 00 00 00 00 00 00 00 " +        //Selector ModLevel
                "00 00 00 00 00 00 00 00 " +
                "02 00 00 00 ?? 00 00 00 " +
                "?? 00 00 00 00 00 00 00";          //Selector ModDifficulty
            _dataAddress = ScanMemoryBMH(dataPattern);

            if (_dataAddress != IntPtr.Zero) { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Data hooked! \nAddress: " + _dataAddress.ToString("X8") + "\n"); }
            else { Dispatcher.BeginInvoke(() => Notes_Textblock.Text += "Data not found - try restarting the game if this happenes again after pressing reload\n"); }

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

        #region Dll-imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void GetSystemInfo(out SYSTEM_INFO Info);
        #endregion

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
