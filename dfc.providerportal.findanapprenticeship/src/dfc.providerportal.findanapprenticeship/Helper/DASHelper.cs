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
using Microsoft.ApplicationInsights.DataContracts;

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
        public DasProvider CreateDasProviderFromProvider(Provider provider)
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

                return new DasProvider
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

        public List<DasLocation> ApprenticeshipLocationsToLocations(IEnumerable<ApprenticeshipLocation> locations)
        {
            List<DasLocation> DASLocations = new List<DasLocation>();
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
                        DASLocations.Add(new DasLocation
                        {
                            ID = location.TribalId,
                            Address = new DasAddress()
                            {
                                Address1 = location.Address?.Address1,
                                Address2 = location.Address?.Address2,
                                County = location.Address?.County,
                                Latitude = location.Address?.Latitude,
                                Longitude = location.Address?.Longitude,
                                Postcode = location.Address?.Postcode,
                            },
                            Name = location.Name,
                            Email = location.Address?.Email,
                            Website = location.Address?.Website,
                            Phone = location.Phone ?? location.Address?.Phone,
                        });
                    }

                }
            }

            return DASLocations;
        }
        public List<DasStandard> ApprenticeshipsToStandards(IEnumerable<Apprenticeship> apprenticeships)
        {
            List<DasStandard> standards = new List<DasStandard>();
            foreach (var apprenticeship in apprenticeships)
            {
                standards.Add(new DasStandard
                {
                    StandardCode = apprenticeship.StandardCode.Value,
                    MarketingInfo = apprenticeship.MarketingInformation,
                    StandardInfoUrl = apprenticeship.Url,
                    Contact = new DasContact
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
        public List<DasFramework> ApprenticeshipsToFrameworks(IEnumerable<Apprenticeship> apprenticeships)
        {
            List<DasFramework> frameworks = new List<DasFramework>();

            foreach (var apprenticeship in apprenticeships)
            {
                frameworks.Add(new DasFramework
                {
                    FrameworkCode = apprenticeship.FrameworkCode.Value,
                    FrameworkInfoUrl = apprenticeship.Url,
                    MarketingInfo = HtmlHelper.StripHtmlTags(apprenticeship.MarketingInformation, true),
                    PathwayCode = apprenticeship.PathwayCode,
                    ProgType = apprenticeship.ProgType,
                    Contact = new DasContact
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
        public List<DasLocation> RegionsToLocations(string[] regionCodes)
        {
            List<DasLocation> apprenticeshipLocations = new List<DasLocation>();
            var regions = new SelectRegionModel().RegionItems.SelectMany(x => x.SubRegion.Where(y => regionCodes.Contains(y.Id)));
            foreach (var region in regions)
            {
                if (!region.ApiLocationId.HasValue) continue;
                var dasLocation = new DasLocation
                {
                    ID = region.ApiLocationId.Value,
                    Name = region.SubRegionName,
                    Address = new DasAddress()
                    {
                        Address1 = region.SubRegionName,
                        Latitude = region.Latitude,
                        Longitude = region.Longitude
                    }
                };
                apprenticeshipLocations.Add(dasLocation);
            }
            return apprenticeshipLocations;
        }
        public List<DasLocationRef> CreateLocationRef(IEnumerable<ApprenticeshipLocation> locations)
        {
            List<DasLocationRef> locationRefs = new List<DasLocationRef>();
            var subRegionItemModels = new SelectRegionModel().RegionItems.SelectMany(x => x.SubRegion);
            foreach(var location in locations)
            {
                if(location.Regions != null)
                {
                    foreach(var region in location.Regions)
                    {
                        locationRefs.Add(new DasLocationRef
                        {
                            ID = subRegionItemModels.Where(x => x.Id == region).Select(y => y.ApiLocationId.Value).FirstOrDefault(),
                            DeliveryModes = ConvertToApprenticeshipDeliveryModes(location),
                            Radius = location.Radius ?? 0
                        }) ;
                    }
                }
                else
                {
                    locationRefs.Add(new DasLocationRef
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
                var evt = new ExceptionTelemetry();
                var undefinedModes = string.Join(", ", location.DeliveryModes
                    .Where(m => !Enum.IsDefined(typeof(DeliveryMode), m)));

                var errorMessage = $"Could not map mode(s) \'{undefinedModes}\' to a matching {nameof(DeliveryMode)}";

                evt.Properties.TryAdd("LocationId", $"{location.LocationId}");

                _telemetryClient.TrackException(
                    new LocationExportException(
                        location.Id.ToString(), 
                        new InvalidCastException(errorMessage)));
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
