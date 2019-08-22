using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;
using System;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.DAS
{
    public class Location : ILocation
    {
        public IAddress Address { get; set; }
        public int? ID { get; set; }
        public List<int> DeliveryModes { get; set; }
        public Guid GuidID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Phone { get; set; }
    }
}
