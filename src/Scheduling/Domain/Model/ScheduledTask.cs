﻿namespace Naos.Core.Scheduling.Domain
{
    using System;
    using System.Threading.Tasks;
    using EnsureThat;

    public class ScheduledTask : IScheduledTask
    {
        private readonly Func<string[], Task> func;
        private readonly Action<string[]> action;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTask"/> class.
        /// </summary>
        /// <param name="cron">The cron expression.</param>
        /// <param name="action">The action.</param>
        public ScheduledTask(Action<string[]> action)
        {
            EnsureArg.IsNotNull(action, nameof(action));

            this.action = action;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTask"/> class.
        /// </summary>
        /// <param name="cron">The cron expression.</param>
        /// <param name="func">The func task.</param>
        public ScheduledTask(Func<string[], Task> func)
        {
            EnsureArg.IsNotNull(func, nameof(func));

            this.func = func;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduledTask"/> class.
        /// </summary>
        /// <param name="cron">The cron.</param>
        protected ScheduledTask()
        {
        }

        // TODO: Other things to schedule
        // Schedule - Command
        // Schedule - Message

        public virtual async Task ExecuteAsync(string[] args = null)
        {
            if(this.func != null)
            {
                await this.func(args).ConfigureAwait(false);
            }
            else if (this.action != null)
            {
                this.action(args);
            }
        }

        // TODO: action/func/type containing the thing to invoke when due
    }
}
