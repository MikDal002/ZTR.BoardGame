using Spectre.Console.Testing;

namespace ZtrBoardGame.Console.Tests.TestHelpers;

public static class TestConsoleInputTestExtensions
{
    public class Confirm(TestConsoleInput input)
    {
        public void PushAnswer(bool confirm)
        {
            if (confirm)
            {
                input.PushKey(ConsoleKey.Enter);
            }
            else
            {
                input.PushTextWithEnter("n");
            }
        }
    }

    extension(TestConsoleInput input)
    {
        public Confirm ForConfirm()
            => new(input);
    }
}
