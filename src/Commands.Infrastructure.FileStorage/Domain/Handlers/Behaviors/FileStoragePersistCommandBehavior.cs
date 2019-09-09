﻿namespace Naos.Core.Commands.Infrastructure.FileStorage
{
    using System.Threading.Tasks;
    using EnsureThat;
    using Naos.Core.Commands.App;
    using Naos.Core.FileStorage.Domain;
    using Naos.Foundation;

    public class FileStoragePersistCommandBehavior : ICommandBehavior
    {
        private readonly IFileStorage storage;
        private readonly ISerializer serializer;
        private readonly string pathTemplate;
        private ICommandBehavior next;

        public FileStoragePersistCommandBehavior(IFileStorage storage, ISerializer serializer = null, string pathTemplate = "{id}-{type}.json")
        {
            EnsureArg.IsNotNull(storage, nameof(storage));

            this.storage = storage;
            this.serializer = serializer ?? new JsonNetSerializer(DefaultJsonSerializerSettings.Create());
            this.pathTemplate = pathTemplate ?? "{id}-{type}";
        }

        public ICommandBehavior SetNext(ICommandBehavior next)
        {
            this.next = next;
            return next;
        }

        /// <summary>
        /// Executes this behavior for the specified command.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The command.</param>
        public async Task ExecutePreHandleAsync<TResponse>(Command<TResponse> request, CommandBehaviorResult result)
        {
            EnsureArg.IsNotNull(request);

            var path = this.pathTemplate
                .Replace("{id}", request.Id)
                .Replace("{type}", request.GetType().PrettyName(false));

            await this.storage.SaveFileObjectAsync(path, request, this.serializer).AnyContext();

            if (!result.Cancelled && this.next != null)
            {
                await this.next.ExecutePreHandleAsync(request, result).AnyContext();
            }

            // terminate here
        }

        public async Task ExecutePostHandleAsync<TResponse>(CommandResponse<TResponse> response, CommandBehaviorResult result)
        {
            if (!result.Cancelled && this.next != null)
            {
                await this.next.ExecutePostHandleAsync(response, result).AnyContext();
            }
        }
    }
}
