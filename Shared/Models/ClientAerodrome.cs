namespace ATCTools.Shared.Models;

public class ClientAerodrome
{
    public string Code { get; set; }
    public string Name { get; set;  }
    public State State { get; set; }
    public Location Location { get; set;  }
    public string? SoP { get; set;  }
    public string? Parent { get; set;  }
    public ClientAerodromeChart[] Charts { get; set;  }
    public ClientSid[] Sids { get; set;  }
    public ClientStar[] Stars { get; set;  }
}