using System;

namespace GroveGames.StateMachine;

public interface IStateMachine<TContext>
{
    public event Action<StateChangedEvent<TContext>>? StateChanged;

    public TContext Context { get; }
    public IState<TContext>? Current { get; }
    public bool IsRunning { get; }

    public void AddTransition(IState<TContext> from, IState<TContext> to, Func<TContext, bool> condition);
    public void AddAnyTransition(IState<TContext> to, Func<TContext, bool> condition);
    public void Start(IState<TContext> initial);
    public void Tick(float deltaTime);
    public void ChangeState(IState<TContext> state);
    public void Stop();
}
