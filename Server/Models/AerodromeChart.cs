namespace ATCTools.Server.Models;

public struct AerodromeChart
{
    public AerodromeChart(Aerodrome aerodrome, string name, string path, string updated, int am)
    {
        Aerodrome = aerodrome;
        Name = name;
        Path = path;
        Updated = updated;
        Amendment = am;
    }
    
    public Aerodrome Aerodrome { get; }
    public string Name { get; }
    public string Path { get; }
    public string Updated { get; }
    public int Amendment { get; }
}