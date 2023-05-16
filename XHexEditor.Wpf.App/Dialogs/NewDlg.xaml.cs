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
using System.Windows.Shapes;

namespace XHexEditor.Wpf.App.Dialogs
{
    /// <summary>
    /// Interaction logic for NewDlg.xaml
    /// </summary>
    public partial class NewDlg : DialogBase
    {
        #region NewSizeEnum

        public enum NewSizeEnum
        {
            Empty,
            OneKilobyte,
            OneMegabyte,
            OneGigabyte,
            FourGigabytes,
            EightGigabytes,
        }

        #endregion

        #region Construction

        public NewDlg(Window parent)
            : base(parent)
        {
            InitializeComponent();

            this.DataContext = this;
        }

        #endregion

        #region Fields

        private NewSizeEnum _newSize = NewSizeEnum.Empty;

        #endregion

        #region Properties

        public bool IsSizeEmpty
        {
            get => _newSize == NewSizeEnum.Empty;
            set
            {
                if (value)
                    _newSize = NewSizeEnum.Empty;
            }
        }

        public bool IsSizeOneKilobyte
        {
            get => _newSize == NewSizeEnum.OneKilobyte;
            set
            {
                if (value)
                    _newSize = NewSizeEnum.OneKilobyte;
            }
        }

        public bool IsSizeOneMegabyte
        {
            get => _newSize == NewSizeEnum.OneMegabyte;
            set
            {
                if (value)
                    _newSize = NewSizeEnum.OneMegabyte;
            }
        }

        public bool IsSizeOneGigabyte
        {
            get => _newSize == NewSizeEnum.OneGigabyte;
            set
            {
                if (value)
                    _newSize = NewSizeEnum.OneGigabyte;
            }
        }

        public bool IsSizeFourGigabytes
        {
            get => _newSize == NewSizeEnum.FourGigabytes;
            set
            {
                if (value)
                    _newSize = NewSizeEnum.FourGigabytes;
            }
        }

        public bool IsSizeEightGigabytes
        {
            get => _newSize == NewSizeEnum.EightGigabytes;
            set
            {
                if (value)
                    _newSize = NewSizeEnum.EightGigabytes;
            }
        }

        public NewSizeEnum NewSize => _newSize;

        #endregion
    }
}
