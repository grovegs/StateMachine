using System;
using System.Collections.Generic;

namespace GroveGames.StateMachine;

public sealed class StateMachine<TContext> : IStateMachine<TContext>
{
    private const int InitialCapacity = 8;
    private const int MaxTransitionsPerTick = 8;

    private readonly List<StateTransition<TContext>> _anyTransitions;
    private readonly List<StateTransition<TContext>> _transitions;
    private IState<TContext>? _current;
    private readonly TContext _context;

    public event Action<StateChangedEvent<TContext>>? StateChanged;

    public TContext Context => _context;
    public IState<TContext>? Current => _current;
    public bool IsRunning => _current != null;

    public StateMachine(TContext context)
    {
        _context = context;
        _anyTransitions = new List<StateTransition<TContext>>(InitialCapacity);
        _transitions = new List<StateTransition<TContext>>(InitialCapacity);
    }

    public void AddTransition(IState<TContext> from, IState<TContext> to, Func<TContext, bool> condition)
    {
        _transitions.Add(new StateTransition<TContext>(from, to, condition));
    }

    public void AddAnyTransition(IState<TContext> to, Func<TContext, bool> condition)
    {
        _anyTransitions.Add(new StateTransition<TContext>(null, to, condition));
    }

    public void Start(IState<TContext> initial)
    {
        ChangeState(initial);
    }

    public void Tick(float deltaTime)
    {
        if (_current == null)
        {
            return;
        }

        for (var i = 0; i < MaxTransitionsPerTick && TryFindTransition(out var next); i++)
        {
            ChangeState(next);
        }

        _current?.Tick(Context, deltaTime);
    }

    public void ChangeState(IState<TContext> state)
    {
        var previous = _current;
        previous?.Exit(Context);
        _current = state;
        state.Enter(Context);
        StateChanged?.Invoke(new StateChangedEvent<TContext>(previous, state));
    }

    public void Stop()
    {
        _current?.Exit(Context);
        _current = null;
    }

    private bool TryFindTransition(out IState<TContext> next)
    {
        if (TryFindTransition(_anyTransitions, out next))
        {
            return true;
        }

        return TryFindTransition(_transitions, out next);
    }

    private bool TryFindTransition(List<StateTransition<TContext>> transitions, out IState<TContext> next)
    {
        for (var i = 0; i < transitions.Count; i++)
        {
            var transition = transitions[i];

            if (transition.To == _current || (transition.From != null && transition.From != _current))
            {
                continue;
            }

            if (transition.Condition(Context))
            {
                next = transition.To;
                return true;
            }
        }

        next = null!;
        return false;
    }
}
