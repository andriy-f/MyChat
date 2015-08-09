using MyChat.Common.Models.Messages;
using NUnit.Framework;

namespace MyChat.Common.Tests
{
    [TestFixture]
    public class ModelsTests
    {
        [Test]
        public void LogonCredentialsConsistencyTest()
        {
            var creds = new LogonCredentials { Login = "peter", Password = "secret" };
            var serialized = creds.ToBytes();
            var deserialized = LogonCredentials.FromBytes(serialized);

            Assert.AreEqual("peter", deserialized.Login);
            Assert.AreEqual("secret", deserialized.Password);
        }

        [Test]
        public void LogonCredentialsConsistencyTest2()
        {
            var creds = new LogonCredentials { Login = "peter", Password = "secret" };
            var serializedCreds = creds.ToBytes();
            var serviceMessage = new ServiceMessage {MessageType = MessageType.Logon, Data = serializedCreds};
            var serializedServiceMessage = serviceMessage.ToBytes();

            var deserializedServiceMessage = ServiceMessage.FromBytes(serializedServiceMessage);
            Assert.AreEqual(MessageType.Logon, deserializedServiceMessage.MessageType);
            var deserializedCreds = LogonCredentials.FromBytes(deserializedServiceMessage.Data);

            Assert.AreEqual("peter", deserializedCreds.Login);
            Assert.AreEqual("secret", deserializedCreds.Password);
        }

        [Test]
        public void SuperMessageConsistencyTest()
        {
            var original = new SuperServiceMessage()
                               {
                                   SuperMessageType =
                                       SuperServiceMessage.SuperServiceMessageType.Response,
                                   DataBuffer = new byte[] { 0xAA, 0xBB, 0xCC }
                               };

            // Serializing
            var serializer = new MessageSerializingVisitor();
            original.Accept(serializer);
            var serializedData = serializer.Result;
            Assert.NotNull(serializedData);
            Assert.True(serializedData.Length > 0);

            // Deserializing
            var deserializer = new MessageDeserializingVisitor()
                                   {
                                       DataBuffer = serializedData,
                                       DataLength = serializedData.Length,
                                       BufferOffset = 0
                                   };
            var reconstructedMessage = new SuperServiceMessage();
            reconstructedMessage.Accept(deserializer);

            Assert.AreEqual(original.SuperMessageType, reconstructedMessage.SuperMessageType);
            Assert.AreEqual(original.DataBuffer, reconstructedMessage.DataBuffer);
        }
    }
}
