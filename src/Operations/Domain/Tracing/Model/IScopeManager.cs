﻿namespace Naos.Core.Operations.Domain
{
    using System.Threading.Tasks;

    public interface IScopeManager
    {
        IScope Active { get; }

        IScope Activate(ISpan span, bool finishOnDispose = true);

        Task Finish(ISpan span);
    }
}