﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectExtensions.Azure.ServiceBus.Serialization;

namespace ProjectExtensions.Azure.ServiceBus.Container {
    
    /// <summary>
    /// Base class for all Containers.
    /// </summary>
    public abstract class AzureBusContainerBase : IAzureBusContainer {

        /// <summary>
        /// Resolve component type of T.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract T Resolve<T>() where T : class;

        /// <summary>
        /// Resolve component.
        /// </summary>
        /// <param name="t">The type to resolve</param>
        /// <returns></returns>
        public abstract object Resolve(Type t);

        /// <summary>
        /// Register an implementation for a service type.
        /// </summary>
        /// <param name="serviceType">The service type.</param>
        /// <param name="implementationType">The implementation type.</param>
        /// <param name="perInstance">True creates an instance each time resolved.  False uses a singleton instance for the entire lifetime of the process.</param>
        public abstract void Register(Type serviceType, Type implementationType, bool perInstance = false);

        /// <summary>
        /// Registers the configuration instance with the bus if it is not already registered
        /// </summary>
        public abstract void RegisterConfiguration();

        /// <summary>
        /// Build the container if needed.
        /// </summary>
        public abstract void Build();

        /// <summary>
        /// Return true if the given type is registered with the container.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public abstract bool IsRegistered(Type type);

        /// <summary>
        /// Return the Service Bus
        /// </summary>
        public virtual IBus Bus {
            get {
                return Resolve<IBus>();
            }
        }

        /// <summary>
        /// Resolve the Default Serializer
        /// </summary>
        public virtual IServiceBusSerializer DefaultSerializer {
            get {
                return Resolve<IServiceBusSerializer>();
            }
        }
    }
}
