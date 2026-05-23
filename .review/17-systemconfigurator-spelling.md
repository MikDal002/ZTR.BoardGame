# Location and context
src/ZtrBoardGame.Console/Commands/Setup/SystemConfiguratorOrchestrator.cs:11

```csharp
public interface ISystemConfiguratorOrchestrator
{
    IAsyncEnumerable<ISystemConfigurer> GetSystemWhichNeedsConfiguration();
}
```

Code is used in the `ISystemConfiguratorOrchestrator` interface.

# What is wrong
The method name `GetSystemWhichNeedsConfiguration` has a grammar/plurality issue. It returns an `IAsyncEnumerable<ISystemConfigurer>`, meaning it can yield multiple configurers, but the method name implies it returns a single system ("GetSystem"). It should be pluralized.

# Solution
Rename the method to `GetSystemsWhichNeedConfiguration` or, even better and more concise, `GetPendingConfigurations` or `GetConfigurersRequiringSetup`.

```csharp
public interface ISystemConfiguratorOrchestrator
{
    IAsyncEnumerable<ISystemConfigurer> GetSystemsWhichNeedConfigurationAsync(); // Note: Async suffix is also good practice
}
```

# Assessment
2/6 - A very minor naming issue, but fixing it improves code readability.
