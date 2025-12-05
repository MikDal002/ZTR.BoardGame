namespace ZtrBoardGame.RaspberryPi;

enum FieldReadShift
{
    INVALID,
    ByZero,
    ByFour
}

record InternalFieldDefinition
{
    public InternalFieldDefinition(string Name, uint ByteNumber, FieldReadShift Shift)
    {
        if (ByteNumber != 1 && ByteNumber != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ByteNumber), "ByteNumber must be 0 or 1");
        }

        this.Name = Name;
        this.ByteNumber = ByteNumber;

        BitShift = Shift switch
        {
            FieldReadShift.ByZero => 0,
            FieldReadShift.ByFour => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(Shift), "Invalid BitShift value")
        };
    }

    public string Name { get; }
    public uint ByteNumber { get; }
    public FieldReadShift Shift { get; }
    public uint BitShift { get; }
}
