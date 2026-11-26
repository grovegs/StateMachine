using System.Collections.Generic;

namespace GroveGames.StateMachine.Tests;

public sealed class StateMachineTests
{
    private readonly FakeContext _context;
    private readonly StateMachine<FakeContext> _machine;
    private readonly FakeState _idle;
    private readonly FakeState _walking;
    private readonly FakeState _running;

    public StateMachineTests()
    {
        _context = new FakeContext();
        _machine = new StateMachine<FakeContext>(_context);
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
        var changes = new List<StateChangedEvent<FakeContext>>();
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

    [Fact]
    public void ChangeState_StateChangesAgainInEnter_RaisesEventsInOrder()
    {
        var events = new List<string>();
        var redirect = new FakeCallbackState("Redirect", context => _machine.ChangeState(_running));
        _machine.StateChanged += change => events.Add($"{Name(change.Previous)}->{Name(change.Current)}");
        _machine.Start(_idle);

        _machine.ChangeState(redirect);

        Assert.Equal(new[] { "->Idle", "Idle->Redirect", "Redirect->Running" }, events);
        Assert.Same(_running, _machine.Current);
    }

    [Fact]
    public void ChangeState_StateChangesAgainInEnter_FinishesEnterBeforeExit()
    {
        var redirect = new FakeCallbackState("Redirect", context => _machine.ChangeState(_running));
        _machine.Start(_idle);
        _context.Calls.Clear();

        _machine.ChangeState(redirect);

        Assert.Equal(new[] { "Idle.Exit", "Redirect.Enter", "Redirect.Entered", "Redirect.Exit", "Running.Enter" }, _context.Calls);
    }

    [Fact]
    public void Tick_StateStopsInEnter_StaysStopped()
    {
        var stopping = new FakeCallbackState("Stopping", context => _machine.Stop());
        _machine.AddTransition(_idle, stopping, context => true);
        _machine.AddAnyTransition(_running, context => !_machine.IsRunning || _machine.Current == stopping);
        _machine.Start(_idle);

        _machine.Tick(0.1f);

        Assert.False(_machine.IsRunning);
        Assert.DoesNotContain("Running.Enter", _context.Calls);
    }

    [Fact]
    public void ChangeState_StatesThatKeepSwitching_ThrowsInvalidOperationException()
    {
        FakeCallbackState? second = null;
        var first = new FakeCallbackState("First", context => _machine.ChangeState(second!));
        second = new FakeCallbackState("Second", context => _machine.ChangeState(first));

        Assert.Throws<InvalidOperationException>(() => _machine.Start(first));
    }

    [Fact]
    public void ChangeState_Null_ThrowsArgumentNullException()
    {
        _machine.Start(_idle);

        Assert.Throws<ArgumentNullException>(() => _machine.ChangeState(null!));
        Assert.Same(_idle, _machine.Current);
    }

    [Fact]
    public void AddTransition_NullCondition_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _machine.AddTransition(_idle, _walking, null!));
        Assert.Throws<ArgumentNullException>(() => _machine.AddAnyTransition(_walking, null!));
    }

    private static string? Name(IState<FakeContext>? state)
    {
        return state switch
        {
            FakeState fake => fake.Name,
            FakeCallbackState callback => callback.Name,
            _ => null
        };
    }

    private sealed class FakeContext
    {
        private readonly List<string> _calls;
        private int _value;

        public FakeContext()
        {
            _calls = new List<string>();
            _value = 0;
        }

        public List<string> Calls => _calls;
        public int Value => _value;

        public void SetValue(int value)
        {
            _value = value;
        }
    }

    private sealed class FakeState : IState<FakeContext>
    {
        private readonly string _name;

        public string Name => _name;

        public FakeState(string name)
        {
            _name = name;
        }

        public void Enter(FakeContext context)
        {
            context.Calls.Add($"{_name}.Enter");
        }

        public void Tick(FakeContext context, float deltaTime)
        {
            context.Calls.Add($"{_name}.Tick");
        }

        public void Exit(FakeContext context)
        {
            context.Calls.Add($"{_name}.Exit");
        }
    }

    private sealed class FakeCallbackState : IState<FakeContext>
    {
        private readonly string _name;
        private readonly Action<FakeContext> _onEnter;

        public string Name => _name;

        public FakeCallbackState(string name, Action<FakeContext> onEnter)
        {
            _name = name;
            _onEnter = onEnter;
        }

        public void Enter(FakeContext context)
        {
            context.Calls.Add($"{_name}.Enter");
            _onEnter(context);
            context.Calls.Add($"{_name}.Entered");
        }

        public void Tick(FakeContext context, float deltaTime)
        {
            context.Calls.Add($"{_name}.Tick");
        }

        public void Exit(FakeContext context)
        {
            context.Calls.Add($"{_name}.Exit");
        }
    }
}
