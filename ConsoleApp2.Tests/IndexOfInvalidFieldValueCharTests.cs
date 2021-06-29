using NUnit.Framework;

namespace ConsoleApp2.Tests
{
    [TestFixture]
    public class IndexOfInvalidFieldValueCharTests
    {
        [Test]
        public void Scalar()
        {
            for (int i = char.MinValue; i <= char.MaxValue; ++i)
            {
                string s = "1" + (char)i;

                Assume.That(s.Length == 2);

                int idx0 = HttpCharacters.IndexOfInvalidFieldValueChar(s);
                int idx1 = HttpCharacters_Vectorized.IndexOfInvalidFieldValueChar(s);

                Assert.AreEqual(idx0, idx1, "Failure by char {0} | 0x{1:X2}", (char)i, i);
            }
        }
        //---------------------------------------------------------------------
        [Test]
        public void Length_8()
        {
            for (int i = char.MinValue; i <= char.MaxValue; ++i)
            {
                string s = "1234567" + (char)i;

                Assume.That(s.Length == 8);

                int idx0 = HttpCharacters.IndexOfInvalidFieldValueChar(s);
                int idx1 = HttpCharacters_Vectorized.IndexOfInvalidFieldValueChar(s);

                Assert.AreEqual(idx0, idx1, "Failure by char {0} | 0x{1:X2}", (char)i, i);
            }
        }
        //---------------------------------------------------------------------
        [Test]
        public void Length_9()
        {
            for (int i = char.MinValue; i <= char.MaxValue; ++i)
            {
                string s = "12345678" + (char)i;

                Assume.That(s.Length == 9);

                int idx0 = HttpCharacters.IndexOfInvalidFieldValueChar(s);
                int idx1 = HttpCharacters_Vectorized.IndexOfInvalidFieldValueChar(s);

                Assert.AreEqual(idx0, idx1, "Failure by char {0} | 0x{1:X2}", (char)i, i);
            }
        }
        //---------------------------------------------------------------------
        [Test]
        public void Length_15()
        {
            for (int i = char.MinValue; i <= char.MaxValue; ++i)
            {
                string s = "12345678901234" + (char)i;

                Assume.That(s.Length == 15);

                int idx0 = HttpCharacters.IndexOfInvalidFieldValueChar(s);
                int idx1 = HttpCharacters_Vectorized.IndexOfInvalidFieldValueChar(s);

                Assert.AreEqual(idx0, idx1, "Failure by char {0} | 0x{1:X2}", (char)i, i);
            }
        }
        //---------------------------------------------------------------------
        [Test]
        public void Length_16()
        {
            for (int i = char.MinValue; i <= char.MaxValue; ++i)
            {
                string s = "123456789012345" + (char)i;

                Assume.That(s.Length == 16);

                int idx0 = HttpCharacters.IndexOfInvalidFieldValueChar(s);
                int idx1 = HttpCharacters_Vectorized.IndexOfInvalidFieldValueChar(s);

                Assert.AreEqual(idx0, idx1, "Failure by char {0} | 0x{1:X2}", (char)i, i);
            }
        }
        //---------------------------------------------------------------------
        [Test]
        public void Length_113()
        {
            for (int i = char.MinValue; i <= char.MaxValue; ++i)
            {
                string s = new string('A', 112) + (char)i;

                Assume.That(s.Length == 113);

                int idx0 = HttpCharacters.IndexOfInvalidFieldValueChar(s);
                int idx1 = HttpCharacters_Vectorized.IndexOfInvalidFieldValueChar(s);

                Assert.AreEqual(idx0, idx1, "Failure by char {0} | 0x{1:X2}", (char)i, i);
            }
        }
    }
}
