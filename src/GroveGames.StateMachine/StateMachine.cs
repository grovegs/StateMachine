using System;
using System.Collections.Generic;

namespace GroveGames.StateMachine;

public sealed class StateMachine<TContext> : IStateMachine<TContext>
{
    private const int InitialCapacity = 8;
    private const int MaxTransitionsPerTick = 8;
    private const int MaxChainedChanges = 64;

    private readonly List<StateTransition<TContext>> _anyTransitions;
    private readonly List<StateTransition<TContext>> _transitions;
    private IState<TContext>? _current;
    private IState<TContext>? _pending;
    private bool _isChanging;
    private bool _isStopPending;
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
        _pending = null;
        _isChanging = false;
        _isStopPending = false;
    }

    public void AddTransition(IState<TContext> from, IState<TContext> to, Func<TContext, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(condition);

        _transitions.Add(new StateTransition<TContext>(from, to, condition));
    }

    public void AddAnyTransition(IState<TContext> to, Func<TContext, bool> condition)
    {
        ArgumentNullException.ThrowIfNull(to);
        ArgumentNullException.ThrowIfNull(condition);

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

        for (var i = 0; i < MaxTransitionsPerTick && _current != null && TryFindTransition(out var next); i++)
        {
            ChangeState(next);
        }

        _current?.Tick(Context, deltaTime);
    }

    public void ChangeState(IState<TContext> state)
    {
        ArgumentNullException.ThrowIfNull(state);

        SetPendingChange(state, false);
    }

    public void Stop()
    {
        SetPendingChange(null, true);
    }

    private void SetPendingChange(IState<TContext>? state, bool isStop)
    {
        _pending = state;
        _isStopPending = isStop;

        if (_isChanging)
        {
            return;
        }

        ApplyPendingChange();
    }

    private void ApplyPendingChange()
    {
        _isChanging = true;

        try
        {
            for (var i = 0; _pending != null || _isStopPending; i++)
            {
                if (i > MaxChainedChanges)
                {
                    throw new InvalidOperationException($"States changed more than {MaxChainedChanges} times in a row from Enter, Exit or StateChanged.");
                }

                var state = _pending;
                var isStop = _isStopPending;
                _pending = null;
                _isStopPending = false;

                if (isStop)
                {
                    _current?.Exit(Context);
                    _current = null;
                    continue;
                }

                var previous = _current;
                previous?.Exit(Context);
                _current = state;
                state!.Enter(Context);
                StateChanged?.Invoke(new StateChangedEvent<TContext>(previous, state));
            }
        }
        finally
        {
            _isChanging = false;
            _pending = null;
            _isStopPending = false;
        }
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
