using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Ookii.Dialogs.Wpf;
using Ninject;
using Ninject.Parameters;

namespace PDFConverter.ViewModel
{
    class ViewModel : ObservableObject
    {
        private DelegateCommand _ExportDataCommand = null;
        private DelegateCommand _ExtractCommand = null;
        private DelegateCommand _SplitCommand = null;

        public BackgroundWorker BWorker = null;

        //private IKernel Kernel = new StandardKernel();
        
        // Cancel background worker
        public void StopWorker()
        {
            if ((BWorker != null) && (BWorker.WorkerSupportsCancellation) && BWorker.IsBusy)
            {
                try 
                {
                    BWorker.CancelAsync();
                    BWorker.Dispose();
                }
                catch { }
            }
            return;
        }

        // Get main thread dispatcher
        private static Dispatcher MainThreadDispatcher = Dispatcher.CurrentDispatcher;

        // Call method on main thread using dispatcher
        private void CallMethodOnMainThread(Action method)
        {
            MainThreadDispatcher.Invoke(DispatcherPriority.Normal, method);
        }

        // Return true if main background worker is available
        private Boolean CanWork()
        {
            if (BWorker == null) { return true; }
            if (!BWorker.IsBusy) { return true; }
            return false;
        }

        // Ctor
        public ViewModel()
        {
            // Stop background worker and save path properties in config file when main window is being closed
            App.appclosingEventHandler += delegate(object sender)
            {
                if (PdfFile.isFilePathOK(".pdf")) { Model.Config.AddSettingFor(Model.Config.PdfFile, PdfFile); }
                if (EditOutputPath.isDirectoryPathOK()) { Model.Config.AddSettingFor(Model.Config.EditOutputPath, EditOutputPath); }
                if (WPsToExtractFile.isFilePathOK(".txt")) { Model.Config.AddSettingFor(Model.Config.WPsToExtractFile, WPsToExtractFile); }
                if (ExportFile.isFilePathOK(".txt")) { Model.Config.AddSettingFor(Model.Config.ExportFile, ExportFile); }

                StopWorker();
            };

            MainThreadDispatcher = Dispatcher.CurrentDispatcher;

            //Application.Current.MainWindow.Closing += delegate(object sender, System.ComponentModel.CancelEventArgs e)
            //{
            //    StopWorker();
            //};

            //App.Kernel = new StandardKernel();

            App.Kernel.Bind<Model.ISplitter>().To<Model.Splitter>().InTransientScope();//.Named("Splitter");
            App.Kernel.Bind<Model.IExtractor>().To<Model.Extractor>().InTransientScope();//.Named("Extractor");
            App.Kernel.Bind<Model.IDataExporter>().To<Model.DataExporter>().InTransientScope();//.Named("DataExporter");
        }

        /********************/
        /* Bound Properties */
        /********************/

        private String _PdfFile = Model.Config.ReadSettingFor(Model.Config.PdfFile) ?? "PDF File Path";
        public String PdfFile
        {
            get { return _PdfFile; }
            set { _PdfFile = value; RaisePropertyChangedEvent("PdfFile"); }
        }

        private String _EditOutputPath = Model.Config.ReadSettingFor(Model.Config.EditOutputPath) ?? "Output Path";
        public String EditOutputPath
        {
            get { return _EditOutputPath; }
            set { _EditOutputPath = value; RaisePropertyChangedEvent("EditOutputPath"); }
        }

        private String _WPsToExtractFile = Model.Config.ReadSettingFor(Model.Config.WPsToExtractFile) ?? "File with WPs to extract";
        public String WPsToExtractFile
        {
            get { return _WPsToExtractFile; }
            set { _WPsToExtractFile = value; RaisePropertyChangedEvent("WPsToExtractFile"); }
        }

        private String _ExportFile = Model.Config.ReadSettingFor(Model.Config.ExportFile) ?? "File to export info into";
        public String ExportFile
        {
            get { return _ExportFile; }
            set { _ExportFile = value; RaisePropertyChangedEvent("ExportFile"); }
        }

        private Int32 _ExtractProgress;
        public Int32 ExtractProgress
        {
            get { return _ExtractProgress; }
            set { _ExtractProgress = value; RaisePropertyChangedEvent("ExtractProgress"); }
        }

        private Int32 _SplitProgess;
        public Int32 SplitProgress
        {
            get { return _SplitProgess; }
            set { _SplitProgess = value; RaisePropertyChangedEvent("SplitProgress"); }
        }

        private Int32 _ExportProgess;
        public Int32 ExportProgress
        {
            get { return _ExportProgess; }
            set { _ExportProgess = value; RaisePropertyChangedEvent("ExportProgress"); }
        }

        /*******************/
        /* Bound ICommands */
        /*******************/

