using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal;
using System;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.Tribal
{
    public class LocationRef : ILocationRef
    {
        public List<int> DeliveryModes { get; set; }
        public int? ID { get; set; }
        public Guid GuidID { get; set; }
        public int Radius { get; set; }

    }
}
