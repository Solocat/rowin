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
                if (name == "desktop" || String.IsNullOrEmpty(name)) continue;
                AppList.Add(new AppItem()
                {
                    FilePath = file,
                });
            }
            InputText = String.Empty; //force filter

            InputBox.Focus();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            EnableBlur(0x64, 0);
        }

       

        private void StartSelected()
        {
            if (AppContainer.SelectedItem != null)
            {
                Process.Start((AppContainer.SelectedItem as AppItem).FilePath);
                Application.Current.Shutdown();
                //this.Hide();
            }
        }

        private void FocusApps(System.Windows.Input.Key key)
        {
            if (InputBox.IsFocused && AppContainer.Items.Count > 0)
            {
                if (AppContainer.SelectedIndex == -1 && key != System.Windows.Input.Key.Up) //no selection
                {
                    AppContainer.SelectedIndex = 0;
                }
                else 
                {
                    switch (key) //force move selection
                    {
                        case System.Windows.Input.Key.Down:
                            if (AppContainer.SelectedIndex + 4 < AppContainer.Items.Count) AppContainer.SelectedIndex += 4;
                            break;
                        case System.Windows.Input.Key.Up:
                            if (AppContainer.SelectedIndex >= 4) AppContainer.SelectedIndex -= 4;
                            break;
                        case System.Windows.Input.Key.Left:
                            if (AppContainer.SelectedIndex % 4 != 0) AppContainer.SelectedIndex--;
                            break;
                        case System.Windows.Input.Key.Right:
                            if (AppContainer.SelectedIndex % 4 != 3 && AppContainer.SelectedIndex + 1 < AppContainer.Items.Count) AppContainer.SelectedIndex++;
                            break;
                        default: break;
                    }
                }

                var listBoxItem = (ListBoxItem)AppContainer.ItemContainerGenerator.ContainerFromItem(AppContainer.SelectedItem);
                listBoxItem.Focus();
            }
        }

        private void FocusText(System.Windows.Input.Key key)
        {
            //if (AppContainer.SelectedIndex < 4) //top row
            InputBox.Focus();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Down: break;
                case System.Windows.Input.Key.Up: break;
                case System.Windows.Input.Key.Left: break;
                case System.Windows.Input.Key.Right: break;
                case System.Windows.Input.Key.Enter: break;
                case System.Windows.Input.Key.Escape: Application.Current.Shutdown(); break;
                default: FocusText(e.Key); break;
            }
            //e.Handled = true;
        }

        

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                //case System.Windows.Input.Key.Enter: StartSelected(); break;
                //case System.Windows.Input.Key.Escape: Application.Current.Shutdown(); break;
                case System.Windows.Input.Key.Down: FocusApps(e.Key); break;
                case System.Windows.Input.Key.Up: FocusApps(e.Key); break;
                case System.Windows.Input.Key.Left: FocusApps(e.Key); break;
                case System.Windows.Input.Key.Right: FocusApps(e.Key); break;
                default: break;
            }
            //e.Handled = true;
        }

        private void AppContainer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            StartSelected();
        }

        private void AppContainer_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Point pt = e.GetPosition(this);
            System.Windows.Media.VisualTreeHelper.HitTest(this, pt);
        }
    }
}
