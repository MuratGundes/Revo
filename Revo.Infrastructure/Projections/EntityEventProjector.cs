﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Revo.Core.Collections;
using Revo.Core.Events;
using Revo.DataAccess.Entities;
using Revo.Domain.Entities;
using Revo.Domain.Entities.EventSourcing;
using Revo.Domain.Events;

namespace Revo.Infrastructure.Projections
{
    /// <summary>
    /// An event projector for an aggregate type with arbitrary read-model(s).
    /// A convention-based abstract base class that calls an Apply for every event type
    /// and also supports sub-projectors.
    /// </summary>
    /// <typeparam name="TSource">Aggregate type.</typeparam>
    public abstract class EntityEventProjector<TSource> :
        IEntityEventProjector<TSource>
        where TSource : class, IAggregateRoot
    {
        private readonly Lazy<MultiValueDictionary<Type, Func<IEventMessage<DomainAggregateEvent>, Task>>> applyHandlers;
        private readonly List<ISubEntityEventProjector> subProjectors = new List<ISubEntityEventProjector>();

        public EntityEventProjector()
        {
            applyHandlers = new Lazy<MultiValueDictionary<Type, Func<IEventMessage<DomainAggregateEvent>, Task>>>(CreateApplyDelegates);
        }

        public Type ProjectedAggregateType => typeof(TSource);

        protected Guid AggregateId { get; private set; }
        protected IReadOnlyCollection<IEventMessage<DomainAggregateEvent>> Events { get; private set; }

        public abstract Task CommitChangesAsync();

        public virtual async Task ProjectEventsAsync(Guid aggregateId, IReadOnlyCollection<IEventMessage<DomainAggregateEvent>> events)
        {
            if (events.Count == 0)
            {
                throw new InvalidOperationException($"No events to project for aggregate with ID {aggregateId}");
            }
            
            AggregateId = aggregateId;
            Events = events;

            try
            {
                await ApplyEvents(events);
            }
            finally
            {
                AggregateId = default(Guid);
                Events = null;
            }
        }

        protected void AddSubProjector(ISubEntityEventProjector projector)
        {
            subProjectors.Add(projector);

            if (applyHandlers.IsValueCreated)
            {
                CreateApplyDelegates(projector.GetType(), projector, applyHandlers.Value);
            }
        }

        protected async Task ExecuteHandler<T>(T evt) where T : IEventMessage<DomainAggregateEvent>
        {
            IReadOnlyCollection<Func<IEventMessage<DomainAggregateEvent>, Task>> handlers;
            if (applyHandlers.Value.TryGetValue(evt.Event.GetType(), out handlers))
            {
                foreach (var handler in handlers)
                {
                    await handler(evt);
                }
            }
        }

        protected virtual async Task ApplyEvents(IEnumerable<IEventMessage<DomainAggregateEvent>> events)
        {
            foreach (IEventMessage<DomainAggregateEvent> ev in events)
            {
                await ExecuteHandler(ev);
            }
        }

        protected virtual IEnumerable<(Type EventType, Func<IEventMessage<DomainAggregateEvent>, Task> Delegate)> GetApplyDelegates(
            Type projectorType, object instance)
        {
            var actions = projectorType
                .GetMethods(BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public |
                            BindingFlags.NonPublic)
                .Where(x => x.Name == "Apply"
                            && x.GetBaseDefinition() == x //exclude overrides
                            && x.GetParameters().Length == 1
                            && typeof(IEventMessage<DomainAggregateEvent>).IsAssignableFrom(x.GetParameters()[0]
                                .ParameterType))
                .Select(x =>
                    (
                        EventType: x.GetParameters()[0].ParameterType.GetGenericArguments()[0],
                        Delegate: (Func<IEventMessage<DomainAggregateEvent>, Task>) (ev =>
                        {
                            Task ret = x.Invoke(instance, new object[] { ev }) as Task;
                            if (ret != null)
                            {
                                return ret;
                            }

                            return Task.FromResult(0);
                        })
                    ));

            actions = actions.Concat(
                projectorType
                    .GetMethods(BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public |
                                BindingFlags.NonPublic)
                    .Where(x => x.Name == "Apply"
                                && x.GetBaseDefinition() == x //exclude overrides
                                && x.GetParameters().Length == 2
                                && typeof(IEventMessage<DomainAggregateEvent>).IsAssignableFrom(x.GetParameters()[0]
                                    .ParameterType)
                                && x.GetParameters()[1].ParameterType.IsAssignableFrom(typeof(Guid)))
                    .Select(x =>
                    (
                        EventType: x.GetParameters()[0].ParameterType.GetGenericArguments()[0],
                        Delegate: (Func<IEventMessage<DomainAggregateEvent>, Task>)(ev =>
                        {
                            Task ret = x.Invoke(instance, new object[] { ev, this.AggregateId }) as Task;
                            if (ret != null)
                            {
                                return ret;
                            }

                            return Task.FromResult(0);
                        })
                    ))
                );

            return actions;
        }

        private MultiValueDictionary<Type, Func<IEventMessage<DomainAggregateEvent>, Task>> CreateApplyDelegates()
        {
            var result = new MultiValueDictionary<Type, Func<IEventMessage<DomainAggregateEvent>, Task>>();
            CreateApplyDelegates(GetType(), this, result);

            foreach (var subProjector in subProjectors)
            {
                CreateApplyDelegates(subProjector.GetType(), subProjector, result);
            }

            return result;
        }

        private void CreateApplyDelegates(Type projectorType, object instance, MultiValueDictionary<Type, Func<IEventMessage<DomainAggregateEvent>, Task>> result)
        {
            if (projectorType.BaseType != null)
            {
                CreateApplyDelegates(projectorType.BaseType, instance, result);
            }

            var actions = GetApplyDelegates(projectorType, instance);

            foreach (var action in actions)
            {
                result.Add(action.Item1, action.Item2);
            }
        }
    }
}
