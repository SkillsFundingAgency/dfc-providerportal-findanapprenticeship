using System;
using System.Dynamic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using Newtonsoft.Json;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public static class ApprenticeshipLocationExtensions
    {
        public static int ToAddressHash(this ApprenticeshipLocation location)
        {
            if (location.Regions != null)
            {
                return GenerateHash(string.Join(",", location.Regions));
            }
            return GenerateLatLonHash(location.Address?.Latitude, location.Address?.Longitude);
        }

        private static int GenerateLatLonHash(double? lat, double? lon)
        {
            return (int)(lat * 1000000) + (int)(lon * 1000000);
        }

        private static int GenerateHash(string value)
        {
            using (var crypto = new SHA256CryptoServiceProvider())
            {
                var hash = 0;
                if (string.IsNullOrEmpty(value)) return (hash);

                var byteContents = Encoding.Unicode.GetBytes(value);

                var hashText = crypto.ComputeHash(byteContents);

                var α = BitConverter.ToInt64(hashText, 0);
                var β = BitConverter.ToInt64(hashText, 8);
                var γ = BitConverter.ToInt64(hashText, 24);

                hash = (int) (α ^ β ^ γ);
                return (hash);
            }
        }
    }

}