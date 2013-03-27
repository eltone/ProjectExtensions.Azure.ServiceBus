using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System.Threading.Tasks;
using System.IO;
using ProjectExtensions.Azure.ServiceBus.Serialization;
using System.Threading;
using System.Reflection;
using NLog;
using Microsoft.Practices.TransientFaultHandling;
using System.Net;
using ProjectExtensions.Azure.ServiceBus.Interfaces;
using ProjectExtensions.Azure.ServiceBus.Wrappers;

namespace ProjectExtensions.Azure.ServiceBus.Receiver {

    /// <summary>
    /// Receiver of Service Bus messages.
    /// </summary>
    class AzureBusReceiver : AzureSenderReceiverBase, IAzureBusReceiver {

        static Logger logger = LogManager.GetCurrentClassLogger();

        object lockObject = new object();

        List<AzureReceiverHelper> mappings = new List<AzureReceiverHelper>();

        IServiceBusSerializer serializer;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="configuration">The configuration data</param>
        /// <param name="configurationFactory"></param>
        /// <param name="serializer"></param>
        public AzureBusReceiver(IBusConfiguration configuration, IServiceBusConfigurationFactory configurationFactory, IServiceBusSerializer serializer)
            : base(configuration, configurationFactory) {
            Guard.ArgumentNotNull(serializer, "serializer");
            this.serializer = serializer;
        }

        /// <summary>
        /// Create a new Subscription.
        /// </summary>
        /// <param name="value">The data used to create the subscription</param>
        public void CreateSubscription(ServiceBusEnpointData value) {
            Guard.ArgumentNotNull(value, "value");

            foreach (var topicName in GetTopicNamesForMessageType(value.MessageType)) {
                EnsureTopic(topicName);

                lock (lockObject) {

                    logger.Info("CreateSubscription {0} Declared {1} MessageTytpe {2}, IsReusable {3} Custom Attribute {4}",
                                value.SubscriptionName,
                                value.DeclaredType.ToString(),
                                value.MessageType.ToString(),
                                value.IsReusable,
                                value.AttributeData != null ? value.AttributeData.ToString() : string.Empty);

                    var helper = new AzureReceiverHelper(topics[topicName], configurationFactory, configuration, serializer, verifyRetryPolicy, retryPolicy, value);
                    mappings.Add(helper);
                    //helper.ProcessMessagesForSubscription();

                    // TODO: use the generated tasks and pass in cancelation tokens to handle receive loop spinning down
                    if (value.AttributeData != null && value.AttributeData.ThreadsPerSubscription > 0) {
                        for (int i = 0; i < value.AttributeData.ThreadsPerSubscription; i++) {
                            Task.Factory.StartNew(helper.ProcessMessagesForSubscription, TaskCreationOptions.LongRunning);
                        }
                    }
                    else {
                        Task.Factory.StartNew(helper.ProcessMessagesForSubscription, TaskCreationOptions.LongRunning);
                    }
                } //lock end
            }
        }

        /// <summary>
        /// Cancel a subscription
        /// </summary>
        /// <param name="value">The data used to cancel the subscription</param>
        public void CancelSubscription(ServiceBusEnpointData value) {
            Guard.ArgumentNotNull(value, "value");

            logger.Info("CancelSubscription {0} Declared {1} MessageTytpe {2}, IsReusable {3}", value.SubscriptionName, value.DeclaredType.ToString(), value.MessageType.ToString(), value.IsReusable);

            var subscriptions = mappings.Where(item => item.Data.EndPointData.SubscriptionName.Equals(value.SubscriptionName, StringComparison.OrdinalIgnoreCase));

            if (!subscriptions.Any()) {
                logger.Info("CancelSubscription Does not exist {0}", value.SubscriptionNameDebug);
                return;
            }

            foreach (var subscription in subscriptions) {
                subscription.Data.Cancel();

                Task t = Task.Factory.StartNew(() => {
                    //HACK find better way to wait for a cancel request so we are not blocking.
                    logger.Info("CancelSubscription Deleting {0}", value.SubscriptionNameDebug);
                    for (int i = 0; i < 100; i++) {
                        if (!subscription.Data.Cancelled) {
                            Thread.Sleep(1000);
                        }
                        else {
                            break;
                        }
                    }

                    TopicDescription topic = topics[value.MessageType.Name];

                    if (topic != null && configurationFactory.NamespaceManager.SubscriptionExists(topic.Path, value.SubscriptionName)) {
                        retryPolicy.ExecuteAction(() => configurationFactory.NamespaceManager.DeleteSubscription(topic.Path, value.SubscriptionName));
                        logger.Info("CancelSubscription Deleted {0}", value.SubscriptionNameDebug);
                    }
                });

                try {
                    Task.WaitAny(t);
                }
                catch (Exception ex) {
                    if (ex is AggregateException) {
                        //do nothing
                    }
                    else {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        public override void Dispose(bool disposing) {
            foreach (var item in mappings) {
                ExtensionMethods.ExecuteAndReturn(() => {
                    if (item.Data.Client != null) {
                        item.Data.Client.Close();
                    }
                });
                ExtensionMethods.ExecuteAndReturn(() => {
                    if (item is IDisposable) {
                        (item as IDisposable).Dispose();
                    }
                });
            }
            mappings.Clear();
        }
    }
}
