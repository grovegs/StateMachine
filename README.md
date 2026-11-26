# GroveGames.StateMachine

[![Build Status](https://github.com/grovegs/StateMachine/actions/workflows/release.yml/badge.svg)](https://github.com/grovegs/StateMachine/actions/workflows/release.yml)
[![Tests](https://github.com/grovegs/StateMachine/actions/workflows/tests.yml/badge.svg)](https://github.com/grovegs/StateMachine/actions/workflows/tests.yml)
[![NuGet](https://img.shields.io/nuget/v/GroveGames.StateMachine)](https://www.nuget.org/packages/GroveGames.StateMachine)
[![Latest Release](https://img.shields.io/github/v/release/grovegs/StateMachine)](https://github.com/grovegs/StateMachine/releases/latest)

Finite state machine with shared states, guarded transitions and per-agent context for .NET and Unity

## Features

- `IState<TContext>`: a state with `Enter`, `Tick` and `Exit` that keeps no data of its own, so one instance serves every agent
- `StateMachine<TContext>`: one machine per agent, holding that agent's context and current state
- Guarded transitions from one state, or from any state, checked on every tick
- A `StateChanged` event with the previous and the current state

## .NET

### Installation

```bash
dotnet add package GroveGames.StateMachine
```

### Usage

```csharp
using GroveGames.StateMachine;

var idle = new IdleState();
var chasing = new ChasingState();
var machine = new StateMachine<Enemy>(enemy);

machine.AddTransition(idle, chasing, static enemy => enemy.SeesTarget);
machine.AddAnyTransition(idle, static enemy => enemy.IsStunned);
machine.StateChanged += change => Console.WriteLine($"{change.Previous} -> {change.Current}");

machine.Start(idle);
machine.Tick(deltaTime);
machine.Stop();
```

`Tick` checks the any-state transitions first, then the transitions of the current state, in the order they were added. It follows at most eight transitions in one tick, then ticks the current state. `ChangeState` switches without a condition, and `Stop` exits the current state. A `ChangeState` or `Stop` called from `Enter`, `Exit` or a `StateChanged` handler waits until the change in progress has finished, so `StateChanged` always reports changes in order. More than 64 such changes in a row throw `InvalidOperationException`.

## Unity

### Installation

1. Install the core package via [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity):
   - Open Window > NuGet > Manage NuGet Packages
   - Search for `GroveGames.StateMachine` and install

2. Add the Unity package via Git URL:
   - Open Package Manager (Window > Package Manager)
   - Click "+" > "Add package from git URL"
   - Enter: `https://github.com/grovegs/StateMachine.git?path=src/GroveGames.StateMachine.Unity/Packages/com.grovegames.statemachine`

3. Create a `csc.rsp` file in your `Assets/` directory with the following content to enable C# 10 features:

   ```text
   -langversion:10
   -nullable:enable
   ```

### Usage

All types are in the core namespace, `GroveGames.StateMachine`. The Unity package adds no types; it makes the core available to asmdefs that reference `GroveGames.StateMachine.Unity`.

## Development

### Project Structure

- `src/GroveGames.StateMachine/` - Core .NET library (`net10.0`, `netstandard2.1`)
- `src/GroveGames.StateMachine.Unity/` - Unity package
- `tests/` - xUnit tests
- `sandbox/` - Sample applications for testing

### Commands

```bash
dotnet build                        # Build all projects
dotnet format                       # Format code
dotnet test                         # Run tests
dotnet pack -c Release              # Create NuGet packages
```

### Requirements

- .NET 10.0 SDK

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
