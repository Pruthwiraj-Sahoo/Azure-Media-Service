using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Media;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace AzureMediaServices.Repository
{

    public class HomeRepository
    {

        ILogger<HomeRepository> _logger;
        public HomeRepository(ILogger<HomeRepository> logger)
        {
            _logger = logger;
        }

        public static Dictionary<string, string> thumbnailAssetName = new Dictionary<string, string>();
        public static Dictionary<string, string> thumbnailContainerName = new Dictionary<string, string>();

        public async Task ListOfThumbnailsAsync()
        {
            try
            {
                thumbnailAssetName.Clear();
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

                var mediaServiceAccountId = MediaServicesAccountResource.CreateResourceIdentifier(
                    subscriptionId: options.AZURE_SUBSCRIPTION_ID.ToString(),
                    resourceGroupName: options.AZURE_RESOURCE_GROUP,
                    accountName: options.AZURE_MEDIA_SERVICES_ACCOUNT_NAME
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
                    Console.WriteLine("Asset Name : " + asset.Data.Name);
                }

                foreach (var asset in mediaServicesAccount.GetMediaAssets().GetAll())
                {
                    if (!asset.Data.Name.Contains("_analyzer"))
                    {
                        BlobServiceClient blobServiceClient = new BlobServiceClient(options.AZURE_CONNECTION_STRING);

                        // Get a reference to the container
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(asset.Data.Container);

                        // List all blobs in the container
                        foreach (BlobItem blobItem in containerClient.GetBlobs())
                        {

                            // Get a reference to the blob
                            BlobClient blobClient = containerClient.GetBlobClient(blobItem.Name);

                            // Check if the blob is a thumbnail (assuming they have a specific naming convention)
                            if (blobItem.Name.Contains(".jpg"))
                            {
                                // Get the thumbnail URL and print it to the console
                                Uri thumbnailUrl = blobClient.Uri;
                                //thumbnailURLList.Add(thumbnailUrl.ToString());

                                if (!thumbnailAssetName.ContainsKey(asset.Data.Container.ToString()))
                                {
                                    //thumbnailAssetName.Add(asset.Data.Container.ToString(), thumbnailUrl.ToString());
                                    thumbnailAssetName.Add(asset.Data.Name, thumbnailUrl.ToString());
                                    Console.WriteLine(thumbnailUrl.ToString());
                                }

                            }
                        }
                    }

                }
            }
            catch (Exception)
            {

                throw;
            }
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
            public string? AZURE_TENANT_ID { get; set; }

            [Required]
            public string? AZURE_CONNECTION_STRING { get; set; }

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