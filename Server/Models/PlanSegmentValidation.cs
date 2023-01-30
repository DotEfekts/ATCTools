using ATCTools.Shared.Models;

namespace ATCTools.Server.Models;

public class PlanSegmentValidation
{
    public string Code { get; set; }
    public string? SubInfo { get; set; }
    public string? MapCode { get; set; }
    public ValidationState State { get; set; }
    public string? ValidationMessage { get; set; }
    public PlanSegmentType Type { get; set; }
    public Location? Location { get; set; }
    public string? FlightLevelChange { get; set; }
    public string? SpeedChange { get; set; }
    public bool? ChangeToIFR { get; set; }

    public virtual string RebuildSegment()
    {
        return Code +
               (FlightLevelChange != null && SpeedChange != null ? "/" + FlightLevelChange + SpeedChange : "") +
               (ChangeToIFR != null ? " " + (ChangeToIFR == true ? "IFR" : "VFR") : "");
    }
}

public enum PlanSegmentType
{
    ENTITY, COORDINATE, UNKNOWN
}

public class DirectPlanSegment : PlanSegmentValidation
{
    public Airway? AirwayAlternate { get; set; }
}

public class AerodromePlanSegment : PlanSegmentValidation 
{
    public Aerodrome Aerodrome { get; set; }
}

public class SidPlanSegment : PlanSegmentValidation 
{
    public AerodromeSid Sid { get; set; }
    public string? SelectedRunway { get; set; }

    public override string RebuildSegment()
    {
        return Code + "/" + SelectedRunway;
    }
}

public class StarPlanSegment : PlanSegmentValidation 
{
    public AerodromeStar Star { get; set; }
    public string? SelectedRunway { get; set; }

    public override string RebuildSegment()
    {
        return Code + "/" + SelectedRunway;
    }
}

public class CodepointPlanSegment : PlanSegmentValidation
{
    public ICodePoint CodePoint { get; set; }
}

public class AirwayPlanSegment : PlanSegmentValidation
{
    public AirwayPoint? Entry { get; set; }
    public Airway Airway { get; set; }
    public bool? Reverse { get; set; }
    public AirwayPoint? Exit { get; set; }
}

