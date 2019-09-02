﻿namespace Microsoft.Extensions.DependencyInjection
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using EnsureThat;
    using Microsoft.Extensions.Logging;
    using Naos.Core.Commands.App;
    using Naos.Core.Commands.App.Web;
    using Naos.Core.FileStorage;
    using Naos.Core.FileStorage.Domain;
    using Naos.Core.FileStorage.Infrastructure;
    using Naos.Foundation;
    using NSwag.Generation.Processors;

    [ExcludeFromCodeCoverage]
    public static class NaosExtensions
    {
        public static CommandsOptions AddRequests(
            this CommandsOptions options,
            Action<CommandRequestOptions> optionsAction = null,
            bool addDefaultRequestCommands = true)
        {
            EnsureArg.IsNotNull(options, nameof(options));
            EnsureArg.IsNotNull(options.Context, nameof(options.Context));

            if (addDefaultRequestCommands)
            {
                options.Context.Services.AddSingleton<CommandRequestRegistration>(sp =>
                    new CommandRequestRegistration<EchoCommand, EchoCommandResponse> { Route = "api/commands/echo", RequestMethod = "post" });
                options.Context.Services.AddSingleton<CommandRequestRegistration>(sp =>
                    new CommandRequestRegistration<EchoCommand, EchoCommandResponse> { Route = "api/commands/echo", RequestMethod = "get" });
                options.Context.Services.AddSingleton<CommandRequestRegistration>(sp =>
                    new RequestCommandRegistration<PingCommand> { Route = "api/commands/ping", RequestMethod = "get" });
            }

            optionsAction?.Invoke(new CommandRequestOptions(options.Context));
            options.Context.Services.AddSingleton<IDocumentProcessor, CommandRequestDocumentProcessor>();
            options.Context.Services.AddStartupTask<CommandRequestQueueProcessor>(new TimeSpan(0, 0, 3));
            // TODO: make configurable/optional
            options.Context.Services.AddSingleton(sp => new CommandRequestStorage(
                new FileStorageLoggingDecorator(
                    sp.GetRequiredService<ILoggerFactory>(),
                    new FolderFileStorage(o => o.Folder(Path.Combine(Path.GetTempPath(), "naos_commands/requests")))))); // optional

            // needed for request dispatcher extensions, so they can be used on the registrations
            options.Context.Services
                .Scan(scan => scan // https://andrewlock.net/using-scrutor-to-automatically-register-your-services-with-the-asp-net-core-di-container/
                    .FromExecutingAssembly()
                    .FromApplicationDependencies(a => !a.FullName.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) && !a.FullName.StartsWith("System", StringComparison.OrdinalIgnoreCase))
                    .AddClasses(classes => classes.AssignableTo(typeof(ICommandRequestExtension)), true));

            options.Context.Messages.Add($"{LogKeys.Startup} naos services builder: command request dispatcher added"); // TODO: list available command + routes

            return options;
        }
    }
}