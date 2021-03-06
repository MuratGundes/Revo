﻿using System;
using System.Collections.Generic;
using System.Linq;
using Revo.Core.Core;
using Revo.Core.Events;
using Revo.Domain.Entities;
using Revo.Domain.Events;
using Revo.Infrastructure.Events.Async;
using Revo.Infrastructure.Projections;

namespace Revo.RavenDB.Projections
{
    public class RavenProjectionEventListener : ProjectionEventListener

    {
        private readonly IServiceLocator serviceLocator;

        public RavenProjectionEventListener(IEntityTypeManager entityTypeManager,
            IServiceLocator serviceLocator, RavenProjectionEventSequencer eventSequencer) :
            base(entityTypeManager)
        {
            this.serviceLocator = serviceLocator;
            EventSequencer = eventSequencer;
        }
        
        public override IAsyncEventSequencer EventSequencer { get; }

        public override IEnumerable<IEntityEventProjector> GetProjectors(Type entityType)
        {
            return serviceLocator.GetAll(
                    typeof(IRavenEntityEventProjector<>).MakeGenericType(entityType))
                .Cast<IEntityEventProjector>();
        }

        public class RavenProjectionEventSequencer : AsyncEventSequencer<DomainAggregateEvent>
        {
            public readonly string QueueNamePrefix = "RavenProjectionEventSequencer:";

            protected override IEnumerable<EventSequencing> GetEventSequencing(IEventMessage<DomainAggregateEvent> message)
            {
                yield return new EventSequencing() { SequenceName = QueueNamePrefix + message.Event.AggregateId.ToString(),
                    EventSequenceNumber = message.Metadata.GetStreamSequenceNumber() };
            }
            
            protected override bool ShouldAttemptSynchronousDispatch(IEventMessage<DomainAggregateEvent> message)
            {
                return true;
            }
        }
    }
}
