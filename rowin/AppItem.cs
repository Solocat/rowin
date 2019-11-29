﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace rowin
{
    public class AppItem : INotifyPropertyChanged
    {
        private BitmapSource ExtractIconFromFile(string file)
        {
            var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(file);
            var icon = Imaging.CreateBitmapSourceFromHIcon(
                sysicon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );
            return icon;
        }

        public string FilePath { get; set; }
        public string Name { get { return Path.GetFileNameWithoutExtension(FilePath); } }

        public BitmapSource Icon { get {
                if (_Icon == null) _Icon = ExtractIconFromFile(FilePath);
                return _Icon;
                        } 
        }
        private BitmapSource _Icon { get; set; }

        public bool IsVisible { get { return CharsBeforeToken != -1; } }

        public int GiveOrder(string text)
        {
            if (String.IsNullOrEmpty(text)) CharsBeforeToken = 0;

            CharsBeforeToken = Name.IndexOf(text, StringComparison.OrdinalIgnoreCase);

            if (CharsBeforeToken != -1)
            {
                TextAfterToken = Name.Substring(CharsBeforeToken + text.Length);
                Token = Name.Substring(CharsBeforeToken, text.Length);
            }

            
            

            OnPropertyChanged("TextBeforeToken");
            OnPropertyChanged("Token");
            OnPropertyChanged("TextAfterToken");

            return CharsBeforeToken;
        }
        public int CharsBeforeToken { get { return _CharsBeforeToken; } set { _CharsBeforeToken = value; OnPropertyChanged("CharsBeforeToken"); OnPropertyChanged("IsVisible"); } }

        private int _CharsBeforeToken { get; set; }

        public string TextBeforeToken { get { return CharsBeforeToken != -1 ? Name.Substring(0, CharsBeforeToken) : Name; } }


        public string Token { get; set; }
        public string TextAfterToken { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(property));
        }
    }
}