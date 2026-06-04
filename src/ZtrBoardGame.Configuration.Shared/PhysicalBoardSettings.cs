using System;
using System.Collections.Generic;
using System.Linq;

namespace ZtrBoardGame.Configuration.Shared;

public class PhysicalBoardSettings
{
    public IReadOnlyCollection<string> Addresses { get; set; } = new List<string>(); // for example 0x20, 0x21, 0x22, 0x23
    public int InterruptPinNumber { get; set; } = 4; // Usually 4
    public int FieldsInPlay { get; set; } = 4; // Usually 4
    public int AmountOfFieldsPerAddressableBoard { get; set; } = 4;
    public IEnumerable<int> GetAddressesAsInt()
        => Addresses.Select(x => Convert.ToInt32(x.Trim(), 16));

    public int AmountOfFields()
        => Addresses.Count * AmountOfFieldsPerAddressableBoard;
}
