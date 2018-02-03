﻿using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using Revo.Domain.Entities;
using Revo.Domain.Events;
using Xunit;

namespace Revo.Infrastructure.Tests.Core.Domain
{
    public class AggregateEventRouterTests
    {
        private readonly IAggregateRoot aggregate;
        private readonly AggregateEventRouter sut;

        public AggregateEventRouterTests()
        {
            aggregate = Substitute.For<IAggregateRoot>();
            aggregate.Id.Returns(Guid.NewGuid());

            sut = new AggregateEventRouter(aggregate);
        }

        [Fact]
        public void ApplyEvent_PushesEventToUncomittedEvents()
        {
            var ev1 = new Event1();
            var ev2 = new Event2();

            sut.ApplyEvent(ev1);
            sut.ApplyEvent(ev2);

            Assert.Equal(2, sut.UncommitedEvents.Count());
            Assert.Equal(ev1, sut.UncommitedEvents.ElementAt(0));
            Assert.Equal(ev2, sut.UncommitedEvents.ElementAt(1));
        }

        [Fact]
        public void ApplyEvent_SetsAggregateId()
        {
            sut.ApplyEvent(new Event1());
            
            Assert.Equal(aggregate.Id, sut.UncommitedEvents.ElementAt(0).AggregateId);
        }
        
        [Fact]
        public void CommitEvents_ClearsUncomittedEvents()
        {
            sut.ApplyEvent(new Event1());
            sut.CommitEvents();

            Assert.Equal(0, sut.UncommitedEvents.Count());
        }

        [Fact]
        public void Apply_FiresAllHandlers()
        {
            List<DomainAggregateEvent> events1 = new List<DomainAggregateEvent>();
            List<DomainAggregateEvent> events2 = new List<DomainAggregateEvent>();

            var ev1 = new Event1();

            sut.Register<Event1>(ev => events1.Add(ev));
            sut.Register<Event1>(ev => events2.Add(ev));
            sut.ApplyEvent(ev1);

            Assert.Equal(events1[0], ev1);
            Assert.Equal(events2[0], ev1);
        }

        [Fact]
        public void ReplayEvents_ExecutesHandlers()
        {
            List<DomainAggregateEvent> events = new List<DomainAggregateEvent>();

            var ev1 = new Event1();

            sut.Register<Event1>(ev => events.Add(ev));
            sut.ApplyEvent(ev1);

            Assert.Equal(events[0], ev1);
        }

        [Fact]
        public void ReplayEvents_DoesntAddUncomittedEvents()
        {
            sut.ReplayEvents(new []{ new Event1() });
            Assert.Equal(0, sut.UncommitedEvents.Count());
        }

        public class Event1 : DomainAggregateEvent
        {
        }

        public class Event2 : DomainAggregateEvent
        {
        }
    }
}