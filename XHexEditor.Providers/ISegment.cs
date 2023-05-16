using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    internal interface ISegment
    {
        /// <summary>
        /// Gets the number of bytes in this segment
        /// </summary>
        long GetLength();

        /// <summary>
        /// Copies the bytes in this segment to the specified buffer
        /// </summary>
        /// <param name="sourceIndex">Starting byte index to copy in this segment</param>
        /// <param name="destBuffer">Buffer to copy bytes into</param>
        /// <param name="destIndex">Index to copy bytes to in the destination buffer</param>
        /// <param name="count">Number of bytes to copy</param>
        void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count);

        /// <summary>
        /// Copies the bytes in this segment to the specified buffer
        /// </summary>
        /// <param name="sourceIndex">Starting byte index to copy in this segment</param>
        /// <param name="destBuffer">Buffer to copy bytes into</param>
        /// <param name="destIndex">Index to copy bytes to in the destination buffer</param>
        /// <param name="count">Number of bytes to copy</param>
        /// <param name="modifications">Information about changes that have been made to the requested byte range</param>
        void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count, ProviderModifications? modifications);

        /// <summary>
        /// Splits this segment into two segments
        /// </summary>
        /// <param name="atIndex">Index where the split will occur, the byte at the index will be returned in the second segment</param>
        ISegment[] Split(long atIndex);

        /// <summary>
        /// Creates a new segment containing the data for this segment but where the offset is increased by the specified amount
        /// </summary>
        ISegment IncreaseOffset(long byAmount);

        /// <summary>
        /// Creates a new segment containing the data for this segment by where the length is decreased by the specified amount
        /// </summary>
        ISegment DecreaseLength(long byAmount);
    }

}
