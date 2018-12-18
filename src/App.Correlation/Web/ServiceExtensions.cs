﻿namespace Naos.Core.App.Correlation.Web
{
    using EnsureThat;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// Extensions on the <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Adds required services to support the Correlation ID functionality.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddNaosCorrelation(this IServiceCollection services)
        {
            EnsureArg.IsNotNull(services, nameof(services));

            services.TryAddSingleton<ICorrelationContextAccessor, CorrelationContextAccessor>();
            services.TryAddTransient<ICorrelationContextFactory, CorrelationContextFactory>();

            return services;
        }
    }
}