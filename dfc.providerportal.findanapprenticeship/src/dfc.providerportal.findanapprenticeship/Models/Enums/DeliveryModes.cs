using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.Enums
{
    public enum TribalDeliveryModes
    {
        [Display(Name = "Undefined")]
        [Description("Undefined")]
        Undefined = 0,
        [Display(Name = "100% Employer Based")]
        [Description("100% Employer Based")]
        EmployerBased = 1,
        [Display(Name = "Day release")]
        [Description("Day Release")]
        DayRelease = 2,
        [Display(Name = "Block release")]
        [Description("Block Release")]
        BlockRelease = 3,

    }
    public enum ApprenticeShipDeliveryLocation
    {
        [Display(Name = "Undefined")]
        [Description("Undefined")]
        Undefined = 0,
        [Display(Name = "Day release")]
        [Description("Day Release")]
        DayRelease = 1,
        [Display(Name = "Block release")]
        [Description("Block Release")]
        BlockRelease = 2,
        [Display(Name = "Employer address")]
        [Description("Employer address")]
        EmployerAddress = 3

    }

}
