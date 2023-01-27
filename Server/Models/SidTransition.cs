namespace ATCTools.Server.Models;

public class SidTransition
{
    public TransitionType Type { get; set; } 
    public string? Code { get; set; }
    public ICodePoint? Point { get; set; }
    public int? Track { get; set; }
}

public enum TransitionType
{
    NAV, RADAR
}