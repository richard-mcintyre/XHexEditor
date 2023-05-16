using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shell;
using XTaskDialog;

namespace XHexEditor.Wpf.App
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs args)
        {
            base.OnStartup(args);

            if (JumpList.GetJumpList(Application.Current) == null)
            {
                JumpList.SetJumpList(Application.Current, new JumpList() { ShowRecentCategory = true });
            }

            string[] commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
            MainWindow wnd = (commandLineArgs.Length == 1) ? new MainWindow(commandLineArgs[0]) : new MainWindow();
            wnd.Show();
        }
    }
}
