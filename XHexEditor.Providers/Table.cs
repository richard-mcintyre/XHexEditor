using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    internal class Table
    {
        #region Construction

        public Table(ISegment rootSegment)
        {
            _rootSegment = rootSegment;
        }

        #endregion

        #region Fields

        private static readonly byte[] _singleByteValues = new byte[]
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
            20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39,
            40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
            60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
            80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99,

            100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113, 114, 115, 116, 117, 118, 119,
            120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139,
            140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159,
            160, 161, 162, 163, 164, 165, 166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179,
            180, 181, 182, 183, 184, 185, 186, 187, 188, 189, 190, 191, 192, 193, 194, 195, 196, 197, 198, 199,

            200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216, 217, 218, 219,
            220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239,
            240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255
        };

        private ISegment _rootSegment;

        #endregion

        #region Methods

        public long GetLength() =>
            _rootSegment.GetLength();

        /// <summary>
        /// Copies the bytes in this table to the specified buffer
        /// </summary>
        /// <param name="sourceIndex">Starting byte index to copy in this table</param>
        /// <param name="destBuffer">Buffer to copy bytes into</param>
        /// <param name="destIndex">Index to copy bytes to in the destination buffer</param>
        /// <param name="count">Number of bytes to copy</param>
        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count) =>
            CopyTo(sourceIndex, destBuffer, destIndex, count, null);

        /// <summary>
        /// Copies the bytes in this table to the specified buffer
        /// </summary>
        /// <param name="sourceIndex">Starting byte index to copy in this table</param>
        /// <param name="destBuffer">Buffer to copy bytes into</param>
        /// <param name="destIndex">Index to copy bytes to in the destination buffer</param>
        /// <param name="count">Number of bytes to copy</param>
        public void CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count, ProviderModifications? modifications)
        {
            modifications?.SetCurrentProviderOffset(0);
            _rootSegment.CopyTo(sourceIndex, destBuffer, destIndex, count, modifications);
        }

        /// <summary>
        /// Inserts a byte at the specified index
        /// </summary>
        /// <param name="atIndex">Index at which the specified value will be inserted</param>
        /// <param name="value">Value to insert</param>
        public void Insert(long atIndex, byte value) =>
            Insert(atIndex, _singleByteValues, value, 1);

        /// <summary>
        /// Inserts bytes with the specified bytes at the specified index
        /// </summary>
        /// <param name="atIndex">Index at which the bytes will be inserted</param>
        /// <param name="value">Value to insert</param>
        public void Insert(long atIndex, byte[] buffer, int offset, int count)
        {
            // If we are to insert a byte at the end or past the end, then grow the table to make 'atIndex' valid
            long tableLength = GetLength();
            if (tableLength < atIndex)
                Grow((int)(atIndex - tableLength));

            List<ISegment> segments = new List<ISegment>();
            segments.AddRange(_rootSegment.Split(atIndex));

            // Once of the segments must at 'atIndex'
            int segmentIndex = segments.IndexOfSegmentStartingAtIndex(atIndex);
            if (segmentIndex == -1)
            {
                segments.Add(new ArraySegment(buffer, offset, count));
            }
            else
            {
                segments.Insert(segmentIndex, new ArraySegment(buffer, offset, count));
            }

            _rootSegment = Compact(segments);
        }

        /// <summary>
        /// Deletes a byte at the specified index
        /// </summary>
        public void Delete(long atIndex) =>
            Delete(atIndex, 1);

        /// <summary>
        /// Deletes a range of bytes at the specified index
        /// </summary>
        public void Delete(long atIndex, long count)
        {
            long remainingCount;

            do
            {
                List<ISegment> segments = new List<ISegment>();
                segments.AddRange(_rootSegment.Split(atIndex));

                // Once of the segments must at 'atIndex'
                int segmentIndex = segments.IndexOfSegmentStartingAtIndex(atIndex);
                if (segmentIndex == -1)
                    throw new InvalidOperationException();  // Should never happen as we just split it

                ISegment segment = segments[segmentIndex];
                segments.RemoveAt(segmentIndex);

                remainingCount = count - segment.GetLength();
                if (remainingCount <= 0)
                {
                    // We only need to update this segment
                    segment = segment.IncreaseOffset(count);
                }
                else
                {
                    // Only some of the range in the segment
                    segment = segment.IncreaseOffset(remainingCount);
                }

                if (segment.GetLength() > 0)
                    segments.Insert(segmentIndex, segment);

                if (segments.Count == 0)
                {
                    _rootSegment = EmptySegment.Empty;
                }
                else if (segments.Count == 1)
                {
                    _rootSegment = segments[0];
                }
                else
                {
                    _rootSegment = Compact(segments);
                }

                count = remainingCount;

            } while (count > 0);
        }

        /// <summary>
        /// Modfies the value of a byte at the specified index
        /// </summary>
        public void Modify(long atIndex, byte value)
        {
            Delete(atIndex, 1);
            Insert(atIndex, value);
        }

        /// <summary>
        /// Changes the bytes to the specified bytes at the specified index
        /// </summary>
        /// <param name="atIndex">Index at which the bytes will be modified</param>
        /// <param name="value">Value to update</param>
        public void Modify(long atIndex, byte[] buffer, int offset, int count)
        {
            Delete(atIndex, count);
            Insert(atIndex, buffer, offset, count);
        }

        private void Grow(int byAmount)
        {
            _rootSegment = new CompositeSegment(
                _rootSegment,
                new ArraySegment(new byte[byAmount], 0, byAmount));
        }

        private static ISegment Compact(IReadOnlyList<ISegment> segments)
        {
            // Only compact if there are more than IdealFillCount segments
            if (segments.Count <= CompositeSegment.IdealFillCount)
            {
                if (segments.Count == 1)
                    return segments[0];

                return new CompositeSegment(segments);
            }

            // Try combining contiguous array segments and see if that reduces it to below IdealFillCount
            ISegment[] combinedSegments = segments.Combine().ToArray();
            if (combinedSegments.Length <= CompositeSegment.IdealFillCount)
            {
                if (combinedSegments.Length == 1)
                    return combinedSegments[0];

                return new CompositeSegment(combinedSegments);
            }

            // Otherwise reduce the segments by chunking them into composite segments
            List<ISegment> subsets = new List<ISegment>();

            for (int i = 0; i < combinedSegments.Length; i++)
            {
                if (combinedSegments[i] is not CompositeSegment)
                    break;

                subsets.Add(combinedSegments[i]);
            }

            int skipCount = subsets.Count;

            const int chunkBy = CompositeSegment.IdealFillCount;
            foreach (ISegment[] subset in combinedSegments.Skip(skipCount).Chunk(chunkBy))
            {
                if (subset.Length < chunkBy)
                {
                    subsets.AddRange(subset);
                }
                else
                {
                    subsets.Add(new CompositeSegment(subset));
                }
            }

            // Combine X number of CompositeSegments that are at the stat of the list
            int combineCount = 0;
            for (int i = 0; i < combinedSegments.Length; i++)
            {
                if (combinedSegments[i] is not CompositeSegment)
                    break;

                combineCount++;
            }

            if (combineCount >= CompositeSegment.IdealFillCount)
            {
                List<ISegment> list = new List<ISegment>();
                list.Add(new CompositeSegment(subsets.Take(combineCount)));
                list.AddRange(subsets.Skip(combineCount));

                subsets = list;
            }

            return new CompositeSegment(subsets);
        }

        #endregion
    }

}
