
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
// @Roo-Code
// Review Step 5/9: Code Quality Standards.
//
// Problem:
//      1. DRY Violation: The `TurnLedsOn` and `TurnLedsOff` methods contain duplicated code for reading the current state of the I2C device and preparing the `BitArray`.
//      2. Magic Numbers: The class uses magic numbers (3, 2, 1, 0) to define the bit positions for the hall sensor and the different colored LEDs. This makes the code hard to read and prone to errors if the hardware layout changes.
//
// Suggestion:
//      1. Refactor the duplicated code into a private helper method that reads the device state and returns a `BitArray`. Both `TurnLedsOn` and `TurnLedsOff` can then call this method.
//      2. Replace the magic numbers with named constants (e.g., `private const int HallBitPosition = 3;`) to make the code self-documenting and easier to maintain.
//
// Confidence: 10/10
internal class I2CField(I2cDevice device, InternalFieldDefinition fieldDefinition) : IField
#pragma warning restore S101
{
    int StartingBitPosition => (int)(fieldDefinition.BitShift + fieldDefinition.ByteNumber * 8);
    const int HallPosition = 3;
    const int GreenPosition = 2;
    const int RedPosition = 1;
    const int BluePosition = 0;

    public string Name => fieldDefinition.Name;

    public event EventHandler OnHallotronEngaged;

    public Hallotron GetHallotronStatus(bool invokeEvent = false)
    {
        var buffer = new byte[2];
        device.Read(buffer);

        var bitArray = new BitArray(buffer);
        var isHallEngaged = !bitArray.Get(HallPosition + StartingBitPosition);

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

        bitArray.Set(BluePosition + StartingBitPosition, leds.Contains(Led.Blue));
        bitArray.Set(RedPosition + StartingBitPosition, leds.Contains(Led.Red));
        bitArray.Set(GreenPosition + StartingBitPosition, leds.Contains(Led.Green));

        bitArray.CopyTo(buffer, 0);
        device.Write(buffer);
    }

    public void TurnLedsOff()
    {
        var buffer = ReadFromDevice(out var bitArray);

        bitArray.Set((BluePosition + StartingBitPosition), false);
        bitArray.Set((RedPosition + StartingBitPosition), false);
        bitArray.Set((GreenPosition + StartingBitPosition), false);

        bitArray.CopyTo(buffer, 0);
        device.Write(buffer);
    }

    byte[] ReadFromDevice(out BitArray bitArray)
    {
        var buffer = new byte[2];
        device.Read(buffer);

        bitArray = new BitArray(buffer);
        return buffer;
    }
}
