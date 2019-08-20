using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal
{
    public interface ILocationRef
    {
        List<int> DeliveryModes { get; set; }
        int? ID { get; set; }
        Guid GuidID { get; set; }
        int Radius { get; set; }
    }
}
