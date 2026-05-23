Location: `tests/ZtrBoardGame.Console.Tests/Features/StepDefinitions/FromBoardToPcStepDefinitions.cs`, `AfterScenario` method

What is wrong and why:
In `AfterScenario`, you are awaiting `_announcementTask` and catching `OperationCanceledException`. However, `_announcementTask.Wait(TimeSpan.FromSeconds(1));` is called synchronously in `ThenTheApplicationShouldRunWithoutStartupErrors`. If `AnnouncePresenceAsync` throws an exception *other* than `OperationCanceledException` (e.g., a network error), `AfterScenario` will throw that exception, potentially masking the actual test failure or causing unexpected test runner behavior. Also, mixing synchronous `.Wait()` and asynchronous `await` on the same task can lead to confusion.

Proposed solution:
In `AfterScenario`, you should either catch `AggregateException` (since `.Wait()` was called earlier, although `await` unrolls it, it's safer to catch a broader set of exceptions if you just want to suppress them during cleanup), or just log the exception instead of silently swallowing `OperationCanceledException` and letting others propagate during teardown.
