﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectExtensions.Azure.ServiceBus;
using NLog;

namespace PubSubUsingConfiguration {

    public class AnotherTestMessageSubscriber : IHandleCompetingMessages<AnotherTestMessage> {

        static Logger logger = LogManager.GetCurrentClassLogger();

        public void Handle(IReceivedMessage<AnotherTestMessage> message, IDictionary<string, object> metadata) {
            logger.Info("AnotherTestMessageSubscriber Message received: {0} {1}", message.Message.Value, message.Message.MessageId);
        }
    }
}
