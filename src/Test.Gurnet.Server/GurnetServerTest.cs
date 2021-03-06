﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Gurnet.Core.Log;
using Gurnet.Server;
using Gurnet.Server.Enums;
using System.Threading;
using Lidgren.Network;
using System.Reflection;
using Gurnet.Core.Networking;
using System.Text;

namespace Test.Gurnet.Server
{
    [TestClass]
    public class GurnetServerTest
    {
        sealed class MockMessageTranslator : IMessageTranslator
        {
            public ActionType ActionType { get; private set; }
            public object Data { get; private set; }

            public void TranslateMessage(string stringMessage)
            {
                if (!string.IsNullOrEmpty(stringMessage))
                {
                    var parts = stringMessage.Split('|');
                    
                    byte packetTypeByte;
                    byte.TryParse(parts[0], out packetTypeByte);

                    ActionType = (ActionType)packetTypeByte;
                    Data = (object)parts[1];
                }
                
            }
        }

        sealed class MockMessageProcessor: IMessageProcessor
        {
            public string Message { get; set; }
            public int MessageBits { get; set; }

            public void ProcessIncomingMessage(NetIncomingMessage incMsg, IMessageTranslator translator)
            {
                if (incMsg == null)
                    throw new ArgumentNullException("incMsg cannot be null");

                MessageBits = incMsg.LengthBits;
                Message = Encoding.UTF8.GetString(incMsg.Data, 0, incMsg.Data.Length);

                switch (incMsg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        {
                            translator.TranslateMessage(Message);
                            break;
                        }
                }
            }
        }

        private GurnetServer GetsNewGurnetServer(ILogger logger, IMessageProcessor processor, IMessageTranslator translator, string name = "gurnet", int port = 14242)
        {
            if (logger == null)
            {
                logger = new ConsoleLogger();
                logger.SetContext("Server");
            } 
            
            GurnetServer server = new GurnetServer(name, port, logger, processor, translator);
            
            return server;
        }

        [TestMethod]
        public void TestProcessIncomingMessage()
        {
            GurnetServer server = GetsNewGurnetServer(null, new MockMessageProcessor(), new MockMessageTranslator());

            string expectedMessage = "this is a message";

            byte[] messageByte = Encoding.UTF8.GetBytes(expectedMessage);

            server.ProcessIncomingMessage(CreateIncomingMessage(messageByte, messageByte.Length));

            Assert.AreEqual(expectedMessage, (server.MessageProcessor as MockMessageProcessor).Message);
            Assert.AreEqual(messageByte.Length, (server.MessageProcessor as MockMessageProcessor).MessageBits);
        }

        [TestMethod]
        public void TestProcessMessageAddPlayer()
        {
            var server = GetsNewGurnetServer(null, new MockMessageProcessor(), new MockMessageTranslator());
            var message = "1|john";

            var messageBytes = Encoding.UTF8.GetBytes(message);
            var incMsg = CreateIncomingMessage(messageBytes, messageBytes.Length, NetIncomingMessageType.Data);

            server.ProcessIncomingMessage(incMsg);

            var translator = server.MessageTranslator as MockMessageTranslator;
            Assert.AreEqual(ActionType.AddPlayer, translator.ActionType);
            Assert.AreEqual("john", (string)translator.Data);
        }

        /// <summary>
        /// Helper method
        /// </summary>
        private NetIncomingMessage CreateIncomingMessage(byte[] fromData, int bitLength, NetIncomingMessageType messageType = NetIncomingMessageType.StatusChanged)
        {
            NetIncomingMessage inc = (NetIncomingMessage)Activator.CreateInstance(typeof(NetIncomingMessage), true);
            typeof(NetIncomingMessage).GetField("m_incomingMessageType", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, messageType);
            typeof(NetIncomingMessage).GetField("m_data", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, fromData);
            typeof(NetIncomingMessage).GetField("m_bitLength", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inc, bitLength);
            return inc;
        }
    }
}
