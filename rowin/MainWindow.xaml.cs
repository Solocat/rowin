using System;
using System.Windows;
using System.Windows.Interop;
using System.IO;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Controls;

namespace rowin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr Handle { get; set; }

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
                Icon = Properties.Resources.rowin,
                Visible = true,
                Text = "Rowin",
                ContextMenu = menu
            };
            TrayIcon.DoubleClick += delegate (object sender, EventArgs args) { FromTray(); };

            GetFiles(Properties.Settings.Default.customFolder);

            Handle = new WindowInteropHelper(this).EnsureHandle();
            HookHotkey();
            AcrylicBlur.EnableBlur(0x64, 0, Handle);
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

            UnHookHotkey();
        }

        private void Window_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            this.Activate();
            InputBox.Focus();
        }
    }
}
