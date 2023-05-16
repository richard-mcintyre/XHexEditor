using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers.Tests
{
    public class ArraySegmentTests : TestBase
    {
        [Test]
        public void GetLength([Values(100)] int length)
        {
            ISegment segment = CreateArraySegment(length);

            Assert.That(segment.GetLength(), Is.EqualTo(length));
        }

        [Test]
        public void CopyTo_All()
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateArraySegment(length, out initial);

            byte[] actual = new byte[length];
            segment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(initial));
        }

        [Test]
        public void CopyTo_StartIndex([Random(0, 98, 20)] int startIndex)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateArraySegment(length, out initial);

            byte[] actual = new byte[length - startIndex];
            segment.CopyTo(startIndex, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, startIndex, initial.Length - startIndex)));
        }

        [Test]
        public void CopyTo_EndIndex([Random(2, 99, 20)] int endIndex)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateArraySegment(length, out initial);

            byte[] actual = new byte[length - endIndex];
            segment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, 0, initial.Length - endIndex)));
        }

        [Test]
        public void Split([Random(1, 99, 20)] int splitAtIndex)
        {
            const int length = 100;

            byte[] initial;
            ISegment segment = CreateArraySegment(length, out initial);

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
            ISegment segment = CreateArraySegment(length, out initial);

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
            ISegment segment = CreateArraySegment(length, out initial);

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
            ISegment segment = CreateArraySegment(length, out initial);

            ISegment newSegment = segment.DecreaseLength(decreaseBy);

            byte[] actual = new byte[length - decreaseBy];
            newSegment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(newSegment.GetLength(), Is.EqualTo(length - decreaseBy));
            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, 0, length - decreaseBy)));
        }

        [Test]
        public void Combine()
        {
            const int length = 10;

            byte[] initial1;
            byte[] initial2;
            byte[] initial3;
            ArraySegment[] source = new ArraySegment[]
            {
                (ArraySegment)CreateArraySegment(length, out initial1),
                (ArraySegment)CreateArraySegment(length, out initial2),
                (ArraySegment)CreateArraySegment(length, out initial3),
            };

            byte[] initialCombined = initial1.Concat(initial2.Concat(initial3)).ToArray();

            ArraySegment combinedSegment = ArraySegment.Combine(source);

            byte[] actual = new byte[combinedSegment.GetLength()];
            combinedSegment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(initialCombined));
        }

        [Test]
        public void Combine_MixtureOfSegments()
        {
            const int length = 10;

            byte[] initial1, initial2, initial3, initial4, initial5, initial6, initial7, initial8;
            ISegment[] source = new ISegment[]
            {
                CreateArraySegment(length, out initial1),
                CreateStreamSegment(length, out initial2),
                CreateArraySegment(length, out initial3),
                CreateArraySegment(length, out initial4),
                CreateArraySegment(length, out initial5),
                CreateStreamSegment(length, out initial6),
                CreateArraySegment(length, out initial7),
                CreateArraySegment(length, out initial8),
            };

            List<byte> initialCombined = new List<byte>();
            initialCombined.AddRange(initial1);
            initialCombined.AddRange(initial2);
            initialCombined.AddRange(initial3);
            initialCombined.AddRange(initial4);
            initialCombined.AddRange(initial5);
            initialCombined.AddRange(initial6);
            initialCombined.AddRange(initial7);
            initialCombined.AddRange(initial8);

            ISegment combinedSegment = SegmentFactory.Create(source.Combine().ToArray());

            byte[] actual = new byte[combinedSegment.GetLength()];
            combinedSegment.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(initialCombined));
        }
    }

}
