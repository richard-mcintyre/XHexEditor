using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    internal class ArraySegment : ISegment
    {
        #region Construction

        public ArraySegment(byte[] buffer, int offset, int length)
        {
            _buffer = buffer;
            _offset = offset;
            _length = length;
        }

        #endregion

        #region Fields

        private readonly byte[] _buffer;
        private readonly int _offset;
        private readonly int _length;

        #endregion

        #region Methods

        public long GetLength() => _length;

        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count) =>
            CopyTo(sourceIndex, destBuffer, destIndex, count, null);

        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count, ProviderModifications? modifications)
        {
            modifications?.Add(sourceIndex, count);
            Array.Copy(_buffer, _offset + sourceIndex, destBuffer, destIndex, count);
        }

        public ISegment[] Split(long atIndex)
        {
            List<ISegment> list = new List<ISegment>();

            int index1 = _offset;
            int length1 = (int)atIndex;
            if (length1 > 0)
                list.Add(new ArraySegment(_buffer, index1, length1));

            int index2 = (int)(_offset + atIndex);
            int length2 = _length - length1;
            if (length2 > 0)
                list.Add(new ArraySegment(_buffer, index2, length2));

            return list.ToArray();
        }

        public ISegment IncreaseOffset(long byAmount) =>
            new ArraySegment(_buffer, (int)(_offset + byAmount), (int)(_length - byAmount));

        public ISegment DecreaseLength(long byAmount) =>
            new ArraySegment(_buffer, _offset, (int)(_length - byAmount));

        /// <summary>
        /// Combines contiguous array segments into a single array segment
        /// </summary>
        /// <param name="segments"></param>
        /// <returns></returns>
        public static ArraySegment Combine(IEnumerable<ArraySegment> segments)
        {
            int count = 0;
            int totalLength = 0;
            foreach (ArraySegment segment in segments)
            {
                totalLength += (int)segment.GetLength();
                count++;
            }

            if (count == 1)
                return segments.ElementAt(0);

            byte[] data = new byte[totalLength];

            int currentOffset = 0;
            foreach (ArraySegment segment in segments)
            {
                int segmentLength = (int)segment.GetLength();

                segment.CopyTo(0, data, currentOffset, segmentLength);

                currentOffset += segmentLength;
            }

            return new ArraySegment(data, 0, data.Length);
        }

        #endregion
    }

}
