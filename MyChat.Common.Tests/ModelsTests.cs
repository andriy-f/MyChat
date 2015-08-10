using MyChat.Common.Models.Messages;
using NUnit.Framework;

namespace MyChat.Common.Tests
{
    using System;

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

        [Test]
        public void ResponseMessageConsistencyTest1()
        {
            var original = new Response()
            {
                Id = Guid.NewGuid(),
                IsSuccess = false,
                Data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }
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
            var reconstructedMessage = new Response();
            reconstructedMessage.Accept(deserializer);

            Assert.AreEqual(original.Id, reconstructedMessage.Id);
            Assert.AreEqual(original.IsSuccess, reconstructedMessage.IsSuccess);
            Assert.AreEqual(original.Data, reconstructedMessage.Data);
        }

        [Test]
        public void ResponseMessageConsistencyTest2()
        {
            var original = new Response()
            {
                Id = Guid.NewGuid(),
                IsSuccess = true,
                Data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }
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
            var reconstructedMessage = new Response();
            reconstructedMessage.Accept(deserializer);

            Assert.AreEqual(original.Id, reconstructedMessage.Id);
            Assert.AreEqual(original.IsSuccess, reconstructedMessage.IsSuccess);
            Assert.AreEqual(original.Data, reconstructedMessage.Data);
        }

        [Test]
        public void RequestMessageConsistencyTest()
        {
            var original = new Request()
            {
                Id = Guid.NewGuid(),
                Data = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }
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
            var reconstructedMessage = new Request();
            reconstructedMessage.Accept(deserializer);

            Assert.AreEqual(original.Id, reconstructedMessage.Id);
            Assert.AreEqual(original.Data, reconstructedMessage.Data);
        }
    }
}
