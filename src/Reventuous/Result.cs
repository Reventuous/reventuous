using System.Collections.Generic;

namespace Reventuous {
    public abstract record Result<T, TState, TId>(TState State, IEnumerable<object>? Changes = null)
        where T : Aggregate<TState, TId>
        where TState : AggregateState<TState, TId>, new()
        where TId : AggregateId;

    public record OkResult<T, TState, TId>(TState State, IEnumerable<object> Changes, string StreamPosition)
        : Result<T, TState, TId>(State, Changes)
        where T : Aggregate<TState, TId>
        where TState : AggregateState<TState, TId>, new()
        where TId : AggregateId;

    public record ErrorResult<T, TState, TId>() : Result<T, TState, TId>(new TState())
        where T : Aggregate<TState, TId>
        where TState : AggregateState<TState, TId>, new()
        where TId : AggregateId;
}