using System;
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

namespace rowin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref AcrylicBlur.WindowCompositionAttributeData data);

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

        public void FilterAndSort(string text)
        {
            foreach (var app in AppList)
            {
                app.GiveOrder(text);
            }
            //AppListView.Refresh();
        }

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

            var files = Directory.GetFiles(@Environment.GetFolderPath(Environment.SpecialFolder.Desktop)).ToList();
            files.AddRange(Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory)));
            files.AddRange(Directory.GetFiles(@"C:\Users\olli.myllymaki\Documents"));
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

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon
            {
                Icon = new System.Drawing.Icon("Icon.ico"),
                Visible = true
            };
            ni.Click += delegate (object sender, EventArgs args)
            {
                this.Show();
                //this.WindowState = WindowState.Normal;
            };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur(0x64, 0);
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
            //if (AppContainer.SelectedIndex < 4) //top row
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
            Trace.WriteLine("input");
        }
    }
}
