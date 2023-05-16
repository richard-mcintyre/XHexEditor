using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    internal class CompositeSegment : ISegment
    {
        #region Constants

        // Low number = faster to make edits but fewer edits before there will be a stackoverflow exception reading from the provider/table
        // High number = slower to make edits but more edits before there will be a stackoverflow exception reading from the provider/table
        // 
        // The stackoverflow exception is because there will be more CompositeSegments within CompositeSegments (higher depth)
        public const int IdealFillCount = 100;

        #endregion

        #region Construction

        public CompositeSegment(params ISegment[] segments)
        {
            _segments.AddRange(segments);
        }

        public CompositeSegment(IEnumerable<ISegment> segments)
        {
            _segments.AddRange(segments);
        }

        #endregion

        #region Fields

        private readonly List<ISegment> _segments = new List<ISegment>();
        private long? _length;

        #endregion

        #region Methods

        public long GetLength()
        {
            if (_length == null)
            {
                long length = 0;
                foreach (ISegment segment in _segments)
                    length += segment.GetLength();

                _length = length;
            }

            return _length.Value;
        }

        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count) =>
            CopyTo(sourceIndex, destBuffer, destIndex, count, null);

        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count, ProviderModifications? modifications)
        {
            int copiedBytes = 0;

            long currentIndex = 0;
            bool includeNextSegment = false;

            foreach (ISegment segment in _segments)
            {
                long segmentLength = segment.GetLength();
                if (segmentLength == 0)
                    continue;

                if (includeNextSegment ||
                    (sourceIndex >= currentIndex && sourceIndex < currentIndex + segmentLength))
                {
                    // starting index in the current segment
                    long sindex = includeNextSegment ? 0 : sourceIndex - currentIndex;

                    // number of bytes available in this segment to be copied (from sindex)
                    long availcount = (segmentLength - sindex);

                    // number of bytes to copy from the current segment
                    int scount = (int)Math.Min(availcount, count - copiedBytes);

                    // copy the segment bytes
                    modifications?.SetCurrentProviderOffset(currentIndex);
                    segment.CopyTo(sindex, destBuffer, destIndex + copiedBytes, scount, modifications);

                    copiedBytes += scount;

                    // if we need to copy more bytes, then those would come from the next segment
                    includeNextSegment = (count - copiedBytes) > 0;

                    // If we dont need to include the next segment, then we are done
                    if (includeNextSegment == false)
                        break;
                }

                currentIndex += segmentLength;
            }
        }

        public ISegment[] Split(long atIndex)
        {
            if (atIndex == 0)
                return new ISegment[] { this };

            List<ISegment> segments = new List<ISegment>();

            long currentIndex = 0;
            foreach (ISegment segment in _segments)
            {
                long segmentLength = segment.GetLength();
                if (segmentLength == 0)
                    continue;

                if (atIndex >= currentIndex && atIndex < currentIndex + segmentLength)
                {
                    long segmentSplitAt = (atIndex - currentIndex);

                    segments.AddRange(segment.Split(segmentSplitAt));
                }
                else
                {
                    segments.Add(segment);
                }

                currentIndex += segmentLength;
            }

            return segments.ToArray();
        }

        public ISegment IncreaseOffset(long byAmount)
        {
            List<ISegment> newSegments = new List<ISegment>();

            long remaining = byAmount;

            long currentIndex = 0;
            for (int i = 0; i < _segments.Count; i++)
            {
                ISegment segment = _segments[i];

                long segmentLength = segment.GetLength();
                if (segmentLength == 0)
                    continue;

                if (remaining < segmentLength)
                {
                    newSegments.Add(segment.IncreaseOffset(remaining));
                    remaining = 0;
                }
                else if (remaining >= segmentLength)
                {
                    // No need to add the segment
                    remaining -= segmentLength;
                }
                else
                {
                    newSegments.Add(segment);
                }

                currentIndex += segmentLength;
            }

            return new CompositeSegment(newSegments.ToArray());
        }

        public ISegment DecreaseLength(long byAmount)
        {
            List<ISegment> newSegments = new List<ISegment>();

            long remaining = byAmount;

            long currentIndex = 0;
            for (int i = _segments.Count - 1; i >= 0; i--)
            {
                ISegment segment = _segments[i];

                long segmentLength = segment.GetLength();
                if (segmentLength == 0)
                    continue;

                if (remaining < segmentLength)
                {
                    newSegments.Insert(0, segment.DecreaseLength(remaining));
                    remaining = 0;
                }
                else if (remaining >= segmentLength)
                {
                    // No need to add the segment
                    remaining -= segmentLength;
                }
                else
                {
                    newSegments.Insert(0, segment);
                }

                currentIndex += segmentLength;
            }

            return new CompositeSegment(newSegments.ToArray());
        }

        #endregion
    }

}
