using System;

namespace ZtrBoardGame.Configuration.Shared;

public class BoardNetworkSettings
{
    public string PcServerAddress { get; set; }
    public string BoardAddress { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
