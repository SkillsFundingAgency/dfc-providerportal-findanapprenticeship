﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.Providerportal.FindAnApprenticeship.Models;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services
{
    public interface IReferenceDataService
    {
        IEnumerable<FeChoice> GetAllFeChoices();
        Task<IEnumerable<FeChoice>> GetAllFeChoicesAsync();
    }
}