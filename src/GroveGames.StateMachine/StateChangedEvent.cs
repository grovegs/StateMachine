namespace GroveGames.StateMachine;

public readonly struct StateChangedEvent<TContext>
{
    private readonly IState<TContext>? _previous;
    private readonly IState<TContext> _current;
    public IState<TContext>? Previous => _previous;
    public IState<TContext> Current => _current;

    public StateChangedEvent(IState<TContext>? previous, IState<TContext> current)
    {
        _previous = previous;
        _current = current;
    }
}
