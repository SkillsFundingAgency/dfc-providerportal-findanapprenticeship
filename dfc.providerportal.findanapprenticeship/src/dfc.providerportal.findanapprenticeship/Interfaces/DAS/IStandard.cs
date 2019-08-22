using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS
{
    public interface IStandard
    {
        IContact Contact { get; set; }

        List<LocationRef> Locations { get; set; }
        string MarketingInfo { get; set; }
        int StandardCode { get; set; }
        string StandardInfoUrl { get; set; }
    }
}
