﻿using Microsoft.WindowsAPICodePack.Dialogs;
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
        string keybindingsPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Godot\\app_userdata\\Beat Banger\\binds.sav";
        Keys key1 = Keys.Z;
        Keys key2 = Keys.X;
        Keys key3 = Keys.C;
        Keys key4 = Keys.V;
        Keys key5 = Keys.Q;
        Keys key6 = Keys.E;
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
                getLevel();     //getLevel() - ernative level loading
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
                string bindings = "";
                bindings = File.ReadAllText(keybindingsPath);
                bindings = bindings.Replace("\n", "").Replace("\r", "");
                bindings = "{\"registered_keys\":" + bindings.Remove(0, bindings.IndexOf("{") - 1);
                bindings = bindings.Remove(bindings.LastIndexOf("}") + 2) + "}";
                JObject bindingsObj = JObject.Parse(bindings);
                var binds = bindingsObj["registered_keys"].Children().ToList();

                key1 = (Keys)binds[0]["keycode"].Value<int>();
                key2 = (Keys)binds[1]["keycode"].Value<int>();
                key3 = (Keys)binds[2]["keycode"].Value<int>();
                key4 = (Keys)binds[3]["keycode"].Value<int>();
                key5 = (Keys)binds[4]["keycode"].Value<int>();
                key6 = (Keys)binds[5]["keycode"].Value<int>();

                LoadNotes();
            }
            catch (Exception ex) { errorMessage(ex); }
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

                            string config = File.ReadAllText(currentLevel.filepath);
                            if (config.Contains("charts"))
                            {
                                string settings = File.ReadAllText(GetSettingsPath(currentLevel.filepath));
                                settings = settings.Replace("\n", "").Replace("\r", "");
                                settings = "{\"data\":" + settings.Remove(0, settings.IndexOf("{"));
                                settings = settings.Remove(settings.LastIndexOf("}") + 1) + "}";
                                JObject settingsObj = JObject.Parse(settings);

                                levelDelay = settingsObj["data"]["song_offset"].Value<double>() * 1000.0;

                                string meta = File.ReadAllText(GetMetaPath(currentLevel.filepath));
                                meta = meta.Replace("\n", "").Replace("\r", "");
                                meta = "{\"data\":" + meta.Remove(0, meta.IndexOf("{"));
                                meta = meta.Remove(meta.LastIndexOf("}") + 1) + "}";
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
