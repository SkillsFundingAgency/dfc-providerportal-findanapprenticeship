﻿using Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS;

namespace Dfc.Providerportal.FindAnApprenticeship.Models.DAS
{
    public class Contact : IContact
    {
        public string ContactUsUrl { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

    }
}