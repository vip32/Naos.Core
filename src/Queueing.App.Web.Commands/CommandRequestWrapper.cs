﻿namespace Naos.Core.Queueing.App.Web
{
    using Naos.Core.Commands.App;

    public class CommandRequestWrapper
    {
        public object Command { get; set; }

        public CommandRequestWrapper SetCommand<TCommand, TResponse>(TCommand command)
            where TCommand : Command<TResponse>
        {
            this.Command = command;
            return this;
        }

        public CommandRequestWrapper SetCommand<TCommand>(TCommand command)
            where TCommand : Command<object>
        {
            this.Command = command;
            return this;
        }
    }
}