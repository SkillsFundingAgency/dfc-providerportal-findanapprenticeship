using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Models.Regions;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.ProviderPortal.Packages;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class DASHelper : IDASHelper
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly IReferenceDataServiceWrapper _referenceDataServiceWrapper;
        private int _intIdentifier { get; set; }

        public DASHelper(TelemetryClient telemetryClient, IReferenceDataServiceWrapper referenceDataServiceWrapper)
        {
            Throw.IfNull(telemetryClient, nameof(telemetryClient));
            Throw.IfNull(referenceDataServiceWrapper, nameof(referenceDataServiceWrapper));

            _telemetryClient = telemetryClient;
            _referenceDataServiceWrapper = referenceDataServiceWrapper;
            _intIdentifier = 300000;
        }

        [Obsolete("Please don't use this any more, instead replace with a mapper class using something like AutoMapper", false)]
        public DASProvider CreateDASProviderFromProvider(Provider provider)
        {
            if (!int.TryParse(provider.UnitedKingdomProviderReferenceNumber, out int ukprn))
            {
                throw new InvalidUkprnException(provider.UnitedKingdomProviderReferenceNumber);
            }

            if (!provider.ProviderContact.Any())
            {
                throw new MissingContactException();
            }

            try
            {
                var contactDetails = provider.ProviderContact.FirstOrDefault();

                var feChoice = _referenceDataServiceWrapper
                    .GetFeChoicesByUKPRN(provider.UnitedKingdomProviderReferenceNumber);

                return new DASProvider
                {
                    Id = provider.ProviderId ?? GenerateIntIdentifier(),
                    Email = contactDetails?.ContactEmail,
                    EmployerSatisfaction = feChoice?.EmployerSatisfaction,
                    LearnerSatisfaction = feChoice?.LearnerSatisfaction,
                    MarketingInfo = provider.MarketingInformation,
                    Name = provider.ProviderName,
                    TradingName = provider.TradingName,
                    NationalProvider = provider.NationalApprenticeshipProvider,
                    UKPRN = ukprn,
                    Website = contactDetails?.ContactWebsiteAddress,
                    Phone = !string.IsNullOrWhiteSpace(contactDetails?.ContactTelephone1)
                        ? contactDetails?.ContactTelephone1
                        : contactDetails?.ContactTelephone2
                };
            }

            catch (Exception e)
            {
                throw new ProviderExportException(ukprn.ToString(), e);
            }
        }

        public List<Location> ApprenticeshipLocationsToLocations(IEnumerable<ApprenticeshipLocation> locations)
        {
            List<Location> DASLocations = new List<Location>();
            if (locations.Any())
            {
                foreach (var location in locations)
                {
                    if (location.Regions != null)
                    {
                        DASLocations.AddRange(RegionsToLocations(location.Regions));
                    }
                    else
                    {
                        DASLocations.Add(new Location
                        {
                            ID = location.TribalId,
                            Address = location.Address,
                            Name = location.Name,
                        });
                    }

                }
            }

            return DASLocations;
        }
        public List<Standard> ApprenticeshipsToStandards(IEnumerable<Apprenticeship> apprenticeships)
        {
            List<Standard> standards = new List<Standard>();
            foreach (var apprenticeship in apprenticeships)
            {

                standards.Add(new Standard
                {
                    StandardCode = apprenticeship.StandardCode.Value,
                    MarketingInfo = apprenticeship.MarketingInformation,
                    StandardInfoUrl = apprenticeship.Url,
                    Contact = new Contact
                    {
                        ContactUsUrl = apprenticeship.Url,
                        Email = apprenticeship.ContactEmail,
                        Phone = apprenticeship.ContactTelephone
                    },
                    Locations = CreateLocationRef(apprenticeship.ApprenticeshipLocations)
                });
            }
            return standards;
        }
        public List<Framework> ApprenticeshipsToFrameworks(IEnumerable<Apprenticeship> apprenticeships)
        {
            List<Framework> frameworks = new List<Framework>();

            foreach (var apprenticeship in apprenticeships)
            {
                frameworks.Add(new Framework
                {
                    FrameworkCode = apprenticeship.FrameworkCode.Value,
                    FrameworkInfoUrl = apprenticeship.Url,
                    MarketingInfo = HtmlHelper.StripHtmlTags(apprenticeship.MarketingInformation, true),
                    PathwayCode = apprenticeship.PathwayCode,
                    ProgType = apprenticeship.ProgType,
                    Contact = new Contact
                    {
                        ContactUsUrl = apprenticeship.ContactWebsite,
                        Email = apprenticeship.ContactEmail,
                        Phone = apprenticeship.ContactTelephone
                    },
                    Locations = CreateLocationRef(apprenticeship.ApprenticeshipLocations)
                });
            }
            return frameworks;
        }
        public List<Location> RegionsToLocations(string[] regionCodes)
        {
            List<Location> apprenticeshipLocations = new List<Location>();
            var regions = new SelectRegionModel().RegionItems.SelectMany(x => x.SubRegion.Where(y => regionCodes.Contains(y.Id)));
            foreach (var region in regions)
            {
                Location location = new Location
                {
                    ID = region.ApiLocationId,
                    Name = region.SubRegionName,
                    Address = new Address
                    {
                        Address1 = region.SubRegionName,
                        Latitude = region.Latitude,
                        Longitude = region.Longitude
                    },

                };
                apprenticeshipLocations.Add(location);
            }
            return apprenticeshipLocations;
        }
        public List<LocationRef> CreateLocationRef(IEnumerable<ApprenticeshipLocation> locations)
        {
            List<LocationRef> locationRefs = new List<LocationRef>();
            var subRegionItemModels = new SelectRegionModel().RegionItems.SelectMany(x => x.SubRegion);
            foreach(var location in locations)
            {
                if(location.Regions != null)
                {
                    foreach(var region in location.Regions)
                    {
                        locationRefs.Add(new LocationRef
                        {
                            ID = subRegionItemModels.Where(x => x.Id == region).Select(y => y.ApiLocationId.Value).FirstOrDefault(),
                            DeliveryModes = ConvertToApprenticeshipDeliveryModes(location),
                            Radius = location.Radius ?? 0
                        }) ;
                    }
                }
                else
                {
                    locationRefs.Add(new LocationRef
                    {
                        ID = location.LocationId,
                        DeliveryModes = ConvertToApprenticeshipDeliveryModes(location),
                        Radius = location.Radius ?? 0
                    });
                }

            }
            return locationRefs;

        }

        public List<string> ConvertToApprenticeshipDeliveryModes(ApprenticeshipLocation location)
        {
            var validDeliveryModes = location.DeliveryModes
                .Where(m => Enum.IsDefined(typeof(DeliveryMode), m))
                .Select(m => (DeliveryMode) m).ToList();

            if (location.DeliveryModes.Count > validDeliveryModes.Count)
            {
                var undefinedModes = string.Join(", ", location.DeliveryModes
                    .Where(m => !Enum.IsDefined(typeof(DeliveryMode), m)));

                var errorMessage = $"Could not map mode(s) \'{undefinedModes}\' to a matching {nameof(DeliveryMode)}";

                var eventProperties = new Dictionary<string, string>();
                var metrics = new Dictionary<string, double>();

                eventProperties.TryAdd("LocationId", location.LocationId.ToString());

                _telemetryClient.TrackException(new LocationExportException(location.Id.ToString()), eventProperties);
            }

            return validDeliveryModes
                .Select(m => m.ToDescription())
                .ToList();
        }

        internal int GenerateIntIdentifier()
        {
            return _intIdentifier++;
        }
    }
}
