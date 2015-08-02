namespace MyChat.Common.Tests
{
    using NUnit.Framework;

    [TestFixture]
    class UtilsTests
    {
        [Test]
        public void SerializaDeserializeUtf8StrNormalTest()
        {
            const string someStr = "Green red pepper";
            var serialized = Utils.SerializeUTF8String(someStr);
            var back = Utils.DeSerializeUTF8String(serialized);

            Assert.AreEqual(someStr, back);
        }

        [Test]
        public void SerializaDeserializeUtf8StrNullTest()
        {
            var serialized = Utils.SerializeUTF8String(null);
            var back = Utils.DeSerializeUTF8String(serialized);

            Assert.AreEqual(string.Empty, back);
        }

        [Test]
        public void SerializaDeserializeUtf8StrEmptyTest()
        {
            var serialized = Utils.SerializeUTF8String(string.Empty);
            var back = Utils.DeSerializeUTF8String(serialized);

            Assert.AreEqual(string.Empty, back);
        }
    }
}
