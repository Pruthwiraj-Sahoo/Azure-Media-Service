using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using Microsoft.Rest.Azure.Authentication;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using AzureMediaServices.Repository;

namespace AzureMediaServices.Controllers
{
    public class UploadController : Controller
    {
        private readonly IConfiguration _configuration;
        EncodeRepository _encodeRepository;
        IndexerRepository _indexerRepository;
        AnalyzerRepository _analyzer;
        ReportRepository _report;
        private bool _isVideoUploaded = false;

        public UploadController(IConfiguration configuration, EncodeRepository encodeRepository, IndexerRepository indexerRepository, AnalyzerRepository analyzer, ReportRepository report)
        {
            _configuration = configuration;
            _encodeRepository = encodeRepository;
            _indexerRepository = indexerRepository;
            _analyzer = analyzer;
            _report=report;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UploadVideo()
        {
            return View();
        }

        [HttpPost]
        [RequestFormLimits(MultipartBodyLengthLimit = 104857600)]
        public async Task<IActionResult> UploadVideo(IFormFile file,String? FileName)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    string tenantId = _configuration["AZURE_TENANT_ID"];
                    string clientId = _configuration["AZURE_CLIENT_ID"];
                    string clientSecret = _configuration["AZURE_CLIENT_SECRET"];
                    string subscriptionId = _configuration["AZURE_SUBSCRIPTION_ID"];
                    string resourceGroup = _configuration["AZURE_RESOURCE_GROUP"];
                    string accountName = _configuration["AZURE_MEDIA_SERVICES_ACCOUNT_NAME"];
                    string storageConnectionString = _configuration["AZURE_CONNECTION_STRING"];
                    var credentials = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, clientSecret);

                    var client = new AzureMediaServicesClient(credentials)
                    {
                        SubscriptionId = subscriptionId
                    };
                    if(FileName!="" || FileName != null)
                    {
                        FileName = FileName.Replace(' ', '_');
                    }

                    string uniqueguid = Guid.NewGuid().ToString()[..13];


                    string assetName = FileName + "_primary_input-"+ uniqueguid;
                    var blobName = "";
                    var containerName = "";
                    var asset = await client.Assets.CreateOrUpdateAsync(resourceGroup, accountName, assetName, new Asset());
                    
                 
                    using (var stream = file.OpenReadStream())
                    {
                        var blobServiceClient = new BlobServiceClient(storageConnectionString);

                        containerName = asset.Container.ToLowerInvariant();
                        Console.WriteLine(containerName);

                        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                        if (!await containerClient.ExistsAsync())
                        {
                            await containerClient.CreateAsync(PublicAccessType.BlobContainer);
                        }
                        else
                        {
                            await containerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);
                            
                        }

                        blobName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        await containerClient.UploadBlobAsync(blobName, stream);
                    }
                    string uploadVideoUri = "https://videoindexstorage.blob.core.windows.net/" + containerName+ "/" + blobName;

                    var rtVal = _analyzer.AnalyzeAsync(uploadVideoUri, FileName, uniqueguid);

                    await _encodeRepository.EncodeAsync(uploadVideoUri, FileName, uniqueguid);
                   

                    //if (rtMsg.Contains("_analyzer"))
                    //{
                    //    await _report.ReadOnline(rtMsg);
                    //}

                    //await _uploadRepository.UploadAsync(uploadVideoUri);
                    //await _indexerRepository.IndexAsync(uploadVideoUri);
                    Console.WriteLine("Upload URI: " + uploadVideoUri);
                    
                    bool encodedMessage = _encodeRepository.IsVideoUploadInProgress;
                    ViewBag.EncodedMessage = encodedMessage;

                    return View();
                }
                catch (ErrorResponseException ere)
                {
                    ModelState.AddModelError("", ere.ToString());
                    return View();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.ToString());
                    return View();
                }

            }
            ModelState.AddModelError("", "Please select the video file");
            return View();
        }

        [HttpPost]

        public IActionResult SetUploadInProgress(bool inProgress)
        {
            _encodeRepository.SetUploadInProgress(inProgress);
            return Ok();
        }
        public ActionResult UploadSuccess()
        {
            string successMessage = TempData["Message"] as string;
            
            ViewBag.SuccessMessage = successMessage;

            return View();
        }

        public IActionResult GetEncodingProgress()
        {
            var progress = EncodeRepository.EncodingProgress.ToString();
            return Content(progress);
        }
    }
 }