using System;
using Xunit;

namespace Dfc.ProviderPortal.FindAnApprenticeship.UnitTests
{
    public class ApprenticeshipServiceTests : IDisposable
    {
        public ApprenticeshipServiceTests()
        {
            // common test scaffolding
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
