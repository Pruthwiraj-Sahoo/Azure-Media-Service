using Azure.Identity;
using Azure.ResourceManager.Media;
using Azure.ResourceManager;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Azure.Management.Media.Models;
using System.Text.Json;

namespace AzureMediaServices.Repository
{
    /// <summary>
    /// This Repository Used to read contentmoderation.json File
    /// Provieds the Avg. Value on specific condition
    /// 1.adultScore
    /// 2.fragments
    /// 3.timestamp with higher adultScore
    /// </summary>
    public class ReportRepository
    {
        public double adultScore;
        public (double,string) fragmnet;
        public (double, string) timestamp;

        #region Read data Offline
        public void ReadOffline()
        {
            try
            {

            }
            catch (Exception)
            {

                throw;
            }
        }
        #endregion

        #region Read data from Azure
        public async Task ReadOnline(string assertName)
        {
            try
            {
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
                var assets = mediaServicesAccount.GetMediaAssets().GetAll();

                foreach (var item in assets)
                {
                    if (item.Data.Name==assertName)
                    {
                        Console.WriteLine(item.Data.Name);
                        BlobServiceClient blobServiceClient = new BlobServiceClient(options.AZURE_CONNECTION_STRING);

                        // Get a reference to the container
                        BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(item.Data.Container);

                        // List all blobs in the container
                        foreach (BlobItem blobItem in containerClient.GetBlobs())
                        {
                            if (blobItem.Name.Contains("contentmoderation.json"))
                            {
                                //var url = "https://videoindexstorage.blob.core.windows.net/"+item.Data.Container+ "/contentmoderation.json";
                                //using FileStream stream = File.OpenRead(url);
                                //var v= await JsonSerializer.DeserializeAsync<string>(stream);

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
        #endregion

        #region Internal Class

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

        #endregion
    }
}
