using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal
{
    public interface ILocation
    {
        IAddress Address { get; set; }
        int? ID { get; set; }
        Guid GuidID { get; set; }
        List<int> DeliveryModes { get; set; }
        string Name { get; set; }
        string Email { get; set; }
        string Website { get; set; }
        string Phone { get; set; }
    }
}
