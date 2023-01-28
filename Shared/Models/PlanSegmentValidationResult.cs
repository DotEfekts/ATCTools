namespace ATCTools.Shared.Models;

public class PlanSegmentValidationResult
{
    public string Segment { get; set; }
    public string? MapCode { get; set; }
    public Location? Location { get; set; }
    public ValidationState State { get; set; }
    public string? StateDetails { get; set; }
}

public enum ValidationState
{
    VALID, INVALID, UNVALIDATED, WARNING, INTERNATIONAL
}