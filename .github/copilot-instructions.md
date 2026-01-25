# Copilot Instructions for PicarX

Context
- Project: PicarX
- Language: C# targeting .NET 8
- Hardware: SunFounder / PiCar-X style robot (Raspberry Pi hardware bindings + Windows dev mode)
- Key runtime dependencies: `OPENAI_API_KEY`, `AZURE_SPEACH_KEY`

What Copilot should know
- Avoid generating direct hardware calls without checking platform: always guard with `Environment.OSVersion.Platform`.
- Prefer changes that keep hardware abstractions (`ICamera`, `ISoundPlayer`, `PicarX` implementations) so logic can be tested off-device.
- Keep DI service lifetimes consistent: controller and hardware services as singletons; background services as hosted services.
- Minimize new dependencies; prefer using existing project abstractions.
- For tests, suggest mocking/stubbing hardware interfaces rather than touching GPIO or I2C.

Coding style
- Follow repository style: concise comments, consistent indentation, and existing naming conventions.
- Use async/await for I/O-bound operations and cancellation tokens where appropriate.

If proposing changes
- Provide small, focused diffs that do not alter cross-cutting abstractions without justification.
- When adding features that require secrets, remind to use environment variables and not to commit keys.

Files to prefer when looking for behavior
- `Program.cs` - startup and DI registration
- `PicarX/` - core hardware implementations
- `Media/` - audio and camera handling
- `ChatGpt/` - conversational logic and TTS/STT integrations

Repository maintainer contact
- origin: https://github.com/veselkamartin/picarx