        public ICommand SplitCommand
        {
            get { return _SplitCommand ?? (_SplitCommand = new DelegateCommand((n) => this.Split(), (s) => CanWork())); }
        }
        public ICommand ExtractCommand
        {
            get { return _ExtractCommand ?? (_ExtractCommand = new DelegateCommand((n) => this.Extract(), (s) => CanWork())); }
        }
        public ICommand ExportDataCommand
        {
            get { return _ExportDataCommand ?? (_ExportDataCommand = new DelegateCommand((n) => this.ExportData(), (s) => CanWork())); }
        }

        /***********/
        /* Methods */
        /***********/

        private void Split()
        {
            SplitProgress = 0;

            using (BWorker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true })
            {
                BWorker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    CallMethodOnMainThread(delegate { _SplitCommand.RaiseCanExecuteChanged(); });

                    Model.ISplitter splitter = App.Kernel.Get<Model.ISplitter>(
                                                    new IParameter[] { 
                                                        new ConstructorArgument("inPathPdf", PdfFile),
                                                        new ConstructorArgument("inBw", sender as BackgroundWorker),
                                                        new ConstructorArgument("inOutputFolder", EditOutputPath)}
                                                    );

                    e.Result = splitter.Split();
                };
                BWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    CallMethodOnMainThread(() => _SplitCommand.RaiseCanExecuteChanged());
                    
                    // Show result in path text box
                    //EditOutputPath = (String)e.Result;
                    
                    Utils.ShowProcessFinishedDialog(e.Result.ToString());
                };
                BWorker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    SplitProgress = e.ProgressPercentage;
                };

                BWorker.RunWorkerAsync();
            }
        }

        private void Extract()
        {
            ExtractProgress = 0;

            using (BWorker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true })
            {               
                BWorker.DoWork += delegate(object sender, DoWorkEventArgs e) 
                {
                    CallMethodOnMainThread(() => _ExtractCommand.RaiseCanExecuteChanged());

                    Model.IExtractor extractor = App.Kernel.Get<Model.IExtractor>(
                                                    new IParameter[] { 
                                                       new ConstructorArgument("inPathPdf", PdfFile),
                                                       new ConstructorArgument("inBw", sender as BackgroundWorker),
                                                       new ConstructorArgument("inOutputFolder", EditOutputPath),
                                                       new ConstructorArgument("inWpFile", WPsToExtractFile)}
                                                    );

                    e.Result = extractor.Extract();
                };

                BWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    CallMethodOnMainThread(() => _ExtractCommand.RaiseCanExecuteChanged());
                    //EditOutputPath = (String)e.Result;
                    Utils.ShowProcessFinishedDialog((String)e.Result);
                };
                BWorker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    ExtractProgress = e.ProgressPercentage;
                };

                BWorker.RunWorkerAsync();
            }
        }

        private void ExportData()
        {
            ExportProgress = 0;

            using (BWorker = new BackgroundWorker() { WorkerReportsProgress = true, WorkerSupportsCancellation = true })
            {
                BWorker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    CallMethodOnMainThread(() => _ExportDataCommand.RaiseCanExecuteChanged());

                    Model.IDataExporter exporter = App.Kernel.Get<Model.IDataExporter>(   
                                                    new IParameter[] { 
                                                       new ConstructorArgument("inPathPdf", PdfFile),
                                                       new ConstructorArgument("inBw", sender as BackgroundWorker),
                                                       new ConstructorArgument("inExportFile", ExportFile)}
                                                    );

                    e.Result = exporter.ExportData();
                };

                BWorker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    CallMethodOnMainThread(() => _ExportDataCommand.RaiseCanExecuteChanged());

                    String result  = (String)e.Result;

                    #region Show result dialog using ookii dialogs
                    //TaskDialog d = new TaskDialog() { Content = result, WindowTitle = "Processing Complete" };
                    //d.Buttons.Add(new TaskDialogButton("Copy to clipboard and cancel"));
                    //d.Buttons.Where((s) => s.ButtonType == ButtonType.Custom).First().Owner.ButtonClicked +=
                    //    (object sender2, TaskDialogItemClickedEventArgs ea) =>
                    //        { System.Windows.Clipboard.SetText(result); };
                    //d.Show();
                    #endregion

                    // show result dialog using wpf user control
                    Window window = new Window()
                    {
                        Title = "PDF processing result",
                        Topmost = true,
                        ResizeMode = ResizeMode.NoResize,
                        Content = new ResultDialog(result),
                        SizeToContent = SizeToContent.WidthAndHeight
                    };

                    window.Show();
                };

                BWorker.ProgressChanged += (object sender, ProgressChangedEventArgs e) =>
                {
                    ExportProgress = e.ProgressPercentage;
                };

                BWorker.RunWorkerAsync();
            }
        }
    }
}