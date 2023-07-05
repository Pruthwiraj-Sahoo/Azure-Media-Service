using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Azure.Management.Media;
using Microsoft.Azure.Management.Media.Models;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AzureMediaServices.Repository
{
    public class AssetRepository : PageModel
    {
        ILogger<AssetRepository> _logger;
        public AssetRepository(ILogger<AssetRepository> logger)
        {
            _logger = logger;
        }
        public static string videoUrl;
        public static string videoName;
        public static string metaDataUrl;
        public static int flag = 0;
        public static string AssetName;
        public void ShowAssetAsync(string assetName)
        {
            AssetName = assetName;
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            if (!Options.TryGetOptions(configuration, out var options))
            {
                return;
            }

            Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", options.AZURE_CLIENT_ID);
            Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", options.AZURE_CLIENT_SECRET);
            Environment.SetEnvironmentVariable("AZURE_TENANT_ID", options.AZURE_TENANT_ID);
            Environment.SetEnvironmentVariable("AZURE_USERNAME", "khushi.kumar@happiestminds.com");
            Environment.SetEnvironmentVariable("AZURE_PASSWORD", "azure@MEDIA");


            var credentials = ApplicationTokenProvider.LoginSilentAsync(options.AZURE_TENANT_ID, options.AZURE_CLIENT_ID, options.AZURE_CLIENT_SECRET).Result;

            var client = new AzureMediaServicesClient(credentials)
            {
                SubscriptionId = options.AZURE_SUBSCRIPTION_ID.ToString()
            };

            var assetContainerSas = client.Assets.ListContainerSas(options.AZURE_RESOURCE_GROUP, options.AZURE_MEDIA_SERVICES_ACCOUNT_NAME, assetName, permissions: AssetContainerPermission.Read);

            Uri containerUri = new Uri(assetContainerSas.AssetContainerSasUrls.First());

            string containerName = containerUri.Segments.Last().TrimEnd('/');


            StreamingLocator streamingLocator = client.StreamingLocators.List(options.AZURE_RESOURCE_GROUP, options.AZURE_MEDIA_SERVICES_ACCOUNT_NAME).FirstOrDefault(s1 => s1.AssetName == assetName);

            if (streamingLocator.StreamingPolicyName == PredefinedStreamingPolicy.DownloadAndClearStreaming || streamingLocator.StreamingPolicyName == PredefinedStreamingPolicy.ClearStreamingOnly)
            {
                var streamingUrls = client.StreamingLocators.ListPaths(options.AZURE_RESOURCE_GROUP, options.AZURE_MEDIA_SERVICES_ACCOUNT_NAME, streamingLocator.Name);


                foreach (var streamingUrl in streamingUrls.StreamingPaths)
                {
                    if(streamingUrl.StreamingProtocol == StreamingPolicyStreamingProtocol.SmoothStreaming)
                    {
                        videoUrl = "https://azuremediaservices-uswe.streaming.media.azure.net" + streamingUrl.Paths[0];
                    }
                }

            }
            videoName = assetName;
            BlobServiceClient blobServiceClient = new BlobServiceClient(options.AZURE_CONNECTION_STRING);

            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            foreach (BlobItem blobItem in containerClient.GetBlobs())
            {

                BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);


                // Check if the blob is a json (assuming they have a specific naming convention)
                if (blobItem.Name.EndsWith("metadata.json"))
                {
                    //Download file with .json extension
                    //blobClient.DownloadTo("C:\\Users\\manasa.marigoli\\Downloads\\AzureMediaServices V5\\AzureMediaServices\\AzureMediaServices\\MetaData\\metadata.json");

                    var CurrentDirectoryPath = Directory.GetCurrentDirectory();

                    string DownloadPath = Path.Combine(CurrentDirectoryPath, "MetaData", "metadata.json");

                    //Download file with .json extension
                    blobClient.DownloadTo(DownloadPath);

                    break;
                }
            }
        }
        public static void ShowMetaData()
        {
            flag = 1;
        }

        internal class Options
        {
            [Required]
            public Guid? AZURE_SUBSCRIPTION_ID { get; set; }

            [Required]
            public string? AZURE_RESOURCE_GROUP { get; set; }

            [Required]
            public string? AZURE_MEDIA_SERVICES_ACCOUNT_NAME { get; set; }

            [Required]
            public string? AZURE_CONNECTION_STRING { get; set; }

            [Required]
            public string? AZURE_TENANT_ID { get; set; }

            [Required]
            public string? AZURE_CLIENT_ID { get; set; }

            [Required]
            public string? AZURE_CLIENT_SECRET { get; set; }

            static public bool TryGetOptions(IConfiguration configuration, [NotNullWhen(returnValue: true)] out Options? options)
            {
                try
                {
                    options = configuration.Get<Options>() ?? throw new Exception("No configuration found. Configuration can be set in appsettings.json or using command line options.");
                    Validator.ValidateObject(options, new ValidationContext(options), true);
                    return true;
                }
                catch (Exception ex)
                {
                    options = null;
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }

        }
    }
}
