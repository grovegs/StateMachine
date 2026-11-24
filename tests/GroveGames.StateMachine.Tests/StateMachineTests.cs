using System.Collections.Generic;

namespace GroveGames.StateMachine.Tests;

public sealed class StateMachineTests
{
    private readonly TestContext _context;
    private readonly StateMachine<TestContext> _machine;
    private readonly FakeState _idle;
    private readonly FakeState _walking;
    private readonly FakeState _running;

    public StateMachineTests()
    {
        _context = new TestContext();
        _machine = new StateMachine<TestContext>(_context);
        _idle = new FakeState("Idle");
        _walking = new FakeState("Walking");
        _running = new FakeState("Running");
    }

    [Fact]
    public void Start_InitialState_EntersIt()
    {
        _machine.Start(_idle);

        Assert.Same(_idle, _machine.Current);
        Assert.True(_machine.IsRunning);
        Assert.Equal(new[] { "Idle.Enter" }, _context.Calls);
    }

    [Fact]
    public void Tick_ConditionMet_ExitsEntersAndTicksTheNextState()
    {
        _machine.AddTransition(_idle, _walking, context => context.Value > 0);
        _machine.Start(_idle);
        _context.Calls.Clear();
        _context.SetValue(1);

        _machine.Tick(0.1f);

        Assert.Same(_walking, _machine.Current);
        Assert.Equal(new[] { "Idle.Exit", "Walking.Enter", "Walking.Tick" }, _context.Calls);
    }

    [Fact]
    public void Tick_ConditionNotMet_TicksTheCurrentState()
    {
        _machine.AddTransition(_idle, _walking, context => context.Value > 0);
        _machine.Start(_idle);
        _context.Calls.Clear();

        _machine.Tick(0.1f);

        Assert.Same(_idle, _machine.Current);
        Assert.Equal(new[] { "Idle.Tick" }, _context.Calls);
    }

    [Fact]
    public void Tick_ChainedTransitions_SettlesWithinOneTick()
    {
        _machine.AddTransition(_idle, _walking, context => context.Value > 0);
        _machine.AddTransition(_walking, _running, context => context.Value > 1);
        _machine.Start(_idle);
        _context.SetValue(2);

        _machine.Tick(0.1f);

        Assert.Same(_running, _machine.Current);
    }

    [Fact]
    public void Tick_AnyTransition_TakesPriorityOverStateTransitions()
    {
        _machine.AddTransition(_idle, _walking, context => context.Value > 0);
        _machine.AddAnyTransition(_running, context => context.Value > 0);
        _machine.Start(_idle);
        _context.SetValue(1);

        _machine.Tick(0.1f);

        Assert.Same(_running, _machine.Current);
    }

    [Fact]
    public void Tick_AnyTransitionToTheCurrentState_DoesNotReenterIt()
    {
        _machine.AddAnyTransition(_idle, context => true);
        _machine.Start(_idle);
        _context.Calls.Clear();

        _machine.Tick(0.1f);

        Assert.Equal(new[] { "Idle.Tick" }, _context.Calls);
    }

    [Fact]
    public void Tick_TransitionFromAnotherState_IsIgnored()
    {
        _machine.AddTransition(_walking, _running, context => true);
        _machine.Start(_idle);

        _machine.Tick(0.1f);

        Assert.Same(_idle, _machine.Current);
    }

    [Fact]
    public void Tick_CyclingConditions_StopsAfterTheTransitionLimit()
    {
        _machine.AddTransition(_idle, _walking, context => true);
        _machine.AddTransition(_walking, _idle, context => true);
        _machine.Start(_idle);

        _machine.Tick(0.1f);

        Assert.True(_machine.IsRunning);
    }

    [Fact]
    public void ChangeState_RaisesStateChangedWithPreviousAndCurrent()
    {
        var changes = new List<StateChangedEvent<TestContext>>();
        _machine.StateChanged += changes.Add;
        _machine.Start(_idle);

        _machine.ChangeState(_walking);

        Assert.Equal(2, changes.Count);
        Assert.Same(_idle, changes[1].Previous);
        Assert.Same(_walking, changes[1].Current);
    }

    [Fact]
    public void Stop_RunningMachine_ExitsAndClearsTheState()
    {
        _machine.Start(_idle);
        _context.Calls.Clear();

        _machine.Stop();

        Assert.Null(_machine.Current);
        Assert.False(_machine.IsRunning);
        Assert.Equal(new[] { "Idle.Exit" }, _context.Calls);
    }

    [Fact]
    public void Tick_NotStarted_DoesNothing()
    {
        _machine.AddAnyTransition(_walking, context => true);

        _machine.Tick(0.1f);

        Assert.Null(_machine.Current);
        Assert.Empty(_context.Calls);
    }

    private sealed class TestContext
    {
        private readonly List<string> _calls = new();
        private int _value;

        public List<string> Calls => _calls;
        public int Value => _value;

        public void SetValue(int value)
        {
            _value = value;
        }
    }

    private sealed class FakeState : IState<TestContext>
    {
        private readonly string _name;

        public FakeState(string name)
        {
            _name = name;
        }

        public void Enter(TestContext context)
        {
            context.Calls.Add($"{_name}.Enter");
        }

        public void Tick(TestContext context, float deltaTime)
        {
            context.Calls.Add($"{_name}.Tick");
        }

        public void Exit(TestContext context)
        {
            context.Calls.Add($"{_name}.Exit");
        }
    }
}
