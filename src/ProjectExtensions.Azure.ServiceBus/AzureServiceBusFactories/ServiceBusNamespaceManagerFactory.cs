﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectExtensions.Azure.ServiceBus.Interfaces;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using Microsoft.Practices.TransientFaultHandling;

namespace ProjectExtensions.Azure.ServiceBus.AzureServiceBusFactories {

    class ServiceBusNamespaceManagerFactory : INamespaceManager {

        NamespaceManager namespaceManager;

        public ServiceBusNamespaceManagerFactory(IServiceBusTokenProvider tokenProvider) {
            Guard.ArgumentNotNull(tokenProvider, "tokenProvider");
            namespaceManager = new NamespaceManager(tokenProvider.ServiceUri, tokenProvider.TokenProvider);
        }

        public SubscriptionDescription CreateSubscription(SubscriptionDescription description) {
            return namespaceManager.CreateSubscription(description);
        }

        public TopicDescription CreateTopic(TopicDescription description) {
            return namespaceManager.CreateTopic(description);
        }

        public void DeleteSubscription(string topicPath, string name) {
            namespaceManager.DeleteSubscription(topicPath, name);
        }

        public SubscriptionDescription GetSubscription(string topicPath, string name) {
            return namespaceManager.GetSubscription(topicPath, name);
        }

        public TopicDescription GetTopic(string path) {
            return namespaceManager.GetTopic(path);
        }

        public bool SubscriptionExists(string topicPath, string name) {
            return namespaceManager.SubscriptionExists(topicPath, name);
        }

    }
}
