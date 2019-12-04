using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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

        public List<string> Words { get {
                var words = new List<string>();
                var list = Name.Split(' ', '-', '_').ToList();
                foreach (var w in list) {
                    if (!String.IsNullOrEmpty(w)) words.Add(w);
                }
                return words;
            }
        }

        public List<char> Acronym { get {
                var list = new List<char>();
                foreach (var w in Words) {
                    if (!String.IsNullOrEmpty(w)) list.Add(w.ToCharArray()[0]);
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

        public List<bool> BoldCharacters { get; set; }

        public TextBlock Text { get; set; }

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
                }
            }
            if (shorthand)
            {
                
            }

            if (!shorthand)
            {
                CharsBeforeToken = Name.IndexOf(text, StringComparison.OrdinalIgnoreCase);

                if (CharsBeforeToken != -1)
                {
                    TextAfterToken = Name.Substring(CharsBeforeToken + text.Length);
                    Token = Name.Substring(CharsBeforeToken, text.Length);

                    TextFragments.Add(Name.Substring(0, CharsBeforeToken));
                    TextFragments.Add(Name.Substring(CharsBeforeToken, text.Length));
                    TextFragments.Add(Name.Substring(CharsBeforeToken + text.Length));

                    Text = new TextBlock();
                    Text.Inlines.Add(new Run(Name.Substring(0, CharsBeforeToken)));
                    Text.Inlines.Add(new Bold(new Run(Name.Substring(CharsBeforeToken, text.Length))));
                    Text.Inlines.Add(new Run(Name.Substring(CharsBeforeToken + text.Length)));
                }
            }

            foreach (var f in TextFragments)
            {
                //Trace.WriteLine(f);
            }

            OnPropertyChanged("TextBeforeToken");
            OnPropertyChanged("Token");
            OnPropertyChanged("TextAfterToken");

            return CharsBeforeToken;
        }
        public int CharsBeforeToken { get { return _CharsBeforeToken; } set { _CharsBeforeToken = value; OnPropertyChanged("IsVisible"); } }

        private int _CharsBeforeToken { get; set; }

        public string TextBeforeToken { get { return CharsBeforeToken != -1 ? Name.Substring(0, CharsBeforeToken) : Name; } }


        public string Token { get; set; }
        public string TextAfterToken { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(property));
        }
    }
}
