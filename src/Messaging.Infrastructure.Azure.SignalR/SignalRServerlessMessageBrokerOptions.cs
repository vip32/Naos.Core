﻿namespace Naos.Messaging.Infrastructure.Azure
{
    using System.Net.Http;
    using MediatR;
    using Naos.Foundation;
    using Naos.Messaging.Domain;
    using Naos.Tracing.Domain;

    public class SignalRServerlessMessageBrokerOptions : BaseOptions
    {
        public ITracer Tracer { get; set; }

        public IMediator Mediator { get; set; }

        public ISerializer Serializer { get; set; }

        public IMessageHandlerFactory HandlerFactory { get; set; }

        public string ConnectionString { get; set; }

        public IHttpClientFactory HttpClient { get; set; }

        public ISubscriptionMap Map { get; set; }

        public string FilterScope { get; set; }

        public string MessageScope { get; set; } = "local";
    }
}
