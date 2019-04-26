using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DA.Services
{

    public interface IStorageService
    {
        Task<string> UploadToBlob(string filename, byte[] imageBuffer = null, Stream stream = null);
    }

    public class StorageService : IStorageService
    {
        private IConfiguration _configuration;
        private string storageConnectionString;
        private string _containerName;
        CloudStorageAccount storageAccount;
        CloudBlobContainer cloudBlobContainer;
        public StorageService(IConfiguration Configuration)
        {
            _configuration = Configuration;
            storageConnectionString = _configuration["storageconnectionstring"];
            _containerName = _configuration["containername"];
            storageAccount = null;
            cloudBlobContainer = null;

        }

        public async Task<string> UploadToBlob(string filename, byte[] imageBuffer = null, Stream stream = null)
        {
            string blobPath = string.Empty;

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // Create a container called 'uploadblob' and append a GUID value to it to make the name unique. 
                    cloudBlobContainer = cloudBlobClient.GetContainerReference(_containerName);// + Guid.NewGuid().ToString());

                    // Get a reference to the blob address, then upload the file to the blob.
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(filename);


                    // OPTION A: use imageBuffer (converted from memory stream)
                    //    await cloudBlockBlob.UploadFromByteArrayAsync(imageBuffer, 0, imageBuffer.Length);

                    // OPTION B: pass in memory stream directly
                    if (stream != null)
                        await cloudBlockBlob.UploadFromStreamAsync(stream);

                    return cloudBlockBlob.Uri.AbsoluteUri;
                }
                catch (StorageException ex)
                {
                    return ex.Message;
                }
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
