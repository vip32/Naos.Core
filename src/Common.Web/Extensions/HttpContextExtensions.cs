﻿namespace Naos.Core.Common.Web
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Primitives;

    /// <summary>
    ///     Extends the HttpContext type
    /// </summary>
    public static partial class HttpContextExtensions
    {
        public static void SetCorrelationId(this HttpContext context, string value)
        {
            if (context == null)
            {
                return;
            }

            if (context.Items.ContainsKey("correlationId"))
            {
                context.Items.Remove("correlationId");
            }

            context.Items.Add("correlationId", value);
        }

        public static string GetCorrelationId(this HttpContext context)
        {
            if (context == null)
            {
                return default;
            }

            context.Items.TryGetValue("correlationId", out object value);
            return value?.ToString();
        }

        public static void SetRequestId(this HttpContext context, string value)
        {
            if (context == null)
            {
                return;
            }

            if (context.Items.ContainsKey("requestId"))
            {
                context.Items.Remove("requestId");
            }

            context.Items.Add("requestId", value);
        }

        public static string GetRequestId(this HttpContext context)
        {
            if (context == null)
            {
                return default;
            }

            context.Items.TryGetValue("requestId", out object value);
            return value?.ToString();
        }
    }
}