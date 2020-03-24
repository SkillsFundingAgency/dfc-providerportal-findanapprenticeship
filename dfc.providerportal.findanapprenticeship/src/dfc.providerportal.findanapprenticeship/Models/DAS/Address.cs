﻿using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.DAS
{
    public class Address : IAddress
    {
        public string Address1 { get; set; }

        public string Address2 { get; set; }

        public string County { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string Postcode { get; set; }

        public string Town { get; set; }
    }
}