using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    public class ProviderByteEventArgs : EventArgs
    {
        public long AtIndex { get; init; }
    }
}
