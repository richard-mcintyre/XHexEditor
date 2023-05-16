using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    static class SegmentFactory
    {
        public static ISegment Create(byte[] buffer, int offset, int length) =>
            new Providers.ArraySegment(buffer, offset, length);

        public static ISegment Create(Stream stream, long offset, long length) =>
            new Providers.StreamSegment(stream, offset, length);

        public static ISegment Create(params ISegment[] segments) =>
            new Providers.CompositeSegment(segments);
    }
}
