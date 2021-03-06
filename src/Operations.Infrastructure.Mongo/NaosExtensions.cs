﻿namespace Microsoft.Extensions.DependencyInjection
{
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using EnsureThat;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using MongoDB.Driver;
    using Naos.Foundation.Domain;
    using Naos.Foundation.Infrastructure;
    using Naos.Operations.Domain;
    using Naos.Operations.Infrastructure.Mongo;

    [ExcludeFromCodeCoverage]
    public static class NaosExtensions
    {
        public static INaosBuilderContext AddMongoLogging(this INaosBuilderContext context)
        {
            EnsureArg.IsNotNull(context, nameof(context));
            EnsureArg.IsNotNull(context.Services, nameof(context.Services));

            var configuration = context.Configuration?.GetSection("naos:operations:logging:mongo").Get<MongoLoggingConfiguration>();
            if (configuration != null)
            {
                context.Services.AddMongoClient("logging", new MongoConfiguration
                {
                    ConnectionString = configuration.ConnectionString?.Replace("[DATABASENAME]", configuration.DatabaseName),
                    DatabaseName = configuration.DatabaseName
                });

                context.Services.AddScoped<ILogEventRepository>(sp =>
                {
                    return new MongoLogEventRepository(o => o
                        .LoggerFactory(sp.GetRequiredService<ILoggerFactory>())
                        .MongoClient(sp.GetServices<IMongoClient>()
                            .FirstOrDefault(c => c.Settings.ApplicationName == "logging")) //TODO: make nice extension to get a named mongoclient
                        .Mapper(new AutoMapperEntityMapper(MapperFactory.Create()))
                        .DatabaseName(configuration.DatabaseName)
                        .CollectionName(configuration.CollectionName));
                });
                context.Messages.Add($"naos services builder: logging azure mongo repository added (collection={configuration.CollectionName})");
            }

            return context;
        }
    }
}
