// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines
{
    internal sealed class InlineScheduler : PipeScheduler
    {
        public override void Schedule(Action<object?> action, object? state)
        {
            action(state);
        }

        internal override void UnsafeSchedule(Action<object?> action, object? state)
        {
            action(state);
        }
    }
}
