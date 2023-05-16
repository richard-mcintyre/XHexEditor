using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers.Tests
{
    public class StreamProviderTests
    {
        [Test]
        public async Task ApplyChanges()
        {
            const int length = 100;

            byte[] original = new byte[length];
            TestContext.CurrentContext.Random.NextBytes(original);

            byte[] expected = new byte[length];
            Array.Copy(original, expected, length);

            using (IProvider provider = new StreamProvider(new MemoryStream(original)))
            {
                // Delete, insert and modify bytes
                provider.Modify(50, 0xff);
                expected[50] = 0xff;

                provider.Delete(90);
                Array.Copy(expected, 91, expected, 90, 9);

                provider.InsertByte(10, 0xff);
                Array.Copy(expected, 10, expected, 11, length - 11);
                expected[10] = 0xff;

                await provider.ApplyChangesAsync(default, null);
            }

            Assert.That(original, Is.EqualTo(expected));
        }

        [Test]
        public async Task ApplyChanges_Insert()
        {
            const int length = 100;

            byte[] original = new byte[length];
            TestContext.CurrentContext.Random.NextBytes(original);

            MemoryStream originalStream = new MemoryStream();   // Since we didnt pass a byte array - its expandable
            originalStream.Write(original, 0, original.Length);

            byte[] insertData = new byte[10];
            TestContext.CurrentContext.Random.NextBytes(insertData);

            const int insertAtIndex = 10;

            using (IProvider provider = new StreamProvider(originalStream))
            {
                for(int i= insertAtIndex; i<insertData.Length + insertAtIndex; i++)
                    provider.InsertByte(i, insertData[i- insertAtIndex]);

                await provider.ApplyChangesAsync(default, null);
            }

            byte[] actual = originalStream.ToArray();

            byte[] expected = new byte[length + insertData.Length];
            Array.Copy(original,0, expected, 0, insertAtIndex);
            Array.Copy(insertData, 0, expected, insertAtIndex, insertData.Length);
            Array.Copy(original, insertAtIndex, expected, insertAtIndex + insertData.Length, original.Length - insertAtIndex);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task ApplyChanges_Delete()
        {
            const int length = 100;

            byte[] original = new byte[length];
            TestContext.CurrentContext.Random.NextBytes(original);

            MemoryStream originalStream = new MemoryStream();   // Since we didnt pass a byte array - its expandable
            originalStream.Write(original, 0, original.Length);

            const int deleteAtIndex = 10;
            const int deleteCount = 10;

            using (IProvider provider = new StreamProvider(originalStream))
            {
                for (int i = 0; i < deleteCount; i++)
                    provider.Delete(deleteAtIndex);

                await provider.ApplyChangesAsync(default, null);
            }

            byte[] actual = originalStream.ToArray();

            byte[] expected = new byte[length - deleteCount];
            Array.Copy(original, 0, expected, 0, deleteAtIndex);
            Array.Copy(original, deleteAtIndex + deleteCount, expected, deleteAtIndex, length - (deleteAtIndex + deleteCount));

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public async Task ApplyChanges_Modify()
        {
            const int length = 100;

            byte[] original = new byte[length];
            TestContext.CurrentContext.Random.NextBytes(original);

            byte[] modifyData = new byte[10];
            TestContext.CurrentContext.Random.NextBytes(modifyData);

            const int modifyAtIndex = 10;

            MemoryStream originalStream = new MemoryStream();   // Since we didnt pass a byte array - its expandable
            originalStream.Write(original, 0, original.Length);

            using (IProvider provider = new StreamProvider(originalStream))
            {
                provider.Modify(modifyAtIndex, modifyData, 0, modifyData.Length);
                await provider.ApplyChangesAsync(default, null);
            }

            byte[] actual = originalStream.ToArray();

            byte[] expected = new byte[length];
            Array.Copy(original, expected, length);
            Array.Copy(modifyData, 0, expected, modifyAtIndex, modifyData.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
