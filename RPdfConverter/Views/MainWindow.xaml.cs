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
using Ninject;

namespace PDFConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Closing += MainWindow_Closing;
            //using (IKernel kb = new StandardKernel()){}
        }

        //void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        //{
        //    ((ViewModel.ViewModel)this.DataContext).StopWorker();
        //}
        //protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        //{
        //    ((ViewModel.ViewModel)this.DataContext).StopWorker();
        //    base.OnClosing(e);
        //}
    }
}
