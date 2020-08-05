using Azure.Storage.Blobs;

namespace Dfc.Providerportal.FindAnApprenticeship.Storage
{
    public interface IBlobStorageClient
    {
        BlobClient GetBlobClient(string blobName);
    }
}