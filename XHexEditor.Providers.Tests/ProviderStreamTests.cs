using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers.Tests
{
    public class ProviderStreamTests : TestBase
    {
        [Test]
        public void StreamAll()
        {
            const int length = 100;

            byte[] data = new byte[length];
            TestContext.CurrentContext.Random.NextBytes(data);

            byte[] actual;
            using (IProvider provider = new StreamProvider(new MemoryStream(data)))
            {
                using (MemoryStream destStream = new MemoryStream())
                {
                    provider.AsStream().CopyTo(destStream);

                    actual = destStream.ToArray();
                }
            }

            Assert.That(actual, Is.EqualTo(data));
        }

        [Test]
        public void StreamWithChanges()
        {
            const int length = 100;

            byte[] data = new byte[length];
            TestContext.CurrentContext.Random.NextBytes(data);

            byte[] actual;
            using (IProvider provider = new StreamProvider(new MemoryStream(data)))
            {
                provider.Modify(50, 0xff);
                data[50] = 0xff;                

                using (MemoryStream destStream = new MemoryStream())
                {
                    provider.AsStream().CopyTo(destStream);

                    actual = destStream.ToArray();
                }
            }

            Assert.That(actual, Is.EqualTo(data));
        }
    }
}
