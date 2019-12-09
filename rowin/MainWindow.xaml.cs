﻿using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Controls;
using System.Configuration;

namespace rowin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref AcrylicBlur.WindowCompositionAttributeData data);

        [DllImport("User32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("User32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private HwndSource _source;
        private const int HOTKEY_ID = 9001;

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == HOTKEY_ID)
            {
                if (this.Visibility == Visibility.Visible) ToTray();
                else FromTray();
                handled = true;
            }
            return IntPtr.Zero;
        }

        internal void EnableBlur(uint opacity, uint color)
        {
            var windowHelper = new WindowInteropHelper(this);

            var accent = new AcrylicBlur.AccentPolicy();
            accent.AccentState = AcrylicBlur.AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = (opacity << 24) | (color & 0xFFFFFF); /* BGR color format */


            var accentStructSize = Marshal.SizeOf(accent);

            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new AcrylicBlur.WindowCompositionAttributeData();
            data.Attribute = AcrylicBlur.WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);

            Marshal.FreeHGlobal(accentPtr);
        }

        public ObservableCollection<AppItem> AppList { get; set; }

        public ICollectionView AppListView { get; private set; }

        public ICollectionViewLiveShaping AppListLive { get; set; }

        public string InputText { get { return _InputText; } set { _InputText = value; FilterAndSort(value); } }
        private string _InputText { get; set; }

        private System.Windows.Forms.NotifyIcon TrayIcon { get; set; }

        public MainWindow()
        {
            AppList = new ObservableCollection<AppItem>();
            AppListView = CollectionViewSource.GetDefaultView(AppList);
            AppListView.Filter = app => ((AppItem)app).IsVisible == true;
            AppListView.SortDescriptions.Add(
                new SortDescription("CharsBeforeToken", ListSortDirection.Ascending));
            AppListView.SortDescriptions.Add(
                new SortDescription("Name", ListSortDirection.Ascending));

            AppListLive = AppListView as ICollectionViewLiveShaping;
            AppListLive.LiveFilteringProperties.Add("IsVisible");
            AppListLive.IsLiveFiltering = true;

            AppListLive.LiveSortingProperties.Add("CharsBeforeToken");
            AppListLive.LiveSortingProperties.Add("Name");
            AppListLive.IsLiveSorting = true;

            DataContext = this;
            InitializeComponent();

            //make tray icon
            var menu = new System.Windows.Forms.ContextMenu();
            menu.MenuItems.Add("Close", (s, e) => this.Close());
            menu.MenuItems.Add("Set custom folder", SetCustomFolder);
            TrayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon("rowin.ico"),
                Visible = true,
                Text = "Rowin",
                ContextMenu = menu
            };
            TrayIcon.DoubleClick += delegate (object sender, EventArgs args) { FromTray(); };

            //GetFiles(ConfigurationManager.AppSettings["customFolder"]);
            GetFiles(Properties.Settings.Default.customFolder);

            //hook hotkey
            var handle = new WindowInteropHelper(this).EnsureHandle();
            _source = HwndSource.FromHwnd(handle);
            _source.AddHook(HwndHook);
            RegisterHotKey(handle, HOTKEY_ID, 0x001, 0x20);

            EnableBlur(0x64, 0);
        }

        private void GetFiles(string path)
        {
            AppList.Clear();

            var files = Directory.GetFiles(@Environment.GetFolderPath(Environment.SpecialFolder.Desktop)).ToList();
            files.AddRange(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)));
            if (!String.IsNullOrEmpty(path)) files.AddRange(Directory.GetFiles(path));
            foreach (var file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (File.GetAttributes(file).HasFlag(FileAttributes.Hidden) || String.IsNullOrEmpty(name))
                {
                    continue;
                }
                AppList.Add(new AppItem()
                {
                    FilePath = file,
                });
            }
            InputText = String.Empty; //force filter
            InputBox.Focus();
        }

        private void SetCustomFolder(object sender, EventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                GetFiles(dialog.SelectedPath);

                Properties.Settings.Default.customFolder = dialog.SelectedPath;
                Properties.Settings.Default.Save();
            }
        }

        public void FilterAndSort(string text)
        {
            foreach (var app in AppList)
            {
                app.GiveOrder(text);
            }
            //AppListView.Refresh();
        }

        public void FromTray()
        {
            InputText = String.Empty;
            InputBox.Text = string.Empty;
            InputBox.Focus();
            AppContainer.SelectedIndex = -1;

            this.Show();
            this.Activate();
        }


        private void ToTray()
        {
            this.Hide();
        }

        private void StartSelected()
        {
            if (AppContainer.SelectedItem != null)
            {
                Process.Start((AppContainer.SelectedItem as AppItem).FilePath);
                //Application.Current.Shutdown();
                ToTray();
            }
        }

        private void FocusApps()
        {
            if (InputBox.IsFocused && AppContainer.Items.Count > 0)
            {
                if (AppContainer.SelectedItem == null)
                {
                    AppContainer.Focus();
                }
                else
                {
                    var listBoxItem = (ListBoxItem)AppContainer.ItemContainerGenerator.ContainerFromItem(AppContainer.SelectedItem);
                    listBoxItem.Focus();
                }
            }
        }

        private void FocusText(System.Windows.Input.Key key)
        {
            InputBox.Focus();
        }

        private void AppContainer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            StartSelected();
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Enter: StartSelected(); e.Handled = true; break;
                case System.Windows.Input.Key.Escape: ToTray(); break;
                case System.Windows.Input.Key.Down: FocusApps(); break;
                case System.Windows.Input.Key.Up: FocusApps(); break;
                case System.Windows.Input.Key.Left: FocusApps(); break;
                case System.Windows.Input.Key.Right: FocusApps(); break;
                case System.Windows.Input.Key.Tab: FocusApps(); break;
                default: InputBox.Focus(); break;
            }
        }

        private void InputBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // Prohibit non-alphabetic

            /*Regex r = new Regex(@"^[a-zA-Z]+$");

            return r.IsMatch(s);

            if (!IsAlphabetic(e.Text))
                e.Handled = true;*/
            //Trace.WriteLine("input");
        }

        private void AdminStart_Click(object sender, RoutedEventArgs e)
        {
            if (AppContainer.SelectedItem != null)
            {

                var info = new ProcessStartInfo
                {
                    FileName = (AppContainer.SelectedItem as AppItem).FilePath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                if (info.Verbs.Contains("runas"))
                {
                    Process.Start(info);
                    ToTray();

                }
                else StartSelected();
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Window is loaded and then hidden, to make first render instant. Window is moved offscreen to prevent flashing.

            this.Height = SystemParameters.PrimaryScreenHeight * 0.67;
            this.Width = SystemParameters.PrimaryScreenWidth * 0.67;
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);

            this.Hide();

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TrayIcon.Icon.Dispose();
            TrayIcon.Dispose();

            _source.RemoveHook(HwndHook);
            _source = null;
            UnregisterHotKey(new WindowInteropHelper(this).Handle, HOTKEY_ID);
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Activate();
            InputBox.Focus();
        }
    }
}
