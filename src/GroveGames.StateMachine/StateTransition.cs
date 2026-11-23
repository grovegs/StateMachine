using System;

namespace GroveGames.StateMachine;

public readonly struct StateTransition<TContext>
{
    private readonly IState<TContext>? _from;
    private readonly IState<TContext> _to;
    private readonly Func<TContext, bool> _condition;
    public IState<TContext>? From => _from;
    public IState<TContext> To => _to;
    public Func<TContext, bool> Condition => _condition;

    public StateTransition(IState<TContext>? from, IState<TContext> to, Func<TContext, bool> condition)
    {
        _from = from;
        _to = to;
        _condition = condition;
    }
}
