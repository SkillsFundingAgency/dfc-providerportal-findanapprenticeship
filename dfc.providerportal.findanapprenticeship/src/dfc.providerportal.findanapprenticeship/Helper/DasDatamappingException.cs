using System;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class DataMappingException : Exception
    {
        public DataMappingException()
        {
        }

        public DataMappingException(string message)
            : base(message)
        {
        }

        public DataMappingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}