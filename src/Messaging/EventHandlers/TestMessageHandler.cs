﻿namespace Naos.Core.Messaging
{
    using System.Threading.Tasks;
    using EnsureThat;
    using Microsoft.Extensions.Logging;
    using Naos.Core.Common;

    public class TestMessageHandler : IMessageHandler<TestMessage>
    {
        protected readonly ILogger<TestMessageHandler> logger;

        public TestMessageHandler(ILogger<TestMessageHandler> logger)
        {
            EnsureArg.IsNotNull(logger, nameof(logger));

            this.logger = logger;
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The event.</param>
        /// <returns></returns>
        public virtual Task Handle(TestMessage message)
        {
            using (this.logger.BeginScope("{CorrelationId}", message.CorrelationId))
            {
                this.logger.LogInformation("MESSAGE handle  (name={MessageName}, id={MessageId}, origin={MessageOrigin}) " + message.Data, message.GetType().PrettyName(), message.Id, message.Origin);

                return Task.CompletedTask;
            }
        }
    }
}