using System;

namespace ZtrBoardGame.Configuration.Shared;

public class BoardNetworkSettings
{
    public string PcServerAddress { get; set; } = string.Empty;
    public string BoardAddress { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
