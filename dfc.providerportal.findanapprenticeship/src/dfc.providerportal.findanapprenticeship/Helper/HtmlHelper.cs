using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public static class HtmlHelper
    {
        public static string StripHtmlTags(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            return doc.DocumentNode.InnerText.EnforceSpacesAfterFullstops();
        }
    }
}
