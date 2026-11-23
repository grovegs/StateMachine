## Unity

- The types are in the `GroveGames.StateMachine` namespace. The Unity package adds none.
- An asmdef that overrides references lists `GroveGames.StateMachine.dll` in its precompiled references.
- A state is a plain class, never a MonoBehaviour. It reaches the scene through the context.
