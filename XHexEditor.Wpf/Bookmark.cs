using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Wpf
{
    public record Bookmark(long ByteIndex, string? Name);
}
