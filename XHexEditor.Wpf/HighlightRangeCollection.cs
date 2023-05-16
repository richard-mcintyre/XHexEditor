using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XHexEditor.Providers;

namespace XHexEditor.Wpf
{
    public class HighlightRangeCollection : ObservableCollection<HighlightRange>
    {
        public HighlightRange? GetFirstThatContainsIndex(long index)
        {
            foreach (HighlightRange range in this)
            {
                if (range.IsInRange(index))
                    return range;
            }

            return null;
        }

    }
}
