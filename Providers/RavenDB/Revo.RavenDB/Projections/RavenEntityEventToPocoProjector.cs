﻿using Revo.Domain.Entities.EventSourcing;
using Revo.Infrastructure.Projections;
using Revo.RavenDB.Entities;

namespace Revo.RavenDB.Projections
{
    public class RavenEntityEventToPocoProjector<TSource, TTarget> :
        CrudEntityEventToPocoProjector<TSource, TTarget>,
        IRavenEntityEventProjector<TSource>
        where TSource : class, IEventSourcedAggregateRoot
        where TTarget : class, new()
    {
        public RavenEntityEventToPocoProjector(IRavenCrudRepository repository) : base(repository)
        {
            Repository = repository;
        }

        protected new IRavenCrudRepository Repository { get; }
    }
}
