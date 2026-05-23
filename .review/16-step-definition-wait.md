# Location and context
tests/ZtrBoardGame.Console.Tests/Features/StepDefinitions/FromBoardToPcStepDefinitions.cs:89

```csharp
    public void ThenTheApplicationShouldRunWithoutStartupErrors()
    {
        _helloService = _serviceProvider.GetRequiredService<IHelloService>();
        var announcementTask = _helloService.AnnouncePresenceAsync(_cancellationTokenSource.Token);

        announcementTask.Wait(TimeSpan.FromSeconds(1));
        announcementTask.IsFaulted.Should().BeFalse();
    }
```

Code is used in the `FromBoardToPcStepDefinitions` to assert that the application starts without errors.

# What is wrong
Using `Task.Wait(TimeSpan)` synchronously blocks the calling thread. While this might "work" in a simple test scenario, it is an anti-pattern in asynchronous programming (`async over sync`) and can lead to deadlocks, especially in integration test runners that might have their own synchronization contexts. The step definition method itself is not marked as `async`.

# Solution
Make the step definition `async Task` instead of `void` and use `await Task.Delay` combined with `Task.WhenAny` to implement the timeout asynchronously, or simply await the task if it's expected to finish, or use a testing library feature for timeouts. Assuming `AnnouncePresenceAsync` is a continuous background task that we just want to ensure starts without immediately faulting:

```csharp
    public async Task ThenTheApplicationShouldRunWithoutStartupErrors()
    {
        _helloService = _serviceProvider.GetRequiredService<IHelloService>();
        var announcementTask = _helloService.AnnouncePresenceAsync(_cancellationTokenSource.Token);

        var completedTask = await Task.WhenAny(announcementTask, Task.Delay(TimeSpan.FromSeconds(1)));

        // If announcementTask completes within 1s, it might have faulted. Check it.
        if (completedTask == announcementTask)
        {
            announcementTask.IsFaulted.Should().BeFalse();
        }
    }
```

# Assessment
3/6 - Mixing sync and async code in tests can lead to flaky tests or deadlocks. It's better to keep it fully async.
