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

        public FileStoragePersistCommandBehavior(IFileStorage storage, ISerializer serializer = null, string pathTemplate = "{id}-{type}.json")
        {
            EnsureArg.IsNotNull(storage, nameof(storage));

            this.storage = storage;
            this.serializer = serializer ?? new JsonNetSerializer(DefaultJsonSerializerSettings.Create());
            this.pathTemplate = pathTemplate ?? "{id}-{type}";
        }

        /// <summary>
        /// Executes this behavior for the specified command.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The command.</param>
        public async Task<CommandBehaviorResult> ExecuteAsync<TResponse>(Command<TResponse> request)
        {
            EnsureArg.IsNotNull(request);

            var path = this.pathTemplate
                .Replace("{id}", request.Id)
                .Replace("{type}", request.GetType().PrettyName(false));

            await this.storage.SaveFileObjectAsync(path, request, this.serializer).AnyContext();

            return await Task.FromResult(new CommandBehaviorResult()).AnyContext();
        }
    }
}
