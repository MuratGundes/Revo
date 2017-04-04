﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using GTRevo.Platform.Core;
using GTRevo.Platform.Core.Lifecycle;

namespace GTRevo.Infrastructure.Domain
{
    public class ConventionEventApplyRegistratorCache : IApplicationStartListener
    {
        private static Dictionary<Type, EventTypeApplyDelegates> componentTypeDelegates;

        private static ITypeExplorer typeExplorer;

        public ConventionEventApplyRegistratorCache(ITypeExplorer typeExplorer)
        {
            ConventionEventApplyRegistratorCache.typeExplorer = typeExplorer;
        }

        public static EventTypeApplyDelegates GetApplyDelegates(Type componentType)
        {
            if (componentTypeDelegates == null)
            {
                CreateAggregateEventDelegates();
            }

            if (!typeof(IComponent).IsAssignableFrom(componentType))
            {
                throw new ArgumentException(
                    $"Only IComponent-derived types have their convention-based event Apply methods indexed (passed type is '{componentType.FullName}')");
            }

            EventTypeApplyDelegates delegates;
            if (!componentTypeDelegates.TryGetValue(componentType, out delegates))
            {
                throw new ArgumentException($"Unknown component type to get apply delegates for: " + componentType.FullName);
            }

            return delegates;
        }

        public void OnApplicationStarted()
        {
            CreateAggregateEventDelegates();
        }

        public static void CreateAggregateEventDelegates()
        {
            if (typeExplorer == null)
            {
                typeExplorer = new TypeExplorer();
            }

            componentTypeDelegates = new Dictionary<Type, EventTypeApplyDelegates>();

            var componentTypes = typeExplorer.GetAllTypes()
                .Where(x => typeof(IComponent).IsAssignableFrom(x)
                    && !x.IsAbstract && !x.IsGenericTypeDefinition);

            foreach (Type aggregateType in componentTypes)
            {
                EventTypeApplyDelegates aggApplyDelegates = new EventTypeApplyDelegates();
                componentTypeDelegates.Add(aggregateType, aggApplyDelegates);
                AddTypeDelegates(aggregateType, aggApplyDelegates);
            }
        }

        private static void AddTypeDelegates(Type componentType, EventTypeApplyDelegates applyDelegates)
        {
            if (componentType.BaseType != null
                && typeof(IComponent).IsAssignableFrom(componentType.BaseType))
            {
                AddTypeDelegates(componentType.BaseType, applyDelegates);
            }

            var applyMethods = componentType
                    .GetMethods(BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic)
                    .Where(x => x.Name == "Apply"
                        && x.GetParameters().Length == 1
                        && typeof(DomainAggregateEvent).IsAssignableFrom(x.GetParameters()[0].ParameterType));

            foreach (MethodInfo applyMethod in applyMethods)
            {
                Type eventType = applyMethod.GetParameters()[0].ParameterType;

                // Create an Action<AggregateRoot, IEvent> that does (agg, evt) => ((ConcreteAggregate)agg).Apply((ConcreteEvent)evt)
                var evtParam = Expression.Parameter(typeof(DomainAggregateEvent), "evt");
                var aggParam = Expression.Parameter(typeof(IComponent), "agg");

                var eventDelegate = Expression.Lambda(
                        Expression.Call(
                            Expression.Convert(
                                aggParam,
                                componentType),
                            applyMethod,
                            Expression.Convert(
                                evtParam,
                                eventType)),
                        aggParam,
                        evtParam).Compile();

                applyDelegates[eventType] = (Action<IComponent, DomainAggregateEvent>) eventDelegate;
            }
        }

        public class EventTypeApplyDelegates : Dictionary<Type, Action<IComponent, DomainAggregateEvent>>
        { 
        }
    }
}