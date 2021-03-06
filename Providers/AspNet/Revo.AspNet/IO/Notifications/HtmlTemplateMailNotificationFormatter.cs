﻿using System.Collections.Generic;
using System.Threading.Tasks;
using RazorEngine.Templating;
using Revo.AspNet.IO.Templates;
using Revo.Extensions.Notifications;
using Revo.Extensions.Notifications.Channels.Mail;

namespace Revo.AspNet.IO.Notifications
{
    public abstract class HtmlTemplateMailNotificationFormatter : IMailNotificationFormatter
    {
        private readonly RazorEngineTemplates razorEngineTemplates;

        protected HtmlTemplateMailNotificationFormatter(RazorEngineTemplates razorEngineTemplates)
        {
            this.razorEngineTemplates = razorEngineTemplates;
        }

        public async Task<IEnumerable<SerializableMailMessage>> FormatNotificationMessage(IEnumerable<INotification> notifications)
        {
            List<SerializableMailMessage> messages = new List<SerializableMailMessage>();

            IEnumerable<MessageTemplate> messageTemplates = await GetMessageTemplate(notifications);
            if (messageTemplates != null)
            {
                foreach (MessageTemplate messageTemplate in messageTemplates)
                {
                    messages.Add(RenderNotificationMessage(messageTemplate));
                }
            }

            return messages;
        }

        protected abstract Task<IEnumerable<MessageTemplate>> GetMessageTemplate(IEnumerable<INotification> notifications);

        protected virtual SerializableMailMessage RenderNotificationMessage(MessageTemplate messageTemplate)
        {
            string text = razorEngineTemplates.EngineService.RunCompile(messageTemplate.TemplatePath,
                null, messageTemplate.Model);
            messageTemplate.Message.Body = text;
            messageTemplate.Message.IsBodyHtml = true;

            return messageTemplate.Message;
        }

        protected class MessageTemplate
        {
            public string TemplatePath { get; set; }
            public object Model { get; set; }
            public SerializableMailMessage Message { get; set; }
        }
    }
}
