﻿using System.IO;
using Revo.Core.IO;
using Revo.Core.IO.Resources;
using Revo.Infrastructure.Globalization.Messages;

namespace Revo.Infrastructure.Globalization
{
    public class YamlLocaleMessageSourceFactory : ILocaleMessageSourceFactory
    {
        private readonly IResourceManager resourceManager;

        public YamlLocaleMessageSourceFactory(string messageResourcePath, string localeCode, int priority,
            IResourceManager resourceManager)
        {
            LocaleCode = localeCode;
            this.resourceManager = resourceManager;
            Priority = priority;
            Load(messageResourcePath);
        }

        public string LocaleCode { get; }
        public int Priority { get; }
        public IMessageSource MessageSource { get; private set; }

        private void Load(string messageResourcePath)
        {
            using (Stream messageStream = resourceManager.CreateReadStream(messageResourcePath))
            {
                YamlMessageSource messageSource = new YamlMessageSource(messageStream);
                MessageSource = messageSource;
            }
        }
    }
}
