using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace XHexEditor.Providers.Tests
{
    public class TestBase
    {
        private const int _paddingByteCount = 12;    // Number of bytes that will be inserted before and after each segments data

        internal static ISegment CreateArraySegment(int length)
        {
            byte[] data = new byte[length + (_paddingByteCount * 2)];
            TestContext.CurrentContext.Random.NextBytes(data);

            return SegmentFactory.Create(data, _paddingByteCount, length);
        }

        internal static ISegment CreateArraySegment(int length, out byte[] array)
        {
            byte[] data = new byte[length + (_paddingByteCount * 2)];
            TestContext.CurrentContext.Random.NextBytes(data);

            array = new byte[length];
            Array.Copy(data, _paddingByteCount, array, 0, length);

            return SegmentFactory.Create(data, _paddingByteCount, length);
        }

        internal static ISegment CreateStreamSegment(int length)
        {
            byte[] data = new byte[length + (_paddingByteCount * 2)];
            TestContext.CurrentContext.Random.NextBytes(data);
            MemoryStream stream = new MemoryStream(data);

            return SegmentFactory.Create(stream, _paddingByteCount, length);
        }

        internal static ISegment CreateStreamSegment(int length, out byte[] array)
        {
            byte[] data = new byte[length + (_paddingByteCount * 2)];
            TestContext.CurrentContext.Random.NextBytes(data);

            array = new byte[length];
            Array.Copy(data, _paddingByteCount, array, 0, length);

            MemoryStream stream = new MemoryStream(data);

            return SegmentFactory.Create(stream, _paddingByteCount, length);
        }

        internal static Table CreateTable(int length) =>
            new Table(CreateStreamSegment(length));

        internal static Table CreateTable(int length, out byte[] array) =>
            new Table(CreateStreamSegment(length, out array));
    }

}
