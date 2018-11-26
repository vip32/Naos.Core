﻿namespace Naos.Sample.App.IntegrationTests.Scheduling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Naos.Core.App.Configuration;
    using Naos.Core.App.Operations.Serilog;
    using Naos.Core.Common;
    using Naos.Core.Scheduling;
    using Naos.Core.Scheduling.Domain;
    using Shouldly;
    using SimpleInjector;
    using Xunit;

    public class JobSchedulerTests : BaseTest
    {
        private readonly Container container = new Container();

        public JobSchedulerTests()
        {
            var configuration = NaosConfigurationFactory.CreateRoot();
            this.container = new Container()
                .AddNaosLogging(configuration)
                .AddNaosScheduling(configuration);
        }

        [Fact]
        public void VerifyContainer_Test()
        {
            this.container.Verify();
        }

        [Fact]
        public void CanInstantiate_Test()
        {
            var sut = this.container.GetInstance<IJobScheduler>();

            sut.ShouldNotBeNull();
        }

        [Fact]
        public async Task RegisterAndTriggerAction_Test()
        {
            var sut = this.container.GetInstance<IJobScheduler>();
            var count = 0;

            sut.Register("key1", "* 12 * * * *", (a) =>
            {
                count++;
                System.Diagnostics.Trace.WriteLine("+++ hello from task " + a);
            });

            var t1 = sut.TriggerAsync("key1");
            var t2 = sut.TriggerAsync("key1");

            await Task.WhenAll(new[] { t1, t2 });

            count.ShouldBe(2); // action does not support overlap (due to async not supported?)
        }

        [Fact]
        public async Task RegisterAndTriggerTask_Test()
        {
            var sut = this.container.GetInstance<IJobScheduler>();
            var count = 0;

            sut.Register("key1", "* 12 * * * *", async (a) =>
                await Task.Run(() =>
                {
                    count++;
                    System.Diagnostics.Trace.WriteLine("+++ hello from task " + a);
                }));

            var t1 = sut.TriggerAsync("key1");
            var t2 = sut.TriggerAsync("key1");

            await Task.WhenAll(new[] { t1, t2 });

            count.ShouldBe(1); // due to overlap (key1) the job runs once
        }

        [Fact]
        public async Task RegisterAndTriggerTypeWithArgs_Test()
        {
            this.container.RegisterInstance(new StubProbe());
            var probe = this.container.GetInstance<StubProbe>();
            var sut = this.container.GetInstance<IJobScheduler>();

            sut.Register<StubJob>("key1", "* 12 * * * *", new[] { "once" });
            sut.Register<StubJob>("key2", "* 12 * * * *", new[] { "once" });

            // at trigger time the StubScheduledTask (with probe in ctor) is resolved from container and executed
            var t1 = sut.TriggerAsync("key1");
            var t2 = sut.TriggerAsync("key2");

            await Task.WhenAll(new[] { t1, t2 });

            probe.Count.ShouldBe(2); // each job (key1/key2) runs once (2 * 1)
        }

        [Fact]
        public async Task RegisterAndTriggerTypeWithOverlapAndArgs_Test()
        {
            this.container.RegisterInstance(new StubProbe());
            var probe = this.container.GetInstance<StubProbe>();
            var sut = this.container.GetInstance<IJobScheduler>();

            sut.Register<StubJob>("key1", "* 12 * * * *", new[] { "once"});

            // at trigger time the StubScheduledTask (with probe in ctor) is resolved from container and executed
            var t1 = sut.TriggerAsync("key1");
            var t2 = sut.TriggerAsync("key1");

            await Task.WhenAll(new[] { t1, t2 });

            probe.Count.ShouldBe(1); // due to overlap (key1) the job runs once
        }

        [Fact]
        public async Task RegisterAndTriggerTypeWithArgsAndCancellation_Test()
        {
            this.container.RegisterInstance(new StubProbe());
            var probe = this.container.GetInstance<StubProbe>();
            var sut = this.container.GetInstance<IJobScheduler>();

            sut.Register<StubJob>("key1", "* 12 * * * *", new[] { "once" });
            sut.Register<StubJob>("key2", "* 12 * * * *", new[] { "cancel" });

            // at trigger time the StubScheduledTask (with probe in ctor) is resolved from container and executed
            var t1 = sut.TriggerAsync("key1");
            var t2 = sut.TriggerAsync("key2");

            await Task.WhenAll(new[] { t1, t2 });

            probe.Count.ShouldBe(1); // jirst job trigger runs once, second job cancels directly
        }

        [Fact]
        public async Task RegisterAndTriggerTypeWithCancellation_Test()
        {
            this.container.RegisterInstance(new StubProbe());
            var probe = this.container.GetInstance<StubProbe>();
            var cts = new CancellationTokenSource();
            var sut = this.container.GetInstance<IJobScheduler>();

            sut.Register<StubJob>("key1", "* 12 * * * *");
            sut.Register<StubJob>("key2", "* 12 * * * *");

            // at trigger time the StubScheduledTask (with probe in ctor) is resolved from container and executed
            cts.CancelAfter(TimeSpan.FromMilliseconds(250));
            var t1 = sut.TriggerAsync("key1", cts.Token);
            var t2 = sut.TriggerAsync("key2", cts.Token);
            //var t3 = sut.TriggerAsync("key3", cts.Token);

            await Task.WhenAll(new[] { t1, t2 });

            probe.Count.ShouldBe(4); // every job loops twice
            //t1.IsCanceled.ShouldBe(true);
            //t2.IsCanceled.ShouldBe(true);
        }

        [Fact]
        public async Task RegisterAndTriggerTypeOverlap_Test()
        {
            this.container.RegisterInstance(new StubProbe());
            var probe = this.container.GetInstance<StubProbe>();
            var sut = this.container.GetInstance<IJobScheduler>();

            sut.Register<StubJob>("key1", "* 12 * * * *");

            // at trigger time the StubScheduledTask (with probe in ctor) is resolved from container and executed
            var t1 = sut.TriggerAsync("key1");
            var t2 = sut.TriggerAsync("key1"); // skipped, due to overlap
            var t3 = sut.TriggerAsync("unk");

            await Task.WhenAll(new[] { t1, t2, t3 });

            probe.Count.ShouldBe(5);
        }

        [Fact]
        public async Task RegisterAndTriggerCustomType_Test()
        {
            this.container.RegisterInstance(new StubProbe());
            var probe = this.container.GetInstance<StubProbe>();
            var sut = this.container.GetInstance<IJobScheduler>();

            sut.Register<StubJob>("key1", "* 12 * * * *", (t) => t.CustomExecuteAsync("arg1", probe));
            sut.Register<StubJob>("key2", "* 12 * * * *", (t) => t.CustomExecuteAsync("arg2", probe));

            // at trigger time the StubScheduledTask (with probe in ctor) is resolved from container and executed
            var t1 = sut.TriggerAsync("key1");
            var t2 = sut.TriggerAsync("key2");

            await Task.WhenAll(new[] { t1, t2 });

            probe.Count.ShouldBe(4); // probe.count gets increased per job
        }

        private class StubJob : Job
        {
            private readonly StubProbe probe;

            public StubJob(StubProbe probe)
            {
                this.probe = probe;
            }

            public override async Task ExecuteAsync(CancellationToken token, string[] args = null)
            {
                await Task.Run(() =>
                {
                    var max = args.Contains("once", StringComparison.OrdinalIgnoreCase) ? 1 : 5;
                    var cancel = args.Contains("cancel", StringComparison.OrdinalIgnoreCase);

                    for (int i = 0; i < max; i++) // fake some long running process, can be cancelled with token
                    {
                        token.ThrowIfCancellationRequested();
                        if (cancel)
                        {
                            throw new OperationCanceledException("oops");
                        }

                        this.probe.Count++;
                        System.Diagnostics.Trace.WriteLine($"+++ hello from job {DateTime.UtcNow.ToString("o")}");

                        Thread.Sleep(200);
                    }
                }, token);
            }

            public async Task CustomExecuteAsync(string arg1, StubProbe probe)
            {
                await Task.Run(() =>
                {
                    this.probe.Count++;
                    probe.Count++;
                    System.Diagnostics.Trace.WriteLine($"+++ hello from custom job {DateTime.UtcNow.ToString("o")} " + arg1);
                });
            }
        }

        private class StubProbe
        {
            public int Count { get; set; }
        }
    }
}