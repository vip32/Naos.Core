﻿namespace Naos.Core.Messaging.Infrastructure.Azure
{
    using System.Net.Http;
    using MediatR;
    using Naos.Core.Common;
    using Naos.Core.Infrastructure.Azure;

    public class SignalRServerlessMessageBrokerOptions : BaseOptions
    {
        public IMediator Mediator { get; set; }

        public IMessageHandlerFactory HandlerFactory { get; set; }

        public SignalRConfiguration Configuration { get; set; }

        public IHttpClientFactory HttpClient { get; set; }

        public ISubscriptionMap Map { get; set; }

        public string FilterScope { get; set; }

        public string MessageScope { get; set; } = "local";
    }
}