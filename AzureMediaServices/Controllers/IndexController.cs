using Azure.Core;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using AzureMediaServices.Repository;
using Microsoft.AspNetCore.Http.Extensions;
using Azure.ResourceManager.Media;
using Azure.Identity;
using Azure.ResourceManager;



namespace AzureMediaServices.Controllers
{
    public class IndexController : Controller
    {
        private readonly IConfiguration _configuration;

        IndexerRepository _indexerRepository;
        private bool _isVideoUploaded = false;



        public IndexController(IConfiguration configuration, IndexerRepository indexerRepository)
        {
            _configuration = configuration;
            _indexerRepository = indexerRepository;
        }

        //[HttpPost]
        //public ActionResult Indexed()
        //{
        //    ViewBag.Message = String.Format("Already Indexed!");
        //    return View();
        //}
 
        

        public IActionResult StartIndexing(string id)
        {
            Console.WriteLine($"Indexing {id}");
            string storageConnectionString = _configuration["AZURE_CONNECTION_STRING"];
            var mediaServiceAccountId = MediaServicesAccountResource.CreateResourceIdentifier(
               subscriptionId: _configuration["AZURE_SUBSCRIPTION_ID"].ToString(),
               resourceGroupName: _configuration["AZURE_RESOURCE_GROUP"],
               accountName: _configuration["AZURE_MEDIA_SERVICES_ACCOUNT_NAME"]
               );
            var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
            var armClient = new ArmClient(new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeInteractiveBrowserCredential = true,
                ExcludeAzureCliCredential = false,
                ExcludeVisualStudioCredential = false
            }));

            var mediaServicesAccount = armClient.GetMediaServicesAccountResource(mediaServiceAccountId);

            foreach (var asset in mediaServicesAccount.GetMediaAssets().GetAll())
            {
                if (asset.Data.Name == id)
                {
                    var containerName = asset.Data.Container;
                    Console.WriteLine(containerName);
                    BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnectionString);

                    BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);


                    foreach (BlobItem blobItem in containerClient.GetBlobs())
                    {
                        BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);


                        // Check if the blob is a json (assuming they have a specific naming convention)
                        if (blobItem.Name.EndsWith(".mp4"))
                        {
                            //Download file with .json extension
                            string blobName = blobItem.Name;
                            string uploadVideoUri = "https://videoindexstorage.blob.core.windows.net/" + containerName + "/" + blobName;


                            Console.WriteLine($"URI : {uploadVideoUri}");
                            _indexerRepository.IndexAsync(uploadVideoUri, id);
                            break;
                        }
                    }
                }
            }
            _indexerRepository.GetAllVideos(id);
            var indexOrNot = _indexerRepository.indexedOrNot;
            ViewBag.IndexOrNot = indexOrNot;

      return Ok();
        }


        [Route("index/{id}")]
        public IActionResult InsightsDisplay(string id)
        {
            _indexerRepository.GetPlayerAndInsights(id);
            return View("~/Views/Asset/InsightsDisplay.cshtml", id);
        }
    }
}