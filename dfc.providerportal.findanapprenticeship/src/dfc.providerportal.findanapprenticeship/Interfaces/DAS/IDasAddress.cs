namespace Dfc.Providerportal.FindAnApprenticeship.Interfaces.DAS
{
    public interface IDasAddress
    {
        string Address1 { get; set; }

        string Address2 { get; set; }

        string County { get; set; }

        double? Latitude { get; set; }

        double? Longitude { get; set; }

        string Postcode { get; set; }

        string Town { get; set; }
    }
}