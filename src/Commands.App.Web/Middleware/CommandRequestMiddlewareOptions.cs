﻿namespace Naos.Core.Commands.App.Web
{
    using Naos.Foundation;

    /// <summary>
    /// Options for request command middleware.
    /// </summary>
    public class CommandRequestMiddlewareOptions
    {
        public CommandRequestRegistration Registration { get; set; }

        public RouteMatcher RouteMatcher { get; set; }
    }
}
