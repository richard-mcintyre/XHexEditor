using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers.Tests
{
    public class CompositeSegmentTests : TestBase
    {
        [Test]
        public void GetLength([Values(100)] int length)
        {
            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length),
                CreateStreamSegment(length));

            Assert.That(segment.GetLength(), Is.EqualTo(length * 2));
        }

        [Test]
        public void CopyTo_All()
        {
            const int length = 100;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            byte[] actual = new byte[length * 2];
            segment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(initial1.Concat(initial2)));
        }

        [Test]
        public void CopyTo_StartIndex([Random(0, 198, 40)] int startIndex)
        {
            const int length = 100;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            byte[] initialCombined = initial1.Concat(initial2).ToArray();

            byte[] actual = new byte[(length * 2) - startIndex];
            segment.CopyTo(startIndex, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initialCombined, startIndex, initialCombined.Length - startIndex)));
        }

        [Test]
        public void CopyTo_EndIndex([Random(2, 199, 20)] int endIndex)
        {
            const int length = 100;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            byte[] initialCombined = initial1.Concat(initial2).ToArray();

            byte[] actual = new byte[(length * 2) - endIndex];
            segment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initialCombined, 0, initialCombined.Length - endIndex)));
        }

        [Test]
        public void Split([Random(1, 199, 20)] int splitAtIndex)
        {
            const int length = 100;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            ISegment[] splitSegments = segment.Split(splitAtIndex);

            ISegment combinedSegments = SegmentFactory.Create(splitSegments);
            Assert.That(combinedSegments.GetLength(), Is.EqualTo(length * 2));

            byte[] actual = new byte[combinedSegments.GetLength()];
            combinedSegments.CopyTo(0, actual, 0, actual.Length);
            Assert.That(actual, Is.EqualTo(initial1.Concat(initial2)));
        }

        [Test]
        public void Split_AtIndex0()
        {
            const int length = 100;
            const int splitAtIndex = 0;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            ISegment[] splitSegments = segment.Split(splitAtIndex);
            Assert.That(splitSegments.Length, Is.EqualTo(1));
            Assert.That(splitSegments[0].GetLength(), Is.EqualTo(length * 2));

            ISegment combinedSegments = SegmentFactory.Create(splitSegments);
            Assert.That(combinedSegments.GetLength(), Is.EqualTo(length * 2));

            byte[] actual = new byte[combinedSegments.GetLength()];
            combinedSegments.CopyTo(0, actual, 0, actual.Length);
            Assert.That(actual, Is.EqualTo(initial1.Concat(initial2)));
        }

        [Test]
        public void Split_AtSegmentBoundary()
        {
            const int length = 100;
            const int splitAtIndex = 100;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            ISegment[] splitSegments = segment.Split(splitAtIndex);
            Assert.That(splitSegments.Length, Is.EqualTo(2));
            Assert.That(splitSegments[0].GetLength(), Is.EqualTo(length));
            Assert.That(splitSegments[1].GetLength(), Is.EqualTo(length));

            ISegment combinedSegments = SegmentFactory.Create(splitSegments);
            Assert.That(combinedSegments.GetLength(), Is.EqualTo(length * 2));

            byte[] actual = new byte[combinedSegments.GetLength()];
            combinedSegments.CopyTo(0, actual, 0, actual.Length);
            Assert.That(actual, Is.EqualTo(initial1.Concat(initial2)));
        }

        [Test]
        public void IncreaseOffset([Values(0, 25, 50, 75, 100, 125, 150, 175, 200)] int adjustBy)
        {
            const int length = 100;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            byte[] initialCombined = initial1.Concat(initial2).ToArray();

            ISegment newSegment = segment.IncreaseOffset(adjustBy);

            byte[] actual = new byte[(length * 2) - adjustBy];
            newSegment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(newSegment.GetLength(), Is.EqualTo((length * 2) - adjustBy));
            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initialCombined, adjustBy, (length * 2) - adjustBy)));
        }

        [Test]
        public void DecreaseLength([Values(0, 25, 50, 75, 100, 125, 150, 175, 200)] int decreaseBy)
        {
            const int length = 100;

            byte[] initial1;
            byte[] initial2;

            ISegment segment = SegmentFactory.Create(
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2));

            byte[] initialCombined = initial1.Concat(initial2).ToArray();

            ISegment newSegment = segment.DecreaseLength(decreaseBy);

            byte[] actual = new byte[(length * 2) - decreaseBy];
            newSegment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(newSegment.GetLength(), Is.EqualTo((length * 2) - decreaseBy));
            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initialCombined, 0, (length * 2) - decreaseBy)));
        }
    }

}
