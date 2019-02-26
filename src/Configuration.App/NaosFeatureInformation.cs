﻿namespace Naos.Core.Configuration.App
{
    using System.Collections.Generic;

    public class NaosFeatureInformation
    {
        public bool? Enabled { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string EchoUri { get; set; }

        public IDictionary<string, string> SampleUris { get; set; }
    }
}
