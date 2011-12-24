#ProjectExtensions.Azure.ServiceBus

An easier way to work with the Azure service bus.

Follow me or tweet at me on Twitter: @joefeser.

##Building 

Use ClickToBuild.bat to build.  You will need to have the Azure SDK 1.5 installed in order to build.

##Nuget Packages

_Note that the additional Nuget packages for additional IoC support are not available yet_

If you don't use an IoC container in your application or you are happy to use Autofac, download the default Nuget package:

* ProjectExtensions.Azure.ServiceBus

If you want to use a specific IoC container, grab our core package:

* ProjectExtensions.Azure.ServiceBus.Core

You can then either implement IAzureBusContainer for your IoC of choice or grab one of the pre-built options:

* Autofac in ProjectExtensions.Azure.ServiceBus.IOC.Autofac
* Castle Windsor in ProjectExtensions.Azure.ServiceBus.IOC.CastleWindsor

We will be adding support for additional IoC containers in the future.  If you have a favorite, let us know, or, better yet, contribute an implementation.

##Getting started

1. Create a console application
2. Using NuGet, install the package ProjectExtensions.Azure.ServiceBus.
3. Optionally Add a reference to NLog
4. Create a Message Class that you wish to handle:

```csharp
public class TestMessage {
  
    public string MessageId {
        get;
        set;
    }

    public int Value {
        get;
        set;
    }
}
```

5\. Create a Handler that will receive notifications when the message is placed on the bus:

```csharp
public class TestMessageSubscriber : IHandleMessages<TestMessage> {

    static Logger logger = LogManager.GetCurrentClassLogger();

    public void Handle(IReceivedMessage<TestMessage> message) {
        logger.Log(LogLevel.Info, "Message received: {0} {1}", message.Message.Value, message.Message.MessageId);
    }
}
```


6\. Place initialization code at the beginning of your method or in your BootStrapper.  You will need a couple of using declarations:

```csharp
using ProjectExtensions.Azure.ServiceBus;
using ProjectExtensions.Azure.ServiceBus.Autofac.Container;
```

Basic setup code (assuming you want to put Azure configuration information in your application configuration file):

```csharp
ProjectExtensions.Azure.ServiceBus.BusConfiguration.WithSettings()
    .UseAutofacContainer()
    .ReadFromConfigFile()
    .ServiceBusApplicationId("AppName")
    .RegisterAssembly(typeof(TestMessageSubscriber).Assembly)
    .Configure();
```

And configuration:

```xml
<add key="ServiceBusIssuerKey" value="base64hash" />
<add key="ServiceBusIssuerName" value="owner" />
//https://addresshere.servicebus.windows.net/
<add key="ServiceBusNamespace" value="namespace set up in service bus (addresshere) portion" />
```

Otherwise, you can configure everything in code:

```csharp
ProjectExtensions.Azure.ServiceBus.BusConfiguration.WithSettings()
	.UseAutofacContainer()
    .ServiceBusApplicationId("AppName")
    .ServiceBusIssuerKey("[sb password]")
    .ServiceBusIssuerName("owner")
    .ServiceBusNamespace("[addresshere]")
    .RegisterAssembly(typeof(TestMessageSubscriber).Assembly)
    .Configure();
```

7\. Put some messages on the Bus:

```csharp
for (int i = 0; i < 20; i++) {
    var message1 = new TestMessage() {
        Value = i,
        MessageId = DateTime.Now.ToString()
    };
    BusConfiguration.Instance.Bus.Publish(message1, null);
}
```

Watch your method get called.

Welcome to Azure Service Bus.

##Using Castle Windsor Instead of Autofac

Unless otherwise noted, everything works as shown in the getting starting section above.

1. Install the Nuget packages ProjectExtensions.Azure.ServiceBus.Core and ProjectExtensions.Azure.ServiceBus.IOC.CastleWindsor instead of ProjectExtensions.Azure.ServiceBus
2. Use this initialization code at the beginning of your method or in your BootStrapper.  You will need a couple of using declarations:

```csharp
using ProjectExtensions.Azure.ServiceBus;
using ProjectExtensions.Azure.ServiceBus.CastleWindsor.Container;
```

Basic setup code (assuming you want to put Azure configuration information in your application configuration file):

```csharp
ProjectExtensions.Azure.ServiceBus.BusConfiguration.WithSettings()
    .UseCastleWindsorContainer()
    .ReadFromConfigFile()
    .ServiceBusApplicationId("AppName")
    .RegisterAssembly(typeof(TestMessageSubscriber).Assembly)
    .Configure();
```

Otherwise, you can configure everything in code:

```csharp
ProjectExtensions.Azure.ServiceBus.BusConfiguration.WithSettings()
	.UseCastleWindsorContainer()
    .ServiceBusApplicationId("AppName")
    .ServiceBusIssuerKey("[sb password]")
    .ServiceBusIssuerName("owner")
    .ServiceBusNamespace("[addresshere]")
    .RegisterAssembly(typeof(TestMessageSubscriber).Assembly)
    .Configure();
```

##Release Notes

###Version 0.8.4

* Allow support for other IoC containers to be added.  Continue to support Autofac.
* Support for Castle Windsor IoC.
* BREAKING CHANGE.  Move Autofac support into seperate DLL.  Existing implementations need to add a reference to ProjectExtensions.Azure.ServiceBus.Autofac and change initialization code as shown in the getting started example.