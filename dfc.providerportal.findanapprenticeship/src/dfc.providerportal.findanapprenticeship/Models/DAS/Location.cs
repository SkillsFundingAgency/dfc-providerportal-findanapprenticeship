using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;
using System;
using System.Collections.Generic;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.DAS
{
    public class Location : ILocation
    {
        public int? ID { get; set; }
        public string Name { get; set; }
        public IAddress Address { get; set; }
        public string Email { get; set; }
        public string Website { get; set; }
        public string Phone { get; set; }
    }
}
