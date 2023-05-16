using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Wpf
{
    record HitTestResult(int HexColumn, int AsciiColumn, int HoverLine)
    {
        /// <summary>
        /// Gets the index of the byte the user clicked on
        /// </summary>
        /// <param name="verticalScrollOffset"></param>
        /// <returns></returns>
        public long GetByteIndex(int verticalScrollOffset, int bytesPerLine)
        {
            long index = ((long)verticalScrollOffset + HoverLine) * bytesPerLine;
            return index + (HexColumn != -1 ? HexColumn : AsciiColumn);
        }
    }
}
