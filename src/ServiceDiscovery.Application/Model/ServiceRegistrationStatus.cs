﻿namespace Naos.ServiceDiscovery.Application
{
    using System;

    public class ServiceRegistrationStatus
    {
        public string RegistrationId { get; set; }

        public bool Healthy { get; set; } = true;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
