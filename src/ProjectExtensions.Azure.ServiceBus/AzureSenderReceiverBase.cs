﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Routing;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceBus;
using NLog;
using Microsoft.Practices.TransientFaultHandling;
using Microsoft.Practices.EnterpriseLibrary.WindowsAzure.TransientFaultHandling.ServiceBus;
using ProjectExtensions.Azure.ServiceBus.TransientFaultHandling.ServiceBus;
using System.Net;
using ProjectExtensions.Azure.ServiceBus.Interfaces;

namespace ProjectExtensions.Azure.ServiceBus {

    /// <summary>
    /// Base class for the sender and receiver client.
    /// </summary>
    abstract class AzureSenderReceiverBase : IDisposable {
        static Logger logger = LogManager.GetCurrentClassLogger();

        internal static string TYPE_HEADER_NAME = "x_proj_ext_type"; //- are not allowed if you filter.

        protected IBusConfiguration configuration;
        protected IServiceBusConfigurationFactory configurationFactory;

        protected RetryPolicy<ServiceBusTransientErrorDetectionStrategy> retryPolicy
            = new RetryPolicy<ServiceBusTransientErrorDetectionStrategy>(30, RetryStrategy.LowMinBackoff, TimeSpan.FromSeconds(5.0), RetryStrategy.LowClientBackoff);
        protected RetryPolicy<ServiceBusTransientErrorToDetermineExistanceDetectionStrategy> verifyRetryPolicy
            = new RetryPolicy<ServiceBusTransientErrorToDetermineExistanceDetectionStrategy>(5, RetryStrategy.LowMinBackoff, TimeSpan.FromSeconds(2.0), RetryStrategy.LowClientBackoff);
        protected IDictionary<string, TopicDescription> topics = new Dictionary<string, TopicDescription>();

        /// <summary>
        /// Base class used to send and receive messages.
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="configurationFactory"></param>
        public AzureSenderReceiverBase(IBusConfiguration configuration, IServiceBusConfigurationFactory configurationFactory) {
            Guard.ArgumentNotNull(configuration, "configuration");
            Guard.ArgumentNotNull(configurationFactory, "configurationFactory");
            this.configuration = configuration;
            this.configurationFactory = configurationFactory;
        }

        protected void EnsureTopic(string topicName) {
            Guard.ArgumentNotNull(topicName, "topicName");
            bool createNew = false;

            try {
                logger.Info("EnsureTopic Try {0} ", topicName);
                // First, let's see if a topic with the specified name already exists.
                topics[topicName] = verifyRetryPolicy.ExecuteAction<TopicDescription>(() => {
                    return configurationFactory.NamespaceManager.GetTopic(topicName);
                });

                createNew = !topics.ContainsKey(topicName);
            }
            catch (MessagingEntityNotFoundException) {
                logger.Info("EnsureTopic Does Not Exist {0} ", topicName);
                // Looks like the topic does not exist. We should create a new one.
                createNew = true;
            }

            // If a topic with the specified name doesn't exist, it will be auto-created.
            if (createNew) {
                try {
                    logger.Info("EnsureTopic CreateTopic {0} ", topicName);
                    var newTopic = new TopicDescription(topicName);

                    topics[topicName] = retryPolicy.ExecuteAction<TopicDescription>(() =>
                    {
                        return configurationFactory.NamespaceManager.CreateTopic(newTopic);
                    });
                }
                catch (MessagingEntityAlreadyExistsException) {
                    logger.Info("EnsureTopic GetTopic {0} ", topicName);
                    // A topic under the same name was already created by someone else, perhaps by another instance. Let's just use it.
                    topics[topicName] = retryPolicy.ExecuteAction<TopicDescription>(() =>
                    {
                        return configurationFactory.NamespaceManager.GetTopic(topicName);
                    });
                }
            }
        }

        protected IEnumerable<string> GetTopicNamesForMessageType(Type messageType) {
            for (int i = 0; i < configuration.TopicsPerMessage; i++) {
                yield return GetTopicName(messageType, i);
            }
        }

        protected string GetRandomTopicNameForMessageType(Type messageType) {
            int index = new Random().Next(0, configuration.TopicsPerMessage - 1);
            return GetTopicName(messageType, index);
        }

        public void Dispose() {
            Dispose(true);
            configurationFactory.MessageFactory.Close();
        }

        public abstract void Dispose(bool disposing);

        private string GetTopicName(Type messageType, int partitionNumber) {
            return messageType.Name + partitionNumber;
        }
    }
}
