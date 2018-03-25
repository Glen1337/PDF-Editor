using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Ninject;
using System.ComponentModel;
using Ookii.Dialogs.Wpf;

namespace PDFConverter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public delegate void appClosing(object sender);
        public static event appClosing appclosingEventHandler;

        public static IKernel Kernel;
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //MainWindow mw = new MainWindow();
            //mw.DataContext = new ViewModel.ViewModel();

            Kernel = new StandardKernel();

            Current.MainWindow = Kernel.Get<MainWindow>();
            Current.MainWindow.DataContext = Kernel.Get<ViewModel.ViewModel>();
            Current.MainWindow.Topmost = true;

            Current.MainWindow.Closing += delegate(object sender, CancelEventArgs e2) 
            {
                appclosingEventHandler.Invoke(this);
                if (!Kernel.IsDisposed) { Kernel.Dispose(); }
            };

            try
            {
                Current.MainWindow.Show();
            }
            catch(System.Exception se)
            {
                using (TaskDialog td = new TaskDialog() { WindowTitle = "Error" })
                {
                    td.Content = "Exception occured when showing window: " +
                                 se.Message +
                                 ((se.InnerException == null) ? "" : se.InnerException.Message);

                    td.Buttons.Add(new TaskDialogButton(ButtonType.Ok));
                    td.ShowDialog();
                }
            }
            return;
        }
        //protected override void OnExit(ExitEventArgs e)
        //{
        //    ((ViewModel.ViewModel)Current.MainWindow.DataContext).StopWorker();
        //    base.OnExit(e);
        //}
    }
}
