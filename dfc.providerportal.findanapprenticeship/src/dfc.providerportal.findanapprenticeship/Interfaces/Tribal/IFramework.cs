using Dfc.Providerportal.FindAnApprenticeship.Models.Tribal;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal
{
    public interface IFramework
    {
        IContact Contact { get; set; }

        int FrameworkCode { get; set; }

        int? ProgType { get; set; }

        int? Level { get; set; }

        List<LocationRef> Locations { get; set; }
        int? PathwayCode { get; set; }

        string FrameworkInfoUrl { get; set; }

        string MarketingInfo { get; set; }
    }
}
