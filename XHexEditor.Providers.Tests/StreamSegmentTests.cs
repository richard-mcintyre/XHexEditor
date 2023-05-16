using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers.Tests
{
    public class StreamSegmentTests : TestBase
    {
        [Test]
        public void GetLength([Values(100)] int length)
        {
            ISegment segment = CreateStreamSegment(length);

            Assert.That(segment.GetLength(), Is.EqualTo(length));
        }

        [Test]
        public void CopyTo_All()
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateStreamSegment(length, out initial);

            byte[] actual = new byte[length];
            segment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(initial));
        }

        [Test]
        public void CopyTo_StartIndex([Random(0, 98, 20)] int startIndex)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateStreamSegment(length, out initial);

            byte[] actual = new byte[length - startIndex];
            segment.CopyTo(startIndex, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, startIndex, initial.Length - startIndex)));
        }

        [Test]
        public void CopyTo_EndIndex([Random(2, 99, 20)] int endIndex)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateStreamSegment(length, out initial);

            byte[] actual = new byte[length - endIndex];
            segment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, 0, initial.Length - endIndex)));
        }

        [Test]
        public void Split([Random(1, 99, 20)] int splitAtIndex)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateStreamSegment(length, out initial);

            ISegment[] splitSegments = segment.Split(splitAtIndex);
            Assert.That(splitSegments.Length, Is.EqualTo(2));
            Assert.That(splitSegments[0].GetLength(), Is.EqualTo(splitAtIndex));
            Assert.That(splitSegments[1].GetLength(), Is.EqualTo(length - splitAtIndex));

            ISegment combinedSegments = SegmentFactory.Create(splitSegments);
            Assert.That(combinedSegments.GetLength(), Is.EqualTo(length));

            byte[] actual = new byte[combinedSegments.GetLength()];
            combinedSegments.CopyTo(0, actual, 0, actual.Length);
            Assert.That(actual, Is.EqualTo(initial));
        }

        [Test]
        public void Split_AtIndex0()
        {
            const int length = 100;
            const int splitAtIndex = 0;

            byte[] initial;
            ISegment segment = CreateStreamSegment(length, out initial);

            ISegment[] splitSegments = segment.Split(splitAtIndex);
            Assert.That(splitSegments.Length, Is.EqualTo(1));
            Assert.That(splitSegments[0].GetLength(), Is.EqualTo(length));

            ISegment combinedSegments = SegmentFactory.Create(splitSegments);
            Assert.That(combinedSegments.GetLength(), Is.EqualTo(length));

            byte[] actual = new byte[combinedSegments.GetLength()];
            combinedSegments.CopyTo(0, actual, 0, actual.Length);
            Assert.That(actual, Is.EqualTo(initial));
        }

        [Test]
        public void IncreaseOffset([Values(0, 25, 50, 75, 100)] int adjustBy)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateStreamSegment(length, out initial);

            ISegment newSegment = segment.IncreaseOffset(adjustBy);

            byte[] actual = new byte[length - adjustBy];
            newSegment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(newSegment.GetLength(), Is.EqualTo(length - adjustBy));
            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, adjustBy, length - adjustBy)));
        }

        [Test]
        public void DecreaseLength([Values(0, 25, 50, 75, 100)] int decreaseBy)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateStreamSegment(length, out initial);

            ISegment newSegment = segment.DecreaseLength(decreaseBy);

            byte[] actual = new byte[length - decreaseBy];
            newSegment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(newSegment.GetLength(), Is.EqualTo(length - decreaseBy));
            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, 0, length - decreaseBy)));
        }
    }

}
