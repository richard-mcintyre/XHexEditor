using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    internal static class SegmentExtensions
    {
        public static int IndexOfSegmentStartingAtIndex(this IEnumerable<ISegment> segments, long atIndex)
        {
            long currentIndex = 0;

            for (int i = 0; i < segments.Count(); i++)
            {
                ISegment segment = segments.ElementAt(i);

                long segmentLength = segment.GetLength();
                if (segmentLength == 0)
                    continue;

                if (atIndex == currentIndex)
                    return i;

                currentIndex += segmentLength;
            }

            return -1;
        }

        public static IEnumerable<ISegment> Combine(this IEnumerable<ISegment> segments)
        {
            List<ISegment> result = new List<ISegment>();

            List<ArraySegment> arraySegmentsToCombine = new List<ArraySegment>();

            foreach (ISegment currentSegment in segments)
            {
                if (currentSegment.GetLength() == 0)
                    continue;

                if (currentSegment is ArraySegment arraySegment)
                {
                    arraySegmentsToCombine.Add(arraySegment);
                    continue;
                }

                if (arraySegmentsToCombine.Any())
                {
                    result.Add(ArraySegment.Combine(arraySegmentsToCombine));
                    arraySegmentsToCombine.Clear();
                }

                result.Add(currentSegment);
            }

            if (arraySegmentsToCombine.Any())
                result.Add(ArraySegment.Combine(arraySegmentsToCombine));

            return result;
        }
    }
}
