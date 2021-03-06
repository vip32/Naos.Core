﻿namespace Naos.Messaging.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using EnsureThat;
    using Microsoft.Extensions.Logging;
    using Naos.FileStorage.Domain;
    using Naos.Foundation;
    using Naos.Messaging.Domain;
    using Naos.Tracing.Domain;
    using Newtonsoft.Json;

    public class FileStorageMessageBroker : IMessageBroker
    {
        private readonly FileStorageMessageBrokerOptions options;
        private readonly ILogger<FileStorageMessageBroker> logger;
        private readonly IDictionary<string, FileSystemWatcher> watchers = new Dictionary<string, FileSystemWatcher>();

        public FileStorageMessageBroker(FileStorageMessageBrokerOptions options)
        {
            EnsureArg.IsNotNull(options, nameof(options));
            EnsureArg.IsNotNull(options.HandlerFactory, nameof(options.HandlerFactory));
            EnsureArg.IsNotNull(options.Storage, nameof(options.Storage));

            this.options = options;
            this.options.Folder = options.Folder.EmptyToNull() ?? Path.GetTempPath();
            this.options.Subscriptions = options.Subscriptions ?? new SubscriptionMap();
            this.options.MessageScope = options.MessageScope ?? AppDomain.CurrentDomain.FriendlyName;
            this.logger = options.CreateLogger<FileStorageMessageBroker>();
        }

        public FileStorageMessageBroker(Builder<FileStorageMessageBrokerOptionsBuilder, FileStorageMessageBrokerOptions> optionsBuilder)
            : this(optionsBuilder(new FileStorageMessageBrokerOptionsBuilder()).Build())
        {
        }

        public IMessageBroker Subscribe<TMessage, THandler>()
            where TMessage : Message
            where THandler : IMessageHandler<TMessage>
        {
            var messageName = typeof(TMessage).PrettyName(false);

            if (!this.options.Subscriptions.Exists<TMessage>())
            {
                if (!this.watchers.ContainsKey(messageName))
                {
                    var path = this.GetDirectory(messageName, this.options.FilterScope);
                    this.logger.LogJournal(LogKeys.AppMessaging, $"message subscribe: {typeof(TMessage).PrettyName()} (service={{Service}}, filterScope={{FilterScope}}, handler={{MessageHandlerType}}, watch={path})", LogPropertyKeys.TrackSubscribeMessage, args: new[] { this.options.MessageScope, this.options.FilterScope, typeof(THandler).Name });
                    this.EnsureDirectory(path);

                    var watcher = new FileSystemWatcher(path)
                    {
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                        EnableRaisingEvents = true,
                        Filter = "*.json"
                    };

                    watcher.Renamed += (sender, e) =>
                    {
                        this.ProcessMessage(e.FullPath).GetAwaiter().GetResult(); // TODO: async!
                    };

                    this.watchers.Add(messageName, watcher);
                    this.logger.LogDebug("{LogKey:l} filesystem onrenamed handler registered (name={messageName})", LogKeys.AppMessaging, typeof(TMessage).PrettyName());

                    this.options.Subscriptions.Add<TMessage, THandler>(messageName);
                }
            }

            return this;
        }

        public void Publish(Message message)
        {
            EnsureArg.IsNotNull(message, nameof(message));

            if (message.CorrelationId.IsNullOrEmpty())
            {
                message.CorrelationId = IdGenerator.Instance.Next;
            }

            var messageName = /*message.Name*/ message.GetType().PrettyName(false);
            using (this.logger.BeginScope(new Dictionary<string, object>
            {
                [LogPropertyKeys.CorrelationId] = message.CorrelationId,
            }))
            using (var scope = this.options.Tracer?.BuildSpan(messageName, LogKeys.AppMessaging, SpanKind.Producer).Activate(this.logger))
            {
                if (message.Id.IsNullOrEmpty())
                {
                    message.Id = IdGenerator.Instance.Next;
                    this.logger.LogDebug($"{{LogKey:l}} set message (id={message.Id})", LogKeys.AppMessaging);
                }

                if (message.Origin.IsNullOrEmpty())
                {
                    message.Origin = this.options.MessageScope;
                    this.logger.LogDebug($"{{LogKey:l}} set message (origin={message.Origin})", LogKeys.AppMessaging);
                }

                // store message in specific (Message) folder
                this.logger.LogJournal(LogKeys.AppMessaging, $"message publish: {message.GetType().PrettyName()} (id={{MessageId}}, origin={{MessageOrigin}})", LogPropertyKeys.TrackPublishMessage, args: new[] { message.Id, message.Origin });
                this.logger.LogTrace(LogKeys.AppMessaging, message.Id, message.GetType().PrettyName(), LogTraceNames.Message);

                if (scope?.Span != null)
                {
                    // propagate the span infos
                    message.Properties.AddOrUpdate("TraceId", scope.Span.TraceId);
                    message.Properties.AddOrUpdate("SpanId", scope.Span.SpanId);
                }

                var path = Path.Combine(this.GetDirectory(messageName, this.options.FilterScope), $"message_{message.Id}_{this.options.MessageScope}.json.tmp");
                if (this.options.Storage.SaveFileObjectAsync(path, message).Result)
                {
                    this.options.Storage.RenameFileAsync(path, path.SliceTillLast("."));
                }

                if (this.options.Mediator != null)
                {
                    // TODO: async publish!
                    /*await */
                    this.options.Mediator.Publish(new MessagePublishedDomainEvent(message)).GetAwaiter().GetResult(); /*.AnyContext();*/
                }
            }
        }

        public void Unsubscribe<TMessage, THandler>()
            where TMessage : Message
            where THandler : IMessageHandler<TMessage>
        {
            // remove folder watch
            throw new NotImplementedException();
        }

        /// <summary>
        /// Processes the message by invoking the message handler.
        /// </summary>
        /// <param name="path"></param>
        private async Task<bool> ProcessMessage(string path)
        {
            var processed = false;
            var messageName = path.SliceTillLast(@"\").SliceFromLast(@"\");
            var messageBody = this.GetFileContents(path);

            if (this.options.Subscriptions.Exists(messageName))
            {
                foreach (var subscription in this.options.Subscriptions.GetAll(messageName))
                {
                    var messageType = this.options.Subscriptions.GetByName(messageName);
                    if (messageType == null)
                    {
                        continue;
                    }

                    var jsonMessage = JsonConvert.DeserializeObject(messageBody, messageType);
                    if (!(jsonMessage is Message message))
                    {
                        return false;
                    }

                    if (message.Origin.IsNullOrEmpty())
                    {
                        //message.CorrelationId = jsonMessage.AsJToken().GetStringPropertyByToken("CorrelationId");
                        message.Origin = path.SliceFromLast("_").SliceTillLast("."); // read metadata from filename
                    }

                    // get parent span infos from message
                    ISpan parentSpan = null;
                    if (message.Properties.ContainsKey("TraceId") && message.Properties.ContainsKey("SpanId"))
                    {
                        // dehydrate parent span
                        parentSpan = new Span(message.Properties["TraceId"] as string, message.Properties["SpanId"] as string);
                    }

                    using (this.logger.BeginScope(new Dictionary<string, object>
                    {
                        [LogPropertyKeys.CorrelationId] = message.CorrelationId,
                        //[LogPropertyKeys.TrackId] = scope.Span.SpanId = allready done in Span ScopeManager (activate)
                    }))
                    using (var scope = this.options.Tracer?.BuildSpan(messageName, LogKeys.AppMessaging, SpanKind.Consumer, parentSpan).Activate(this.logger))
                    {
                        this.logger.LogJournal(LogKeys.AppMessaging, $"message processed: {messageType.PrettyName()} (id={{MessageId}}, name={{MessageName}}, service={{Service}}, origin={{MessageOrigin}})", LogPropertyKeys.TrackReceiveMessage, args: new[] { message?.Id, this.options.MessageScope, message.Origin });
                        this.logger.LogTrace(LogKeys.AppMessaging, message?.Id, message.GetType().PrettyName(), LogTraceNames.Message);

                        if (scope?.Span != null)
                        {
                            // propagate the span infos
                            message.Properties.AddOrUpdate("TraceId", scope.Span.TraceId);
                            message.Properties.AddOrUpdate("SpanId", scope.Span.SpanId);
                        }

                        // construct the handler by using the DI container
                        var handler = this.options.HandlerFactory.Create(subscription.HandlerType); // should not be null, did you forget to register your generic handler (EntityMessageHandler<T>)
                        var concreteType = typeof(IMessageHandler<>).MakeGenericType(messageType);

                        var method = concreteType.GetMethod("Handle");
                        if (handler != null && method != null)
                        {
                            // TODO: async publish!
                            if (this.options.Mediator != null)
                            {
                                await this.options.Mediator.Publish(new MessageHandledDomainEvent(message, this.options.MessageScope)).AnyContext();
                            }

                            await ((Task)method.Invoke(handler, new object[] { jsonMessage as object })).AnyContext();
                        }
                        else
                        {
                            this.logger.LogWarning("{LogKey:l} process failed, message handler could not be created. is the handler registered in the service provider? (name={MessageName}, service={Service}, id={MessageId}, origin={MessageOrigin})",
                                LogKeys.AppMessaging, messageType.PrettyName(), this.options.MessageScope, message?.Id, message.Origin);
                        }
                    }
                }

                processed = true;
            }
            else
            {
                this.logger.LogDebug($"{{LogKey:l}} unprocessed: {messageName}", LogKeys.AppMessaging);
            }

            return processed;
        }

        private string GetDirectory(string messageName, string filterScope)
        {
            return $@"{this.options.Folder.EmptyToNull() ?? Path.GetTempPath()}naos_messaging\{filterScope}\{messageName}\".Replace("\\", @"\");
        }

        private void EnsureDirectory(string fullPath)
        {
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        private string GetFileContents(string path)
        {
            Thread.Sleep(this.options.ProcessDelay); // this helps with locked files
            return this.options.Storage.GetFileContentsAsync(path).Result;
        }
    }
}
