using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XHexEditor.Wpf.App
{
    public static class HexEditorAppCommands
    {
        public static RoutedCommand OpenRecentItem { get; } = new RoutedCommand();

    }
}
