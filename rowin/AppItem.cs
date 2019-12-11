using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace rowin
{
    public class AppItem : INotifyPropertyChanged
    {
        public AppItem()
        {
            Inlines = new ObservableCollection<Inline>();
        }

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

        public List<string> Words { get {
                var words = new List<string>();
                var list = Name.Split(' ', '-', '_').ToList();
                foreach (var w in list) {
                    if (!String.IsNullOrEmpty(w)) words.Add(w);
                }
                return words;
            }
        }

        public List<int> WordIndices { get {
                var inx = new List<int>();
                foreach (var w in Words) {
                    inx.Add(Name.IndexOf(w));
                }
                return inx;
            }
        }

        public List<char> Acronym { get {
                var list = new List<char>();
                foreach (var w in Words) {
                    if (!String.IsNullOrEmpty(w)) list.Add(w.First());
                }
                return list;
            } 
        }

        public List<string> DivideWords()
        {
            return null;
        }

        //like: V isual S tudio C ode or Micro soft Edge
        public List<string> TextFragments { get; set; }


        public ObservableCollection<Inline> Inlines { get; set; }

        public BitmapSource Icon { get {
                if (_Icon == null) _Icon = ExtractIconFromFile(FilePath);
                return _Icon;
                        } 
        }
        private BitmapSource _Icon { get; set; }

        public bool IsVisible { get { return CharsBeforeToken != -1; } }

        public int GiveOrder(string text)
        {
            CharsBeforeToken = 0;
            TextFragments = new List<string>();

            bool shorthand = false;
            if (text.Length > 1)
            {
                Inlines = new ObservableCollection<Inline>();

                shorthand = true;
                var chars = text.ToCharArray();
                for (int i = 0; i < text.Length; i++)
                {
                    if (i >= Acronym.Count || 
                        char.ToLowerInvariant(Acronym[i]) != char.ToLowerInvariant(chars[i]))
                    {
                        shorthand = false;
                        break;
                    }

                    TextFragments.Add(chars[i].ToString());
                    TextFragments.Add(Words[i].Substring(1, Words[i].Length - 1));

                    //string remaining = Words[i].Substring(1, Words[i].Length - 1);

                    string remaining;
                    if (i + 1 >= WordIndices.Count) remaining = Name.Substring(WordIndices[i] + 1);
                    else {
                        Trace.WriteLine(WordIndices[i + 1]);
                        remaining = Name.Substring(WordIndices[i] + 1, WordIndices[i + 1] - WordIndices[i] - 1); 
                    }

                    Inlines.Add(new Bold(new Run(Acronym[i].ToString())));
                    if (!string.IsNullOrEmpty(remaining))Inlines.Add(new Run(remaining));
                }

                OnPropertyChanged("Inlines");
            }
            if (shorthand)
            {

            }

            if (!shorthand)
            {
                CharsBeforeToken = Name.IndexOf(text, StringComparison.OrdinalIgnoreCase);

                if (CharsBeforeToken != -1)
                {
                    Inlines = new ObservableCollection<Inline>();

                    if (CharsBeforeToken > 0) Inlines.Add(new Run(Name.Substring(0, CharsBeforeToken)));
                    if (text.Length > 0) Inlines.Add(new Bold(new Run(Name.Substring(CharsBeforeToken, text.Length))));
                    if (Name.Length > CharsBeforeToken + text.Length) Inlines.Add(new Run(Name.Substring(CharsBeforeToken + text.Length)));


                    OnPropertyChanged("Inlines");

                  
                }
            }

            return CharsBeforeToken;
        }
        public int CharsBeforeToken { get { return _CharsBeforeToken; } set { _CharsBeforeToken = value; OnPropertyChanged("IsVisible"); } }

        private int _CharsBeforeToken { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(property));
        }
    }
}
