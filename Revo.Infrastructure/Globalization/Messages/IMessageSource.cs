﻿using System.Collections.Immutable;

namespace Revo.Infrastructure.Globalization.Messages
{
    public interface IMessageSource
    {
        ImmutableDictionary<string, string> Messages { get; }
        bool TryGetMessage(string key, out string message);
    }
}
