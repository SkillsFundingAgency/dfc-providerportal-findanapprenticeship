using Dfc.Providerportal.FindAnApprenticeship.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper
{
    public interface IReferenceDataServiceWrapper
    {
        IEnumerable<FeChoice> GetFeChoicesByUKPRN(string UKPRN);
    }
}
