using EmbedIO;

namespace ViciOne.Suite.DataPort;

internal record AppEntry(WebServer Server)
{
    public int UserCount { get; set; }
    public int StartRequested { get; set; }
    public bool IsRunning { get; set; }
}
