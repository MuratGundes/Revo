﻿namespace Revo.Infrastructure.Globalization.Messages.Database
{
    public class DbLocaleMessageSourceFactory : ILocaleMessageSourceFactory
    {
        public DbLocaleMessageSourceFactory(string localeCode, int priority, IDbMessageCache dbMessageCache,
            IDbMessageLoader dbMessageLoader)
        {
            LocaleCode = localeCode;
            Priority = priority;
            MessageSource = new DbMessageSource(localeCode, dbMessageCache, dbMessageLoader);
        }

        public string LocaleCode { get; }
        public int Priority { get; }
        public IMessageSource MessageSource { get; }
    }
}
