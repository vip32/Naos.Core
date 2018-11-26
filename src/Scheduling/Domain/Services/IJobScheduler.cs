﻿namespace Naos.Core.Scheduling.Domain
{
    using System;
    using System.Threading.Tasks;

    public interface IJobScheduler
    {
        bool IsRunning { get; }

        IJobScheduler OnError(Action<Exception> errorHandler);

        IJobScheduler Register(string cron, Action<string[]> action);

        IJobScheduler Register(string key, string cron, Action<string[]> action);

        IJobScheduler Register(string cron, Func<string[], Task> task);

        IJobScheduler Register(string key, string cron, Func<string[], Task> task);

        IJobScheduler Register(JobRegistration registration, IJob job);

        IJobScheduler Register<T>(string cron, string[] args = null)
            where T : IJob;

        IJobScheduler Register<T>(string key, string cron, string[] args = null)
            where T : IJob;

        Task RunAsync();

        Task RunAsync(DateTime moment);

        Task TriggerAsync(string key);

        IJobScheduler UnRegister(string key);

        IJobScheduler UnRegister(JobRegistration registration);
    }
}