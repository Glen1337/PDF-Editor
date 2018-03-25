using System;
using System.Collections.Generic;
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
using Ookii.Dialogs.Wpf;

namespace PDFConverter
{
    /// <summary>
    /// Interaction logic for FileControl.xaml
    /// </summary>
    public partial class FileControl : UserControl
    {
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            "FilePath",
            typeof(String),
            typeof(FileControl),
            new PropertyMetadata(null));

        public FileControl()
        {
            InitializeComponent();
        }

        public String FilePath
        {
            get { return (String)GetValue(FilePathProperty); }
            set { SetValue(FilePathProperty, value); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaOpenFileDialog vofd = new VistaOpenFileDialog();

            vofd.Multiselect = false;
            vofd.Filter = "File|*.txt;*.TXT;*.pdf;*.PDF";
            vofd.CheckFileExists = true;
            vofd.CheckPathExists = true;

            // Initialize current file to one in property (previously selected file or file typed in text box)
            //try 
            //{
            //    String s = System.IO.Path.GetDirectoryName(FilePath);
            //    if (!String.IsNullOrWhiteSpace(FilePath)) vofd.InitialDirectory = s; 
            
            //}
            //catch { }

            if ((Boolean)vofd.ShowDialog() == true)
            {
                FilePath = vofd.FileName;
            }
        }
    }
}
