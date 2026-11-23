namespace GroveGames.StateMachine;

public interface IState<in TContext>
{
    public void Enter(TContext context);
    public void Tick(TContext context, float deltaTime);
    public void Exit(TContext context);
}
