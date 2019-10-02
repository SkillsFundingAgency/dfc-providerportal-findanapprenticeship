using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Helper;
using Dfc.Providerportal.FindAnApprenticeship.Interfaces.Models.Regions;
using Dfc.Providerportal.FindAnApprenticeship.Models;
using Dfc.Providerportal.FindAnApprenticeship.Models.Enums;
using Dfc.Providerportal.FindAnApprenticeship.Models.Providers;
using Dfc.Providerportal.FindAnApprenticeship.Models.DAS;
using System;
using System.Collections.Generic;
using System.Linq;
using Dfc.ProviderPortal.Packages;

namespace Dfc.Providerportal.FindAnApprenticeship.Helper
{
    public class DASHelper : IDASHelper
    {
        private readonly IReferenceDataServiceWrapper _referenceDataServiceWrapper;
        private int _intIdentifier { get; set; }
        public DASHelper(IReferenceDataServiceWrapper referenceDataServiceWrapper)
        {
            Throw.IfNull(referenceDataServiceWrapper, nameof(referenceDataServiceWrapper));
            _referenceDataServiceWrapper = referenceDataServiceWrapper;
            _intIdentifier = 300000;
        }
        public DASProvider CreateDASProviderFromProvider(Provider provider)
        {
            
            var contactDetails = provider.ProviderContact.FirstOrDefault();
            var feChoice = _referenceDataServiceWrapper.GetFeChoicesByUKPRN(provider.UnitedKingdomProviderReferenceNumber).FirstOrDefault();
            return new DASProvider
            {
                Id = provider.ProviderId ?? GenerateIntIdentifier(),
                Email = contactDetails != null ? contactDetails.ContactEmail : string.Empty,
                EmployerSatisfaction = feChoice.EmployerSatisfaction ?? 0.0,
                LearnerSatisfaction = feChoice.LearnerSatisfaction ?? 0.0,
                MarketingInfo = provider.MarketingInformation,
                Name = provider.ProviderName,
                TradingName = provider.TradingName,
                NationalProvider = provider.NationalApprenticeshipProvider,
                UKPRN = int.Parse(provider.UnitedKingdomProviderReferenceNumber),
                Website = contactDetails != null ? contactDetails.ContactWebsiteAddress : string.Empty,
                Phone = string.IsNullOrWhiteSpace(contactDetails.ContactTelephone1) ? contactDetails.ContactTelephone1 : contactDetails.ContactTelephone2

            };

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
                            ID = location.TribalId ?? (int?) null,
                            GuidID = location.Id,
                            Address = location.Address ?? null,
                            Email = location.Address != null ? location.Address.Email : string.Empty,
                            Name = location.Name,
                            Phone = location.Phone,
                            Website = location.Address != null ? location.Address.Website : string.Empty
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
                    MarketingInfo = apprenticeship.MarketingInformation,
                    PathwayCode = apprenticeship.PathwayCode ?? (int?)null,
                    ProgType = apprenticeship.ProgType ?? (int?)null,
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
        internal List<LocationRef> CreateLocationRef(IEnumerable<ApprenticeshipLocation> locations)
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
                            DeliveryModes = ConvertToApprenticeshipDeliveryModes(location.DeliveryModes),
                            Radius = location.Radius ?? 0
                        }) ;
                    }
                }
                else
                {
                    locationRefs.Add(new LocationRef
                    {
                        ID = location.LocationId,
                        DeliveryModes = ConvertToApprenticeshipDeliveryModes(location.DeliveryModes),
                        Radius = location.Radius ?? 0
                    });
                }

            }
            return locationRefs;

        }
        internal List<string> ConvertToApprenticeshipDeliveryModes(List<int> courseDirectoryModes)
        {
            List<string> DASList = new List<string>();
            foreach (var mode in courseDirectoryModes)
            {
                switch(mode)
                {
                    case (int)ApprenticeShipDeliveryLocation.DayRelease:
                        {
                            DASList.Add(DASDeliveryModes.DayRelease.ToDescription());
                            break;
                        }
                    case (int)ApprenticeShipDeliveryLocation.BlockRelease:
                        {
                            DASList.Add(DASDeliveryModes.BlockRelease.ToDescription());
                            break;
                        }
                    case (int)ApprenticeShipDeliveryLocation.EmployerAddress:
                        {
                            DASList.Add(DASDeliveryModes.EmployerBased.ToDescription());
                            break;
                        }

                }
            }
            if(courseDirectoryModes.Count == 0)
            {
                DASList.Add(DASDeliveryModes.EmployerBased.ToDescription());
            }
            DASList.Sort();
            return DASList;
        }
        internal int GenerateIntIdentifier()
        {
            return _intIdentifier++;
        }
    }
}
