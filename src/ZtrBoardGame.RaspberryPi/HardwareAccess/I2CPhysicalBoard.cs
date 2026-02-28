using Microsoft.Extensions.Options;
using System.Device.Gpio;
using ZtrBoardGame.Configuration.Shared;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

interface IPhysicalBoard
{
    public IEnumerable<IField> GetFields();
}

public interface IPhysicalNotificator : IDisposable
{
    public Task BlinkRedAsync(TimeSpan time);
    public Task BlinkGreenAsync(TimeSpan time);
}

#pragma warning disable S101
class I2CPhysicalBoard : IPhysicalBoard, IPhysicalNotificator
#pragma warning restore S101
{
    private readonly List<IModule> _modules;
    private readonly GpioController _controller = new();

    public I2CPhysicalBoard(IOptions<PhysicalBoardSettings> options)
    {
        _controller.OpenPin(options.Value.InterruptPinNumber, PinMode.Input);
        _modules = options.Value
            .GetAddressesAsInt()
            .Select(address =>
                new FourFieldKubasModule(address, _controller, options.Value.InterruptPinNumber) as IModule)
            .ToList();
    }

    public IEnumerable<IField> GetFields()
        => _modules.SelectMany(m => m.GetFields());

    public void Dispose()
    {
        foreach (var module in _modules)
        {
            module.Dispose();
        }

        _controller.Dispose();
    }

    public async Task BlinkRedAsync(TimeSpan time)
    {
        var fields = GetFields().ToList();
        fields.ForEach(f => f.TurnLedsOff());

        fields.ForEach(f => f.TurnLedsOn(Led.Red));
        await Task.Delay(time);

        fields.ForEach(f => f.TurnLedsOff());
    }

    public async Task BlinkGreenAsync(TimeSpan time)
    {
        var fields = GetFields().ToList();
        fields.ForEach(f => f.TurnLedsOff());

        fields.ForEach(f => f.TurnLedsOn(Led.Green));
        await Task.Delay(time);

        fields.ForEach(f => f.TurnLedsOff());
    }
}
