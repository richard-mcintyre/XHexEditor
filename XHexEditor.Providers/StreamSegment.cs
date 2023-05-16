using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    internal class StreamSegment : ISegment
    {
        #region Construction

        public StreamSegment(Stream stream, long offset, long length)
        {
            _stream = stream;
            _offset = offset;
            _length = length;
        }

        #endregion

        #region Fields

        private readonly Stream _stream;
        private readonly long _offset;
        private readonly long _length;

        #endregion

        #region Methods

        public long GetLength() => _length;

        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count) =>
            CopyTo(sourceIndex, destBuffer, destIndex, count, null);

        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count, ProviderModifications? modifications)
        {
            _stream.Seek(_offset + sourceIndex, SeekOrigin.Begin);
            _stream.Read(destBuffer, destIndex, count);
        }

        public ISegment[] Split(long atIndex)
        {
            List<ISegment> list = new List<ISegment>();

            long index1 = _offset;
            long length1 = atIndex;
            if (length1 > 0)
                list.Add(new StreamSegment(_stream, index1, length1));

            long index2 = _offset + atIndex;
            long length2 = _length - length1;
            if (length2 > 0)
                list.Add(new StreamSegment(_stream, index2, length2));

            return list.ToArray();
        }

        public ISegment IncreaseOffset(long byAmount) =>
            new StreamSegment(_stream, (_offset + byAmount), (_length - byAmount));

        public ISegment DecreaseLength(long byAmount) =>
            new StreamSegment(_stream, _offset, (_length - byAmount));

        #endregion
    }

}
