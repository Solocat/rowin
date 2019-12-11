using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace rowin
{
    /// <summary>
    /// Interaction logic for FormattedTextBlock.xaml
    /// </summary>
    public partial class FormattedTextBlock : UserControl, INotifyPropertyChanged
    {
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty =
           DependencyProperty.Register("Text",
               typeof(string),
               typeof(FormattedTextBlock));


        public IEnumerable<Inline> Inlines 
        {
            get { return (IEnumerable<Inline>)GetValue(InlinesProperty); } 
            set { SetValue(InlinesProperty, value);  AllText.Inlines.Add(new Run("heihei")); OnPropertyChanged("Inlines"); Trace.WriteLine(value.ToList().Count); } 
        }

        public static readonly DependencyProperty InlinesProperty =
            DependencyProperty.Register("Inlines",
                typeof(IEnumerable<Inline>),
                typeof(FormattedTextBlock));

        public List<string> TextFragments { get; set; }

        

        //public string Text { get { return _Text; } set { Trace.WriteLine(value); _Text = value; OnPropertyChanged("Text"); } }
        //private string _Text { get; set; }

        public void SetFragments(List<string> frags)
        {
            /*AllText.Inlines.Clear();
            foreach (var frag in frags)
            {
                AllText.Inlines.Add(frag);
            }*/
        }

        public FormattedTextBlock()
        {
            //DataContext = this;
            InitializeComponent();

            Inlines = new List<Inline>();

            //AllText.Inlines.Add(new Run("heihei"));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(property));
        }
    }
}
