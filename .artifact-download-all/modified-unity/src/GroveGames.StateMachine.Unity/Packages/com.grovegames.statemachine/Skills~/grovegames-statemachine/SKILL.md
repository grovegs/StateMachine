---
name: grovegames-statemachine
description: Rules for agent behavior built on GroveGames.StateMachine in game code. Use when writing or reviewing a state, a transition condition, a StateMachine per agent, or code that calls Start, Tick, ChangeState or Stop on one.
---

# Using GroveGames.StateMachine

Rules for game code that uses this package. They hold in every game. The API is in the package's `README.md`.

- A state keeps no per-agent data. Create each state once and share it between all agents; everything that differs per agent lives in the context.
- Create one `StateMachine<TContext>` per agent and reuse it when the agent is pooled: `Stop` it on return and `Start` it again on rent.
- A transition condition is a delegate that is created once and cached, or a `static` lambda. Never pass a method group or a capturing lambda per agent; it allocates for every agent.
- Conditions only read the context. They run on every tick, so they change nothing and stay cheap.
- Order matters: any-state transitions are checked before the transitions of the current state, each in the order they were added. Add the most urgent one first.
- Let conditions drive the flow. Call `ChangeState` only for a change no condition can express, such as a forced reset.
- Something owns the tick. Call `Tick(deltaTime)` once per frame from the service or controller that owns the agents; a machine does not tick itself.
- React to changes through `StateChanged`, and unsubscribe when the owner is disposed.

## Unity

- The types are in the `GroveGames.StateMachine` namespace. The Unity package adds none.
- An asmdef that overrides references lists `GroveGames.StateMachine.dll` in its precompiled references.
- A state is a plain class, never a MonoBehaviour. It reaches the scene through the context.
