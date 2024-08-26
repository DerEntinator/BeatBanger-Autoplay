using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using WindowsInput;
using WindowsInput.Native;

namespace BeatBanger_Autoplay
{
    public class Note
    {
        public int key;
        public double timestamp;
        public int modifier;
        public double holdtime;
    }
    public class Keyvent
    {
        public VirtualKeyCode key;
        public long timestamp;
        public bool down;
    }
    public class ConfigFile
    {
        public string filepath;
        public string level;
        public string levelPack;
        public string type;
    }

    public partial class MainWindow : Window
    {
        InputSimulator InputSimulator = new InputSimulator();
        Stopwatch stopWatch = new Stopwatch();

        ObservableCollection<string> levelpack_Collection = new ObservableCollection<string>();
        ObservableCollection<string> level_Collection = new ObservableCollection<string>();
        ObservableCollection<string> difficulty_Collection = new ObservableCollection<string>();

        List<ConfigFile> fileList = new List<ConfigFile>();
        JObject notesJSON;
        List<Keyvent> timesheet = new List<Keyvent>();

        bool stopTask = false;

        #region GlobalHotkey https://stackoverflow.com/questions/11377977/global-hotkeys-in-wpf-working-from-every-window

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(
    [In] IntPtr hWnd,
    [In] int id,
    [In] uint fsModifiers,
    [In] uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(
            [In] IntPtr hWnd,
            [In] int id);

        private HwndSource _sourceStart;
        private HwndSource _sourceStop;
        private const int HOTKEY_ID_START = 9000;
        private const int HOTKEY_ID_STOP = 9001;
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helperStart = new WindowInteropHelper(this);
            _sourceStart = HwndSource.FromHwnd(helperStart.Handle);
            _sourceStart.AddHook(HwndHook);
            var helperStop = new WindowInteropHelper(this);
            _sourceStop = HwndSource.FromHwnd(helperStop.Handle);
            _sourceStop.AddHook(HwndHook);
            RegisterHotKey();
        }
        protected override void OnClosed(EventArgs e)
        {
            _sourceStart.RemoveHook(HwndHook);
            _sourceStart = null;
            _sourceStop.RemoveHook(HwndHook);
            _sourceStop = null;
            UnregisterHotKey();
            base.OnClosed(e);
        }
        private void RegisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            const uint VK_F10 = 0x79;
            const uint VK_F11 = 0x7A;
            if (!RegisterHotKey(helper.Handle, HOTKEY_ID_START, 0, VK_F10) || !RegisterHotKey(helper.Handle, HOTKEY_ID_STOP, 0, VK_F11))
            {
                // handle error
            }
        }
        private void UnregisterHotKey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID_START);
            UnregisterHotKey(helper.Handle, HOTKEY_ID_STOP);
        }
        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    switch (wParam.ToInt32())
                    {
                        case HOTKEY_ID_START:
                            OnHotKeyPressedStart();
                            handled = true;
                            break;
                        case HOTKEY_ID_STOP:
                            OnHotKeyPressedStop();
                            handled = true;
                            break;
                    }
                    break;
            }
            return IntPtr.Zero;
        }
        private void OnHotKeyPressedStart()
        {
            run();
        }
        private void OnHotKeyPressedStop()
        {
            stopTask = true;
        }
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.Multiselect = false;
            dialog.Title = "Select Game Folder";
            dialog.InitialDirectory = Assembly.GetEntryAssembly().Location;
            ///Ask user for File selection///
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                GameFolder_TextBox.Text = dialog.FileName;
            }
            else
            {
                return;
            }
            Reload();
        }

        private void FolderSelect_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                CommonOpenFileDialog dialog = new CommonOpenFileDialog();
                dialog.IsFolderPicker = true;
                dialog.Multiselect = false;
                ///Ask user for File selection///
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    GameFolder_TextBox.Text = dialog.FileName;
                }
                else
                {
                    return;
                }
                Reload();
            }
            catch (Exception ex) { }
        }

        private void Source_Changer(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void Levelpack_Changer(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                level_Collection.Clear();

                foreach (ConfigFile entry in fileList)
                {
                    if ((bool)Story_Radio.IsChecked)
                    {
                        if (entry.type == Story_Radio.Tag.ToString())
                            if (entry.levelPack == Levelpack_List.SelectedItem.ToString())
                                level_Collection.Add(entry.level);
                    }
                    else if ((bool)Mods_Radio.IsChecked)
                    {
                        if (entry.type == Mods_Radio.Tag.ToString())
                            if (entry.levelPack == Levelpack_List.SelectedItem.ToString())
                                level_Collection.Add(entry.level);
                    }
                }

                Level_List.ItemsSource = level_Collection.Distinct();
                difficulty_Collection.Clear();
                Difficulty_List.ItemsSource = difficulty_Collection;
                Difficulty_List.SelectedIndex = -1;
            }
            catch (Exception ex) { }
        }

        private void Levels_Changer(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                difficulty_Collection.Clear();

                string level = "";
                if ((bool)Story_Radio.IsChecked)
                {
                    level = fileList
                        .FindAll(x => x.type == Story_Radio.Tag.ToString())
                        .FindAll(x => x.levelPack == Levelpack_List.SelectedItem.ToString())
                        .Find(x => x.level == Level_List.SelectedItem.ToString()).filepath;
                }
                else if ((bool)Mods_Radio.IsChecked)
                {
                    level = fileList
                        .FindAll(x => x.type == Mods_Radio.Tag.ToString())
                        .FindAll(x => x.levelPack == Levelpack_List.SelectedItem.ToString())
                        .Find(x => x.level == Level_List.SelectedItem.ToString()).filepath;
                }

                string config = File.ReadAllText(level);

                //Jsonify
                config = config.Replace("\n", "").Replace("\r", "").Replace("[main]data=", "{\"data\":") + "}";

                notesJSON = JObject.Parse(config);

                List<string> difficulties = notesJSON["data"]["charts"].Children().Values<string>("name").ToList();

                foreach (string entry in difficulties.ToArray().Distinct())
                    difficulty_Collection.Add(entry);

                Difficulty_List.ItemsSource = difficulties;
                Difficulty_List.SelectedIndex = -1;
            }
            catch (Exception ex) { }
        }

        private void Difficulty_Changer(object sender, SelectionChangedEventArgs e)
        {
            LoadNotes();
        }
        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            Reload();
        }
        private void Start_Click(object sender, RoutedEventArgs e)
        {
            run();
        }
        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            stopTask = true;
        }

        private void Key_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadNotes();
        }


        private void Reload()
        {
            try
            {
                levelpack_Collection.Clear();
                Levelpack_List.ItemsSource = level_Collection;
                Levelpack_List.SelectedIndex = -1;
                fileList.Clear();

                List<string> tempList = Directory.GetFiles(GameFolder_TextBox.Text, "notes.cfg", SearchOption.AllDirectories).ToList();

                foreach (string file in tempList)
                {
                    ConfigFile temp = new ConfigFile();
                    temp.filepath = file;
                    temp.level = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(file)));
                    temp.levelPack = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file))));
                    temp.type = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(file)))));
                    fileList.Add(temp);
                }

                foreach (ConfigFile entry in fileList)
                {
                    if ((bool)Story_Radio.IsChecked)
                    {
                        if (entry.type == Story_Radio.Tag.ToString())
                            levelpack_Collection.Add(entry.levelPack);
                    }
                    else if ((bool)Mods_Radio.IsChecked)
                    {
                        if (entry.type == Mods_Radio.Tag.ToString())
                            levelpack_Collection.Add(entry.levelPack);
                    }
                }

                Levelpack_List.ItemsSource = levelpack_Collection.Distinct();

                level_Collection.Clear();
                Level_List.ItemsSource = level_Collection;
                Level_List.SelectedIndex = -1;
                difficulty_Collection.Clear();
                Difficulty_List.ItemsSource = difficulty_Collection;
                Difficulty_List.SelectedIndex = -1;
            }
            catch (Exception ex) { }
        }
        private void LoadNotes()
        {
            try
            {
                Notes_Textblock.Text = "";
                timesheet.Clear();

                List<JToken> rawNotes = notesJSON["data"]["charts"][Difficulty_List.SelectedIndex]["notes"].ToList();

                double offset = rawNotes[0].Value<double>("timestamp");
                foreach (JToken note in rawNotes)
                {
                    Keyvent pressKey = new Keyvent();
                    pressKey.down = true;
                    Keyvent holdRelease = new Keyvent();
                    holdRelease.down = false;

                    switch (note.Value<int>("input_type"))
                    {
                        case 0:
                            pressKey.key = (VirtualKeyCode)Key1.Text.ToUpper()[0];
                            holdRelease.key = (VirtualKeyCode)Key1.Text.ToUpper()[0];
                            break;
                        case 1:
                            pressKey.key = (VirtualKeyCode)Key2.Text.ToUpper()[0];
                            holdRelease.key = (VirtualKeyCode)Key2.Text.ToUpper()[0];
                            break;
                        case 2:
                            pressKey.key = (VirtualKeyCode)Key3.Text.ToUpper()[0];
                            holdRelease.key = (VirtualKeyCode)Key3.Text.ToUpper()[0];
                            break;
                        case 3:
                            pressKey.key = (VirtualKeyCode)Key4.Text.ToUpper()[0];
                            holdRelease.key = (VirtualKeyCode)Key4.Text.ToUpper()[0];
                            break;
                    }

                    pressKey.timestamp = (long)((note.Value<double>("timestamp") - offset) * 1000.0);
                    if (note.Value<double>("hold_end_timestamp") != 0.0)
                    {
                        holdRelease.timestamp = (long)((note.Value<double>("hold_end_timestamp") - offset) * 1000.0);
                    }
                    else
                        holdRelease.timestamp = (long)((note.Value<double>("timestamp") - offset) * 1000.0 + 30.0);

                    timesheet.Add(pressKey);
                    timesheet.Add(holdRelease);

                }

                timesheet.Sort((s1, s2) => s1.timestamp.CompareTo(s2.timestamp));

                string outputText = "";
                foreach (Keyvent kvent in timesheet)
                    outputText += "Key:" + kvent.key + " Down:" + kvent.down + " Ts:" + kvent.timestamp + "\n";
                Notes_Textblock.Text = outputText;

            }
            catch (Exception ex) { }
        }

        public async Task run()
        {
            stopTask = false;
            int delay = Convert.ToInt32(PollingRate.Text);
            var cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            Task playing = Task.Run(async () =>
            {
                stopWatch.Restart();
                VirtualKeyCode buffer = VirtualKeyCode.CANCEL;
                for (int n = 0; n < timesheet.Count; n++)
                {
                    while ((stopWatch.ElapsedMilliseconds <= timesheet[n].timestamp))
                    {
                        buffer = timesheet[n].key;
                        if (stopTask)
                            cts.Cancel(); token.ThrowIfCancellationRequested();
                        await Task.Delay(delay);
                    }
                    if (timesheet[n].down)
                        InputSimulator.Keyboard.KeyDown(buffer);
                    else
                        InputSimulator.Keyboard.KeyUp(buffer);
                }

                InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_Y);
                InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_X);
                InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_C);
                InputSimulator.Keyboard.KeyUp(VirtualKeyCode.VK_V);

                stopWatch.Stop();
            }
                , token);
            await Task.Yield();
        }
    }
}
