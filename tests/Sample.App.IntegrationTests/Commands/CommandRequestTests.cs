﻿namespace Naos.Sample.App.IntegrationTests.Commands
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using FluentValidation;
    using MediatR;
    using Microsoft.Extensions.DependencyInjection;
    using Naos.Core.Commands.App;
    using Naos.Core.Commands.Infrastructure.FileStorage;
    using Naos.Core.Configuration.App;
    using Naos.Core.FileStorage.Infrastructure;
    using Naos.Foundation;
    using Shouldly;
    using Xunit;

    public class CommandRequestTests : BaseTest
    {
        private readonly IMediator mediator;

        public CommandRequestTests()
        {
            var configuration = NaosConfigurationFactory.Create();

            this.Services
                .AddMediatR(AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.GetName().Name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)).ToArray())
                .AddNaos(configuration, "Product", "Capability", new[] { "All" }, n => n
                    .AddOperations(o => o
                        .AddLogging(correlationId: $"TEST{RandomGenerator.GenerateString(9)}")
                        .AddTracing())
                    .AddCommands(o => o
                        .AddBehavior<TracerCommandBehavior>()
                        .AddBehavior<ValidateCommandBehavior>()
                        .AddBehavior<JournalCommandBehavior>()
                        .AddBehavior(sp => new FileStoragePersistCommandBehavior(
                            new FolderFileStorage(f => f
                                .Folder(Path.Combine(Path.GetTempPath(), "naos_filestorage", "commands")))))));

            this.ServiceProvider = this.Services.BuildServiceProvider();
            this.mediator = this.ServiceProvider.GetService<IMediator>();
        }

        [Fact]
        public void CanInstantiate_Test()
        {
            this.mediator.ShouldNotBeNull();
        }

        [Fact]
        public async Task CanSend_Test()
        {
            // arrange
            var command = new EchoCommand
            {
                Message = "John Doe"
            };

            // act
            var response = await this.mediator.Send(command).AnyContext();

            // assert
            response.ShouldNotBeNull();
            response.Result.ShouldNotBeNull();
            response.Result.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CanSendInvalid_Test()
        {
            // arrange
            var command = new EchoCommand();

            // act
            var response = await this.mediator.Send(command).AnyContext();

            // assert
            //response.ShouldNotBeNull();
            //response.Result.ShouldNotBeNull();
            //response.Result.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CanSend_Factory1_Test()
        {
            // arrange
            var command = Factory.Create(typeof(EchoCommand)) as EchoCommand;
            command.Message = "John Doe";

            // act
            var response = await this.mediator.Send(command).AnyContext() as CommandResponse<EchoCommandResponse>;

            // assert
            response.ShouldNotBeNull();
            response.Result.ShouldNotBeNull();
            response.Result.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CanSend_Factory2_Test()
        {
            // arrange
            var command = Activator.CreateInstance(typeof(EchoCommand), null) as EchoCommand;
            command.Message = "John Doe";

            // act
            var response = await this.mediator.Send(command).AnyContext();

            // assert
            response.ShouldNotBeNull();
            response.Result.ShouldNotBeNull();
            response.Result.Message.ShouldNotBeNullOrEmpty();
        }

        [Fact]
        public async Task CanSend_Factory4_Test()
        {
            // arrange
            var command = SerializationHelper.JsonDeserialize(
                "{\"Message\": \"John Doe\"}",
                typeof(EchoCommand));
            command.ShouldNotBeNull();

            // act
            var response = await this.mediator.Send(command).AnyContext() as CommandResponse<EchoCommandResponse>;

            // assert
            response.ShouldNotBeNull();
            response.Result.ShouldNotBeNull();
            response.Result.Message.ShouldNotBeNullOrEmpty();
        }
    }
}
