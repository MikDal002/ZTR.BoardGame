using System.Device.Gpio;
using System.Device.I2c;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

interface IModule
{
    public IEnumerable<IField> GetFields();
}

internal class FourFieldKubasModule : IModule, IDisposable
{
    private readonly Object _fieldReadLock = new();
    private readonly GpioController _controller = new();
    readonly I2cDevice _device;
    IField[] _fields;
    private static int THE_ONLY_MODULE_INTERRUPT_PIN = 4;
    private bool _disposed;

    public FourFieldKubasModule(int address = 0x20)
    {
        I2cConnectionSettings settings = new(1, address);
        _device = I2cDevice.Create(settings);
        _device.Write(new byte[] { 0xFF, 0xFF }); // Inicjalizacja - wszystko na 1

        _controller.OpenPin(THE_ONLY_MODULE_INTERRUPT_PIN, PinMode.Input);
        _controller.RegisterCallbackForPinValueChangedEvent(THE_ONLY_MODULE_INTERRUPT_PIN,
            PinEventTypes.Falling,
            Callback);
    }

    void Callback(object s, PinValueChangedEventArgs e)
    {
        var buffer = new byte[2];
        _device.Read(buffer);

        foreach (var internalField in GetFields())
        {
            internalField.GetHallotronStatus(true);
        }
    }

    public IEnumerable<IField> GetFields()
    {
        lock (_fieldReadLock)
        {
            if (_fields is not null)
            {
                return _fields;
            }

            var field01 = new InternalFieldDefinition("F1", 0, FieldReadShift.ByZero);
            var field02 = new InternalFieldDefinition("F2", 0, FieldReadShift.ByFour);
            var field03 = new InternalFieldDefinition("F3", 1, FieldReadShift.ByZero);
            var field04 = new InternalFieldDefinition("F4", 1, FieldReadShift.ByFour);

            _fields = new IField[]
            {
                new I2CField(_device, field01), new I2CField(_device, field02), new I2CField(_device, field03),
                new I2CField(_device, field04),
            };

            return _fields;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _controller.Dispose();
            _device.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
