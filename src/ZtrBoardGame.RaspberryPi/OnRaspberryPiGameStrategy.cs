using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System.Diagnostics;
using ZtrBoardGame.RaspberryPi.HardwareAccess;

namespace ZtrBoardGame.RaspberryPi;

public static class InstallRaspberryPiGameStrategy
{
    public static IServiceCollection AddRaspberryPiGameStrategy(this IServiceCollection services)
    {
        services.AddSingleton<IGameStrategy, OnRaspberryPiGameStrategy>();
        services.AddSingleton<IModule, FourFieldKubasModule>();
        return services;
    }
}

class OnRaspberryPiGameStrategy : IGameStrategy
{
    readonly IAnsiConsole _console;
    readonly List<IField> _fields;

    public OnRaspberryPiGameStrategy(IModule module, IAnsiConsole console)
    {
        _console = console;
        _fields = module.GetFields().ToList();
    }

    private async Task SignalBegginingOrFinishOfTheGame()
    {
        _fields.ForEach(f => f.TurnLedsOff());

        var pulseDuration = TimeSpan.FromMilliseconds(500);

        await Task.Delay(pulseDuration);

        _fields.ForEach(f => f.TurnLedsOn(Led.Green));
        await Task.Delay(pulseDuration);

        _fields.ForEach(f => f.TurnLedsOn(Led.Red));

        await Task.Delay(pulseDuration);

        _fields.ForEach(f => f.TurnLedsOn(Led.Blue));

        await Task.Delay(pulseDuration);
        _fields.ForEach(f => f.TurnLedsOff());

        await Task.Delay(pulseDuration);
    }
    private async Task<TimeSpan> DoActualGame(FieldOrder order)
    {
        var totalAnotherStopWatch = TimeSpan.Zero;

        foreach (var fieldId in order.Order)
        {
            var fieldTime = await PlaySingleField(fieldId);
            totalAnotherStopWatch += fieldTime;

            _console.WriteLine("Moving to next field");
        }

        _console.WriteLine($"Twój wynik z innego StopWatcha ${totalAnotherStopWatch}");
        return totalAnotherStopWatch;
    }

    async Task<TimeSpan> PlaySingleField(int fieldId)
    {
        var mre = new ManualResetEvent(false);
        var currentField = _fields[fieldId];
        TimeSpan? anotherStopWatchEngaged = null;
        var anotherStopWatch = new Stopwatch();

        currentField.OnHallotronEngaged += (sender, timeOfEvent) =>
        {
            anotherStopWatchEngaged = anotherStopWatch.Elapsed;
            mre.Set();
        };

        currentField.TurnLedsOn(Led.Red);

        while (true)
        {
            _console.WriteLine($"Waiting for pawn to be placed on field {currentField.Name}...");

            mre.WaitOne();

            _console.WriteLine($"Pawn placed on field {currentField.Name} at {anotherStopWatch}");

            await SequenceForPawnToBeHold(currentField);

            if (CheckIfPawnSitsOnTheField(currentField, anotherStopWatch, mre))
            {
                return anotherStopWatchEngaged!.Value;
            }
        }
    }

    bool CheckIfPawnSitsOnTheField(IField currentField, Stopwatch anotherStopWatch,
        ManualResetEvent mre)
    {
        if (currentField.GetHallotronStatus() != Hallotron.IsEnagaged)
        {
            _console.WriteLine($"Move on field {currentField.Name} was too short. Waiting for next attempt...");
            mre.Reset();

            return false;
        }

        anotherStopWatch.Stop();

        _console.WriteLine($"Valid move on field {currentField.Name} confirmed at {DateTime.Now}");
        currentField.TurnLedsOn(Led.Green);

        return true;
    }

    static async Task SequenceForPawnToBeHold(IField currentField)
    {
        currentField.TurnLedsOff();

        var holedSequencePulseDuration = TimeSpan.FromMilliseconds(250);
        await Task.Delay(holedSequencePulseDuration);
        currentField.TurnLedsOn(Led.Red);

        await Task.Delay(holedSequencePulseDuration);
        currentField.TurnLedsOff();

        await Task.Delay(holedSequencePulseDuration);
        currentField.TurnLedsOn(Led.Red);

        await Task.Delay(holedSequencePulseDuration);
        currentField.TurnLedsOff();
    }

    public async Task<TimeSpan> Do(FieldOrder order)
    {
        Console.WriteLine("Sekwencja rozpoczynająca");
        await SignalBegginingOrFinishOfTheGame();

        Console.WriteLine("Rozpoczynanie właściwej gry");
        var result = await DoActualGame(order);

        Console.WriteLine("Sekwencja kończąca");
        await SignalBegginingOrFinishOfTheGame();

        return result;
    }
}

