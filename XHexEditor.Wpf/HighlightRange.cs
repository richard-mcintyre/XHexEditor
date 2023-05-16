using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using XHexEditor.Providers;

namespace XHexEditor.Wpf
{
    public record HighlightRange(long StartIndex /*inclusive*/, long EndIndex /*inclusive*/, Brush Brush) 
        : ProviderRange(StartIndex, EndIndex)
    {
        public HighlightRange(ProviderRange range, Brush brush)
            : this(range.StartIndex, range.EndIndex, brush)
        {
        }
    }

}
