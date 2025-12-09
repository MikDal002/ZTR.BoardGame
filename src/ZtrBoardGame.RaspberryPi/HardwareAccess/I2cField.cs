
using System.Collections;
using System.Device.I2c;

namespace ZtrBoardGame.RaspberryPi.HardwareAccess;

public enum Hallotron
{
    INVALID,
    IsEnagaged,
    IsDisengaged
}

public enum Led
{
    INVALID,
    Blue,
    Red,
    Green
}

public interface IField
{
    public event EventHandler OnHallotronEngaged;
    public Hallotron GetHallotronStatus(bool invokeEvent = false);
    public void TurnLedsOn(params Led[] leds);
    public void TurnLedsOff();
    public string Name { get; }
}

#pragma warning disable S101
internal class I2CField(I2cDevice device, object i2cLock, InternalFieldDefinition fieldDefinition) : IField
#pragma warning restore S101
{
    int StartingBitPosition => (int)(fieldDefinition.BitShift + fieldDefinition.ByteNumber * 8);
    private const int HallBitPosition = 3;
    private const int GreenBitPosition = 2;
    private const int RedBitPosition = 1;
    private const int BlueBitPosition = 0;

    public string Name => fieldDefinition.Name;

    public event EventHandler OnHallotronEngaged;

    public Hallotron GetHallotronStatus(bool invokeEvent = false)
    {
        var buffer = new byte[2];
        lock (i2cLock)
        {
            device.Read(buffer);
        }

        var bitArray = new BitArray(buffer);
        var isHallEngaged = !bitArray.Get(HallBitPosition + StartingBitPosition);

        if (!isHallEngaged)
        {
            return Hallotron.IsDisengaged;
        }

        if (invokeEvent)
        {
            OnHallotronEngaged?.Invoke(this, EventArgs.Empty);
        }

        return Hallotron.IsEnagaged;
    }

    public void TurnLedsOn(params Led[] leds)
    {
        var buffer = ReadFromDevice(out var bitArray);

        bitArray.Set(BlueBitPosition + StartingBitPosition, leds.Contains(Led.Blue));
        bitArray.Set(RedBitPosition + StartingBitPosition, leds.Contains(Led.Red));
        bitArray.Set(GreenBitPosition + StartingBitPosition, leds.Contains(Led.Green));

        bitArray.CopyTo(buffer, 0);
        lock (i2cLock)
        {
            device.Write(buffer);
        }
    }

    public void TurnLedsOff()
    {
        var buffer = ReadFromDevice(out var bitArray);

        bitArray.Set((BlueBitPosition + StartingBitPosition), false);
        bitArray.Set((RedBitPosition + StartingBitPosition), false);
        bitArray.Set((GreenBitPosition + StartingBitPosition), false);

        bitArray.CopyTo(buffer, 0);
        lock (i2cLock)
        {
            device.Write(buffer);
        }
    }

    byte[] ReadFromDevice(out BitArray bitArray)
    {
        var buffer = new byte[2];
        lock (i2cLock)
        {
            device.Read(buffer);
        }

        bitArray = new BitArray(buffer);
        return buffer;
    }
}
