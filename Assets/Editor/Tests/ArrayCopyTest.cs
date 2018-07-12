using Packer;
using NUnit.Framework;

namespace PackerTests
{
    [TestFixture]
    public class ArrayCopyTest
    {
        [Test]
        public void TestOriginalDoesNotInfluenceCopy()
        {
            var original = new bool[5, 5];

            var copy = Helper.Copy(original);

            original[4, 4] = true;

            Assert.IsFalse(copy[4, 4]);
        }

        [Test]
        public void TestCopyDoesNotInfluenceOriginal()
        {
            var original = new bool[5, 5];

            var copy = Helper.Copy(original);

            copy[4, 4] = true;

            Assert.IsFalse(original[4, 4]);
        }
    }
}
