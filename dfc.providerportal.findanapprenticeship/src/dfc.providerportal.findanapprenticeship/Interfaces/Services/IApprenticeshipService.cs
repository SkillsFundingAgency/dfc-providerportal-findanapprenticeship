using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Apprenticeships;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Models;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Tribal;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.Services
{
    public interface IApprenticeshipService
    {
        Task<IEnumerable<IStandardsAndFrameworks>> StandardsAndFrameworksSearch(string search);
        Task<IApprenticeship> AddApprenticeship(IApprenticeship apprenticeship);
        Task<IApprenticeship> GetApprenticeshipById(Guid id);
        Task<IStandardsAndFrameworks> GetStandardsAndFrameworksById(Guid id, int type);
        Task<IEnumerable<IApprenticeship>> GetApprenticeshipByUKPRN(int UKPRN);
        Task<IApprenticeship> Update(IApprenticeship apprenticeship);
        Task<HttpResponseMessage> ChangeApprenticeshipStatusForUKPRNSelection(int UKPRN, RecordStatus CurrentStatus, RecordStatus StatusToBeChangedTo);
        Task<List<string>> DeleteBulkUploadApprenticeships(int UKPRN);
        Task<List<string>> DeleteApprenticeshipsByUKPRN(int UKPRN);
        Task<IEnumerable<IApprenticeship>> GetApprenticeshipCollection();
        IEnumerable<ITribalProvider> ApprenticeshipsToTribalProviders(List<Apprenticeship> apprenticeships);
        Task<IEnumerable<IApprenticeship>> GetUpdatedApprenticeships();
        IEnumerable<IStandardsAndFrameworks> CheckForDuplicateApprenticeships(IEnumerable<IStandardsAndFrameworks> standardsAndFrameworks, int UKPRN);
    }
}
