using System.Text.RegularExpressions;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public static class StringExtensions
    {
        /// <summary>
        /// COUR-2346 - Ensure spaces after fullstops are preserved once html tags are stripped out
        /// </summary>
        /// <param name="input">the string to parse</param>
        /// <returns>A processed string with spaces after fullstops, except for the last fullstop</returns>
        public static string EnforceSpacesAfterFullstops(this string input)
        {
            return Regex.Replace(input, "\\.(?!\\s)(?!\\.)(?!$)", ". ");
        }
    }

}
