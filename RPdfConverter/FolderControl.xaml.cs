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
    /// Interaction logic for FDControl.xaml
    /// </summary>
    public partial class FolderControl : UserControl
    {
        public static readonly DependencyProperty FolderPathProperty = DependencyProperty.Register(
            "FolderPath",
            typeof(String),
            typeof(FolderControl),
            new FrameworkPropertyMetadata(null));

        public String FolderPath
        {
            get { return (String)GetValue(FolderPathProperty); }
            set { SetValue(FolderPathProperty, value); }
        }

        public FolderControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Ookii.Dialogs.Wpf.VistaFolderBrowserDialog vfbd = new VistaFolderBrowserDialog();
            
            vfbd.ShowNewFolderButton = true;
            //try 
            //{
            //    if (!String.IsNullOrWhiteSpace(FolderPath) && !FolderPath.EndsWith("\\")) { vfbd.SelectedPath = FolderPath + "\\"; }
            //    else { vfbd.SelectedPath = FolderPath;  }
                
            //}
            //catch { }

            if ((Boolean)vfbd.ShowDialog() == true)
            {
                FolderPath = vfbd.SelectedPath;
            }
        }
    }
}
