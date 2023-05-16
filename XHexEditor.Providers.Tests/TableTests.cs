using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace XHexEditor.Providers.Tests
{
    public class TableTests : TestBase
    {
        [Test]
        public void GetLength()
        {
            const int length = 100;

            Table table = CreateTable(length);

            long actual = table.GetLength();

            Assert.That(actual, Is.EqualTo(length));
        }

        [Test]
        public void CopyTo_All()
        {
            const int length = 100;

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] actual = new byte[length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(initial));
        }

        [Test]
        public void CopyTo_StartIndex([Random(0, 98, 20)] int startIndex)
        {
            const int length = 100;

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] actual = new byte[length - startIndex];
            table.CopyTo(startIndex, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, startIndex, initial.Length - startIndex)));
        }

        [Test]
        public void CopyTo_EndIndex([Random(2, 99, 20)] int endIndex)
        {
            const int length = 100;

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] actual = new byte[length - endIndex];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(new ArraySegment<byte>(initial, 0, initial.Length - endIndex)));
        }

        [Test]
        public void InsertSingleByte([Values(0, 25, 50, 100, 125)] long insertAtIndex)
        {
            const int length = 100;
            const byte insertValue = 0xff;

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] expected;
            if (insertAtIndex <= length)
            {
                expected = new byte[length + 1];
                expected[insertAtIndex] = insertValue;
                Array.Copy(initial, 0, expected, 0, insertAtIndex);
                Array.Copy(initial, insertAtIndex, expected, insertAtIndex + 1, length - insertAtIndex);
            }
            else
            {
                expected = new byte[insertAtIndex + 1];
                expected[insertAtIndex] = insertValue;
                Array.Copy(initial, 0, expected, 0, initial.Length);
            }

            table.Insert(insertAtIndex, insertValue);

            Assert.That(table.GetLength(), Is.EqualTo(expected.Length));

            byte[] actual = new byte[expected.Length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void InsertManyBytes_OneByteAtATime()
        {
            const int length = 4096;
            const int insertByteCount = 400;
            byte insertValue = 0xff;

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] expected = initial;
            for (int i = 0; i < insertByteCount; i++)
            {
                int insertAtIndex = TestContext.CurrentContext.Random.Next(initial.Length - 2);

                table.Insert(insertAtIndex, insertValue);

                expected = new byte[table.GetLength()];
                expected[insertAtIndex] = insertValue;
                Array.Copy(initial, 0, expected, 0, insertAtIndex);
                Array.Copy(initial, insertAtIndex, expected, insertAtIndex + 1, initial.Length - insertAtIndex);

                initial = expected;

                insertValue--;
            }

            Assert.That(table.GetLength(), Is.EqualTo(expected.Length));

            byte[] actual = new byte[expected.Length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void InsertBuffer([Values(0, 25, 50, 75, 100, 125)] int insertAtIndex)
        {
            const int length = 100;
            const int insertByteCount = 25;

            byte[] insertBuffer = new byte[insertByteCount];
            TestContext.CurrentContext.Random.NextBytes(insertBuffer);

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] expected;
            if (insertAtIndex <= length)
            {
                expected = new byte[length + insertByteCount];
                Array.Copy(initial, 0, expected, 0, insertAtIndex);
                Array.Copy(insertBuffer, 0, expected, insertAtIndex, insertByteCount);
                Array.Copy(initial, insertAtIndex, expected, insertAtIndex + insertByteCount, length - insertAtIndex);
            }
            else
            {
                expected = new byte[insertAtIndex + insertByteCount];
                Array.Copy(insertBuffer, 0, expected, insertAtIndex, insertByteCount);
                Array.Copy(initial, 0, expected, 0, initial.Length);
            }

            table.Insert(insertAtIndex, insertBuffer, 0, insertBuffer.Length);

            Assert.That(table.GetLength(), Is.EqualTo(expected.Length));

            byte[] actual = new byte[expected.Length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void DeleteSingleByte([Values(0, 25, 50, 99)] long deleteAtIndex)
        {
            const int length = 100;

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] expected = new byte[length - 1];
            Array.Copy(initial, 0, expected, 0, deleteAtIndex);
            Array.Copy(initial, deleteAtIndex + 1, expected, deleteAtIndex, length - (deleteAtIndex + 1));

            table.Delete(deleteAtIndex);

            Assert.That(table.GetLength(), Is.EqualTo(expected.Length));

            byte[] actual = new byte[expected.Length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test, Sequential]
        public void DeleteRange([Values(0, 35, 45, 55, 90)] long deleteAtIndex)
        {
            const int length = 50;
            const long deleteByteCount = 10;

            byte[] initial1;
            byte[] initial2;
            Table table = new Table(SegmentFactory.Create(
                    CreateStreamSegment(length, out initial1),
                    CreateStreamSegment(length, out initial2)
                ));

            byte[] combinedInitial = initial1.Concat(initial2).ToArray();

            byte[] expected = new byte[(length * 2) - deleteByteCount];
            Array.Copy(combinedInitial, 0, expected, 0, deleteAtIndex);
            Array.Copy(combinedInitial, deleteAtIndex + deleteByteCount, expected, deleteAtIndex, (length * 2) - (deleteAtIndex + deleteByteCount));

            table.Delete(deleteAtIndex, deleteByteCount);

            Assert.That(table.GetLength(), Is.EqualTo(expected.Length));

            byte[] actual = new byte[expected.Length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ModifySingleByte([Values(0, 25, 50, 99)] long modifyAtIndex)
        {
            const int length = 100;
            const byte updateToValue = 0xff;

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] expected = new byte[length];
            Array.Copy(initial, expected, length);
            expected[modifyAtIndex] = updateToValue;

            table.Modify(modifyAtIndex, updateToValue);

            Assert.That(table.GetLength(), Is.EqualTo(expected.Length));

            byte[] actual = new byte[expected.Length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void ModifyBuffer([Values(0, 25, 50, 75)] long modifyAtIndex)
        {
            const int length = 100;
            const int modifyByteCount = 25;

            byte[] modifyToBuffer = new byte[modifyByteCount];
            TestContext.CurrentContext.Random.NextBytes(modifyToBuffer);

            byte[] initial;
            Table table = CreateTable(length, out initial);

            byte[] expected = new byte[length];
            Array.Copy(initial, expected, length);
            Array.Copy(modifyToBuffer, 0, expected, modifyAtIndex, modifyByteCount);

            table.Modify(modifyAtIndex, modifyToBuffer, 0, modifyToBuffer.Length);

            Assert.That(table.GetLength(), Is.EqualTo(expected.Length));

            byte[] actual = new byte[expected.Length];
            table.CopyTo(0, actual, 0, actual.Length);

            Assert.That(actual, Is.EqualTo(expected));
        }


    }

}
