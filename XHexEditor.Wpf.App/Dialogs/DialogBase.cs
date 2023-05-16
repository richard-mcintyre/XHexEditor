using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace XHexEditor.Wpf.App.Dialogs
{
    public class DialogBase : Window
    {
        #region Routed commands

        public static readonly RoutedCommand OKCommand = new RoutedCommand();
        public static readonly RoutedCommand CancelCommand = new RoutedCommand();

        #endregion

        #region Construction

        public DialogBase(Window parent)
        {
            this.Style = Application.Current.FindResource("DialogBaseStyle") as Style;

            this.Owner = parent;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            this.CommandBindings.Add(new CommandBinding(OKCommand, OnOK_Executed, OnOK_CanExecute));
            this.CommandBindings.Add(new CommandBinding(CancelCommand, OnCancel_Executed));
        }

        #endregion

        #region Methods

        protected virtual bool CanOk() => true;

        protected virtual bool OnOk() => true;

        #endregion

        #region Command handlers

        private void OnOK_CanExecute(object sender, CanExecuteRoutedEventArgs args) =>
            args.CanExecute = CanOk();

        private void OnOK_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            if (OnOk())
            {
                this.DialogResult = true;
                Close();
            }
        }

        private void OnCancel_Executed(object sender, ExecutedRoutedEventArgs args)
        {
            this.DialogResult = false;
            Close();
        }

        #endregion
    }
}
