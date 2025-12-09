using Microsoft.Extensions.Options;
using System.Device.Gpio;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

interface IPhysicalBoard
{
    public IEnumerable<IField> GetFields();
}

class PhysicalBoardSettings
{
    public IReadOnlyCollection<string> Addresses { get; set; } = new List<string>(); // for example 0x20, 0x21, 0x22, 0x23
    public int InterruptPinNumber { get; set; } // Usually 4

    public IEnumerable<int> GetAddressesAsInt()
        => Addresses.Select(x => Convert.ToInt32(x.Trim(), 16));
}

#pragma warning disable S101
class I2CPhysicalBoard : IPhysicalBoard, IDisposable
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
    {
        return _modules.SelectMany(m => m.GetFields());
    }

    public void Dispose()
    {
        foreach (var module in _modules)
        {
            module.Dispose();
        }

        _controller.Dispose();
    }
}
