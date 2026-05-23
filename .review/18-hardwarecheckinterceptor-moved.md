# Location and context
src/ZtrBoardGame.Console/Infrastructure/HardwareCheckInterceptor.cs:1

```csharp
diff --git a/src/ZtrBoardGame.Console/Infrastructure/HardwareCheckInterceptor.cs b/src/ZtrBoardGame.Console/Infrastructure/HardwareCheckInterceptor.cs
deleted file mode 100644
index 7ce7470..0000000
--- a/src/ZtrBoardGame.Console/Infrastructure/HardwareCheckInterceptor.cs
+++ /dev/null
```
```csharp
diff --git a/src/ZtrBoardGame.Console/Commands/Setup/HardwareCheckInterceptor.cs b/src/ZtrBoardGame.Console/Commands/Setup/HardwareCheckInterceptor.cs
new file mode 100644
index 0000000..9dfb8c2
```

Code is used to intercept commands to check hardware configuration.

# What is wrong
The file `HardwareCheckInterceptor.cs` was moved from `Infrastructure` to `Commands/Setup`. However, an interceptor is an infrastructure-level concern that runs across multiple commands (or specific commands based on logic), not a command itself, nor specific to the "Setup" command alone (it likely intercepts the `board run` command to ensure it's set up). Moving it to `Commands/Setup` makes the project structure confusing, as interceptors are usually placed in `Infrastructure`, `Middleware`, or a dedicated `Interceptors` folder.

# Solution
Keep `HardwareCheckInterceptor.cs` in the `Infrastructure` directory or an `Interceptors` directory.

# Assessment
2/6 - It is an architectural and organizational preference, but keeping cross-cutting concerns out of specific command folders helps maintain a clean structure.
