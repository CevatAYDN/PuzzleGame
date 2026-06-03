# Test Policy — PuzzleGame

The test suite is **NUnit + Unity Test Framework**, and the project deliberately uses hand-written `Fake*` doubles instead of a mocking library.

## Frameworks

- NUnit attributes: `[Test]`, `[TestFixture]`, `[SetUp]`, `[TearDown]`, `[TestCase]`.
- Unity Test Framework (`com.unity.test-framework` 1.6.0) for Unity-runtime tests; pure NUnit for Domain tests.
- Test runner: `Window > General > Test Runner` in the Editor, or `dotnet test PuzzleGame.Tests.csproj` from CLI when asmdefs allow.

## Layout

Tests mirror the layer they cover:

```
Assets/Tests/
├── Domain/
│   ├── Models/                  # covers Assets/Scripts/Domain/Models
│   └── Services/                # covers Assets/Scripts/Domain/Services
├── Application/
│   └── Services/                # covers Assets/Scripts/Application/Services
├── Infrastructure/
│   └── Pool/                    # covers Assets/Scripts/Infrastructure/Pool etc.
├── Events/                      # covers the EventAggregator in Application/Events
└── Fakes/                       # Fake* test doubles shared across fixtures
```

One fixture per public class. Filename = `<ClassName>Tests.cs`. One `[Test]` per behaviour — split cases rather than chaining asserts.

## Fakes, not mocks

Use hand-written `Fake*` classes in `Assets/Tests/Fakes/`. They are deterministic, allocation-free, and have zero third-party dependencies.

When you need a new fake:

1. Read 1–2 existing fakes (`FakeBottleValidator`, `FakeAnimationService`, `FakeTweenService`, `FakePourService`) to match the style.
2. Implement the interface explicitly — no `throw new NotImplementedException()` paths; default values are fine, the test should set what it needs.
3. Expose counters / recorded calls as public `int XxxCallCount` / `List<Xxx> XxxCalls` properties so tests can assert on them.

**Do not** add Moq, NSubstitute, FakeItEasy, or any mocking framework. If a reviewer sees one in a `Packages/manifest.json` or `.csproj`, it gets rejected.

## Domain tests must stay Unity-free

The Domain layer's whole point is that it compiles and tests without Unity. If a Domain test imports `UnityEngine.*`, the production code has the wrong dependency — flag it, do not paper over it.

## Coverage expectations

- New public methods on `Domain/Services/**` and `Domain/Models/**` get tests.
- New services in `Application/Services/**` get at least one happy-path and one edge-case test.
- New `Infrastructure/Implementations/**` types get a fakes-based test that verifies they implement the interface correctly (no Unity runtime test required if the type has no Unity dependency).
- `Editor/**` tools are tested manually with a documented scenario, not via automated tests.

## When you write a test

- Arrange / Act / Assert, with a blank line between each.
- One assertion theme per `[Test]`.
- Use `Assert.That(actual, Is.EqualTo(expected))` (the constraint model), not the legacy `Assert.AreEqual`.
- For collections, prefer `Is.EquivalentTo` for order-independent or `Is.EqualTo` for strict-order.
- For exceptions, `Assert.Throws<T>(() => ...)` or `Assert.That(() => ..., Throws.TypeOf<T>())`.

## Running the suite

- **Unity Editor:** `Window > General > Test Runner > EditMode` for the bulk of tests; `PlayMode` only when a test genuinely needs the runtime.
- **CLI:** `dotnet test PuzzleGame.Tests.csproj -c Debug` works for the pure-Domain / pure-Application tests that don't need the Unity runtime. The full suite needs the Editor.
- A green test run is part of the "done" definition for any change. A skipped test is a code-review issue.
