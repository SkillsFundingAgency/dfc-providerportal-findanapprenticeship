using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.Providerportal.FindAnApprenticeship.Settings
{
    public class ReferenceDataServiceSettings : IReferenceDataServiceSettings
    {
        public string ApiUrl { get; set; }
        public string ApiKey { get; set; }
    }
}
