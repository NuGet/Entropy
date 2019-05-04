using System.Linq;
using System.Threading.Tasks;
using NuGet.Versioning;
using NuGet.Protocol.Registration;
using NuGet.Protocol.Catalog;
using System.Net;

namespace TestIngestionPerf
{
    public class RegistrationChecker : IEndpointChecker
    {
        private readonly IRegistrationClient _registrationClient;
        private readonly string _endpoint;

        public RegistrationChecker(
            IRegistrationClient registrationClient,
            string endpoint)
        {
            _registrationClient = registrationClient;
            _endpoint = endpoint.TrimEnd('/');
        }

        public string Name => $"Registration: {_endpoint}";

        public async Task<bool> DoesPackageExistAsync(string id, NuGetVersion version)
        {
            var indexUrl = RegistrationUrlBuilder.GetIndexUrl(_endpoint, id);
            var index = await _registrationClient.GetIndexOrNullAsync(indexUrl);
            if (index == null)
            {
                return false;
            }

            var matchingPageItem = index
                .Items
                .FirstOrDefault(x => NuGetVersion.Parse(x.Lower) <= version && version <= NuGetVersion.Parse(x.Upper));
            if (matchingPageItem == null)
            {
                return false;
            }

            var items = matchingPageItem.Items;
            if (items == null)
            {
                try
                {
                    var page = await _registrationClient.GetPageAsync(matchingPageItem.Url);
                    items = page.Items;
                }
                catch (SimpleHttpClientException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    return false;
                }
            }

            var matchingItem = items
                .FirstOrDefault(x => NuGetVersion.Parse(x.CatalogEntry.Version) == version);
            if (matchingItem == null)
            {
                return false;
            }

            return true;
        }
    }
}
