using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using Microsoft.Win32;
using XHexEditor.Providers;
using XHexEditor.Wpf.App.Dialogs;
using XTaskDialog;
using SR = XHexEditor.Wpf.App.Properties.Resources;

namespace XHexEditor.Wpf.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Dependency properties

        public static readonly DependencyProperty OpenedFileNameProperty =
            DependencyProperty.Register(nameof(OpenedFileName), typeof(string), typeof(MainWindow));

        #endregion

        #region Construction

        public MainWindow()
            : this(String.Empty)
        {
        }

        public MainWindow(string openFileName)
        {
            InitializeComponent();

            if (String.IsNullOrEmpty(openFileName) == false)
            {
                this.Dispatcher.BeginInvoke(() => OpenFile(openFileName));
            }

            this.DataContext = this;
        }

        #endregion

        #region Properties

        public string ApplicationTitle => SR.ApplicationTitle;

        public string OpenedFileName
        {
            get => (string)GetValue(OpenedFileNameProperty);
            set => SetValue(OpenedFileNameProperty, value);
        }

        #endregion

        #region Methods

        private void OnFileMenuItem_Opened(object sender, RoutedEventArgs args)
        {
            MenuItem menu = (MenuItem)sender;

            UpdateRecentMenuItems(
                menu.Items.OfType<MenuItem>().Where(o => o.Command == HexEditorAppCommands.OpenRecentItem),
                AppData.RecentFiles);
        }

        private void UpdateRecentMenuItems(IEnumerable<MenuItem> items, string[] fileNames)
        {
            for (int i = 0; i < items.Count(); i++)
            {
                MenuItem item = items.ElementAt(i);

                if (i < fileNames.Length)
                {
                    item.Visibility = Visibility.Visible;
                    item.Header = $"_{i+1}. {System.IO.Path.GetFileName(fileNames[i])}";
                    item.CommandParameter = fileNames[i];
                }
                else
                {
                    item.Visibility = Visibility.Collapsed;
                }                   
            }

            recentItemsMenuSeparator.Visibility = fileNames.Any() ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OpenFile(string fileName)
        {
            try
            {
                FileStream fileStream = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

                SetProvider(new StreamProvider(fileStream), fileName);

                /////////

                AppData.AddToRecentFiles(fileName);

                JumpList.AddToRecentCategory(new JumpTask()
                {
                    Title = System.IO.Path.GetFileName(fileName),
                    Arguments = $"\"{fileName}\"",
                    Description = fileName,
                });

                JumpList.GetJumpList(Application.Current).Apply();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private static IProvider CreateMemoryMappedFileProvider(long size)
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, size);
            MemoryMappedViewStream stream = mmf.CreateViewStream(0, size, MemoryMappedFileAccess.ReadWrite);

            return new StreamProvider(stream);
        }

        private void SetProvider(IProvider provider, string fileName)
        {
            editor.Provider?.Dispose();
            editor.Provider = provider;

            this.OpenedFileName = fileName;
        }

        /// <summary>
        /// Called when the user attempts to close the window
        /// </summary>
        protected override void OnClosing(CancelEventArgs args)
        {
            // If there are unsaved changes, we need to prompt the user
            if (editor.IsModified)
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);

                string fileName = String.IsNullOrWhiteSpace(this.OpenedFileName) ? SR.Untitled : System.IO.Path.GetFileName(this.OpenedFileName);

                TaskDialog dlg = new TaskDialog(helper.Handle,
                    SR.DoYouWantToSaveYourWorkQMark,
                    String.Format(SR.ThereAreUnsavedChangesInParam, fileName),
                    TaskDialogButton.None);
                dlg.WindowTitle = SR.ApplicationTitle;
                dlg.CustomButtons = new TaskDialogCustomButton[]
                {
                    new TaskDialogCustomButton((int)TaskDialogResult.Yes, SR.Save),
                    new TaskDialogCustomButton((int)TaskDialogResult.No, SR.DontSave),
                    new TaskDialogCustomButton((int)TaskDialogResult.Cancel, SR.Cancel)
                };

                TaskDialogResult result = dlg.Show();

                switch (result)
                {
                    case TaskDialogResult.Yes:
                        args.Cancel = !SaveChanges(prompt: false);
                        break;

                    case TaskDialogResult.No:
                        break;

                    case TaskDialogResult.Cancel:
                    default:
                        args.Cancel = true;
                        break;
                }
            }
            
            base.OnClosing(args);
        }

        private bool SaveChanges(bool prompt)
        {
            if (String.IsNullOrEmpty(this.OpenedFileName))
                prompt = true;  // We dont know the file name to save to, so we need to prompt

            if (prompt)
            {
                if (PromptForSaveFileName(out string fileName))
                    return SaveToNewFileName(fileName);
            }
            else
            {
                return SaveInPlace();
            }

            return false;
        }

        private bool SaveInPlace()
        {
            try
            {
                WindowInteropHelper helper = new WindowInteropHelper(this);

                ProgressDialog dlg = new ProgressDialog(helper.Handle, String.Empty, String.Empty);
                dlg.WindowTitle = this.ApplicationTitle;

                string fileName = System.IO.Path.GetFileName(this.OpenedFileName);

                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    ProgressDialogProgressInfo progress = new ProgressDialogProgressInfo();

                    Task task = editor.SaveChangesAsync(cts.Token, new Progress<ApplyChangesProgressInfo>(o =>
                    {
                        switch (o.Status)
                        {
                            case ApplyChangesStatus.Preparing:
                                progress.MainInstruction = String.Format(SR.SavingParamDotDotDot, fileName);
                                progress.Content = SR.PreparingDotDotDot;
                                break;

                            case ApplyChangesStatus.Applying:
                                {
                                    if (o.BytesRemaining > 0)
                                        progress.Content = String.Format(SR.ParamBytesRemaining, o.BytesRemaining);
                                }
                                break;
                        }
                    }));

                    dlg.Show(task, cts, progress);
                }

                return true;
            }
            catch (OperationCanceledException)
            { }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return false;
        }

        private bool SaveToNewFileName(string fileName)
        {
            try
            {
                using (FileStream fileStream = File.Create(fileName))
                {
                    using (Stream providerStream = editor.Provider.AsStream())
                    {
                        using (CancellationTokenSource cts = new CancellationTokenSource())
                        {
                            Task task = providerStream.CopyToAsync(fileStream, cts.Token);

                            WindowInteropHelper helper = new WindowInteropHelper(this);
                            string justFileName = System.IO.Path.GetFileName(fileName);

                            ProgressDialog.Show(task, helper.Handle, String.Format(SR.SavingParamDotDotDot, justFileName), String.Empty, cts, null);
                        }
                    }
                }

                OpenFile(fileName);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return true;
        }

        private bool PromptForSaveFileName(out string fileName)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.FileName = this.OpenedFileName;
            dlg.Filter = SR.FileFilter_AllFiles;

            bool result = dlg.ShowDialog().GetValueOrDefault();
            fileName = result ? dlg.FileName! : String.Empty;

            return result;
        }

        #endregion

        #region Command handlers

        private void OnNew_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            NewDlg dlg = new NewDlg(this);

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                if (editor.Provider != null)
                    editor.Provider.Dispose();

                try
                {
                    IProvider provider;
                    switch (dlg.NewSize)
                    {
                        case NewDlg.NewSizeEnum.OneKilobyte:
                            provider = CreateMemoryMappedFileProvider(1024);
                            break;

                        case NewDlg.NewSizeEnum.OneMegabyte:
                            provider = CreateMemoryMappedFileProvider(1024 * 1024);
                            break;

                        case NewDlg.NewSizeEnum.OneGigabyte:
                            provider = CreateMemoryMappedFileProvider(1024 * 1024 * 1024);
                            break;

                        case NewDlg.NewSizeEnum.FourGigabytes:
                            provider = CreateMemoryMappedFileProvider(1024L * 1024 * 1024 * 4);
                            break;

                        case NewDlg.NewSizeEnum.EightGigabytes:
                            provider = CreateMemoryMappedFileProvider(1024L * 1024 * 1024 * 8);
                            break;

                        case NewDlg.NewSizeEnum.Empty:
                        default:
                            provider = new StreamProvider(new MemoryStream());
                            break;
                    }

                    SetProvider(provider, String.Empty);
                }
                catch (Exception e)
                {
                    SetProvider(new StreamProvider(new MemoryStream()), String.Empty);

                    MessageBox.Show(e.Message);
                }
            }
        }

        private void OnOpen_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                OpenFile(dlg.FileName);
            }
        }

        private void OnOpenRecentItem_Executed(object sender, ExecutedRoutedEventArgs args) =>
            OpenFile(args.Parameter.ToString()!);

        private void OnClose_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            Application.Current.Shutdown();
        }

        private void OnSave_Executed(object sender, ExecutedRoutedEventArgs args) => SaveChanges(prompt: false);

        private void OnSaveAs_Executed(object sender, ExecutedRoutedEventArgs args) => SaveChanges(prompt: true);

        #endregion

        #region Events

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        #endregion
    }
}
