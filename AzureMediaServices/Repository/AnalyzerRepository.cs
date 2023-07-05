using Azure.Identity;
using Azure.ResourceManager.Media.Models;
using Azure.ResourceManager.Media;
using Azure.ResourceManager;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Azure;
using Microsoft.Azure.Management.Media;
using Microsoft.Rest.Azure.Authentication;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AzureMediaServices.Repository
{
    public class AnalyzerRepository
    {

        public async Task<string> AnalyzeAsync(string FileSourceUri, string videoname = "")
        {
            const string OutputFolder = "Output";
            const string CustomTransform = "AudioVideoAnalyzer";
            const string BaseSourceUri = "https://azuremediaservices-uswe.streaming.media.azure.net";

            try
            {
                var configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
               .Build();

                if (!Options.TryGetOptions(configuration, out var option))
                {
                    return "Configuration not found";
                }

                Environment.SetEnvironmentVariable("AZURE_CLIENT_ID", option.AZURE_CLIENT_ID);
                Environment.SetEnvironmentVariable("AZURE_CLIENT_SECRET", option.AZURE_CLIENT_SECRET);
                Environment.SetEnvironmentVariable("AZURE_TENANT_ID", option.AZURE_TENANT_ID);
                Environment.SetEnvironmentVariable("AZURE_USERNAME", "khushi.kumar@happiestminds.com");
                Environment.SetEnvironmentVariable("AZURE_PASSWORD", "azure@MEDIA");

                var mediaServicesResourceId = MediaServicesAccountResource.CreateResourceIdentifier(
               subscriptionId: option.AZURE_SUBSCRIPTION_ID.ToString(),
               resourceGroupName: option.AZURE_RESOURCE_GROUP,
               accountName: option.AZURE_MEDIA_SERVICES_ACCOUNT_NAME);

                var credential = new DefaultAzureCredential(includeInteractiveCredentials: true);
                var armClient = new ArmClient(new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeInteractiveBrowserCredential = true,
                    ExcludeAzureCliCredential = false,
                    ExcludeVisualStudioCredential = false
                }));
                var mediaServicesAccount = armClient.GetMediaServicesAccountResource(mediaServicesResourceId);

                string uniqueness = Guid.NewGuid().ToString()[..13];
                string jobName = $"job-{uniqueness}";
                string locatorName = $"locator-{uniqueness}";
                string outputAssetName = $"{videoname}_output_analyzer-{uniqueness}";

                var transform = await CreateTransformAsync(mediaServicesAccount, CustomTransform);

                var outputAsset = await CreateOutputAssetAsync(mediaServicesAccount, outputAssetName);

                UpdateContainerAccessPolicy(mediaServicesAccount, outputAssetName);

                //SetUploadInProgress(true);

                var job = await SubmitJobAsync(
                    transform,
                    jobName,
                    new MediaJobInputHttp
                    {
                        BaseUri = new Uri(BaseSourceUri),
                        Files = { FileSourceUri },
                        Label = "input1"
                    },
                    outputAsset);

                job = await WaitForJobToFinishAsync(job);

                if (job.Data.State == MediaJobState.Error)
                {
                    await CleanUpAsync(transform, job, outputAsset);
                    return "Error in Job Completion";
                }
                Directory.CreateDirectory(OutputFolder);

                //var streamingLocator = await CreateStreamingLocatorAsync(mediaServicesAccount, outputAsset.Data.Name, locatorName);

                //SetUploadInProgress(false);

                await DownloadResultsAsync(outputAsset, OutputFolder);

                return outputAssetName;

            }
            catch (Exception)
            {

                throw;
            }
            finally
            {

            }
           

           

           

        }
        static async Task<MediaTransformResource> CreateTransformAsync(MediaServicesAccountResource mediaServicesAccount, string transformName)
        {
            var transform = await mediaServicesAccount.GetMediaTransforms().CreateOrUpdateAsync(
                WaitUntil.Completed,
                transformName,
                new MediaTransformData
                {
                    Outputs =
                    {
                new MediaTransformOutput(
                    preset: new Azure.ResourceManager.Media.Models.VideoAnalyzerPreset()
                )
                    }
                });

            return transform.Value;
        }
        static async Task<MediaAssetResource> CreateOutputAssetAsync(MediaServicesAccountResource mediaServicesAccount, string assetName)
        {
            var asset = await mediaServicesAccount.GetMediaAssets().CreateOrUpdateAsync(
                WaitUntil.Completed,
                assetName,
                new MediaAssetData());
            return asset.Value;

        }

        static async void UpdateContainerAccessPolicy(MediaServicesAccountResource mediaServicesAccount, string assetName)
        {

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            if (!Options.TryGetOptions(configuration, out var option))
            {
                return;
            }

            string tenantId = option.AZURE_TENANT_ID;
            string clientId = option.AZURE_CLIENT_ID;
            string clientSecret = option.AZURE_CLIENT_SECRET;
            string subscriptionId = option.AZURE_SUBSCRIPTION_ID.ToString();
            string resourceGroup = option.AZURE_RESOURCE_GROUP;
            string accountName = option.AZURE_MEDIA_SERVICES_ACCOUNT_NAME;
            string storageConnectionString = option.AZURE_CONNECTION_STRING;
            var credentials = await ApplicationTokenProvider.LoginSilentAsync(tenantId, clientId, clientSecret);

            var client = new AzureMediaServicesClient(credentials)
            {
                SubscriptionId = subscriptionId
            };

            var asset = await client.Assets.GetAsync(resourceGroup, accountName, assetName);
            var blobServiceClient = new BlobServiceClient(storageConnectionString);
            var containerName = asset.Container.ToLowerInvariant();

            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.SetAccessPolicyAsync(PublicAccessType.BlobContainer);
        }

        static async Task<MediaJobResource> SubmitJobAsync(MediaTransformResource transform, string jobName, MediaJobInputBasicProperties jobInput, MediaAssetResource outputAsset)
        {
            Console.WriteLine("Creating a Job...");
            var job = await transform.GetMediaJobs().CreateOrUpdateAsync(
                WaitUntil.Completed,
                jobName,
                new MediaJobData
                {
                    Input = jobInput,
                    Outputs =
                    {
                new MediaJobOutputAsset(outputAsset.Data.Name)
                    }
                });

            return job.Value;
        }

        static async Task<MediaJobResource> WaitForJobToFinishAsync(MediaJobResource job)
        {
            var sleepInterval = TimeSpan.FromSeconds(30);
            MediaJobState? state;

            do
            {
                job = await job.GetAsync();
                state = job.Data.State.GetValueOrDefault();

                Console.WriteLine($"Job is '{state}'.");
                for (int i = 0; i < job.Data.Outputs.Count; i++)
                {
                    var output = job.Data.Outputs[i];
                    Console.Write($"\tJobOutput[{i}] is '{output.State}'.");
                    if (output.State == MediaJobState.Processing)
                    {
                        Console.Write($"  Progress: '{output.Progress}'.");
                    }

                    Console.WriteLine();
                }

                if (state != MediaJobState.Finished && state != MediaJobState.Error && state != MediaJobState.Canceled)
                {
                    var progressOutput = job.Data.Outputs.FirstOrDefault(output => output.Label == "output1");
                    if (progressOutput != null)
                    {
                        //EncodingProgress = progressOutput.Progress ?? 0;
                    }
                    await Task.Delay(sleepInterval);
                }
            }
            while (state != MediaJobState.Finished && state != MediaJobState.Error && state != MediaJobState.Canceled);

            return job;
        }

        static async Task<StreamingLocatorResource> CreateStreamingLocatorAsync(MediaServicesAccountResource mediaServicesAccount, string assetName, string locatorName)
        {

            var locator = await mediaServicesAccount.GetStreamingLocators().CreateOrUpdateAsync(
                WaitUntil.Completed,
                locatorName,
                new StreamingLocatorData
                {
                    AssetName = assetName,
                    StreamingPolicyName = "Predefined_ClearStreamingOnly"
                });

            return locator.Value;
        }

        public void SetUploadInProgress(bool inProgress)
        {
            //_isVideoUploadInProgress = inProgress;
        }
        async static Task DownloadResultsAsync(MediaAssetResource asset, string outputFolderName)
        {
            var assetContainerSas = asset.GetStorageContainerUrisAsync(new MediaAssetStorageContainerSasContent
            {
                Permissions = MediaAssetContainerPermission.Read,
                ExpireOn = DateTime.UtcNow.AddHours(1)
            });

            var containerSasUrl = await assetContainerSas.FirstAsync();

            var container = new BlobContainerClient(containerSasUrl);

            string directory = Path.Combine(outputFolderName, asset.Data.Name);
            Directory.CreateDirectory(directory);

            Console.WriteLine("Downloading results to {0}.", directory);

            await foreach (var blob in container.GetBlobsAsync())
            {
                var blobClient = container.GetBlobClient(blob.Name);
                string filename = Path.Combine(directory, blob.Name);
                await blobClient.DownloadToAsync(filename);
            }

            Console.WriteLine("Download complete.");
        }
        static async Task CleanUpAsync(MediaTransformResource transform, MediaJobResource job, MediaAssetResource outputAsset)
        {
            await job.DeleteAsync(WaitUntil.Completed);
            await transform.DeleteAsync(WaitUntil.Completed);
            await outputAsset.DeleteAsync(WaitUntil.Completed);
        }



        /// <summary>
        /// Internal class to get value from appsetting.json or from CMD
        /// </summary>
        internal class Options
        {
            [Required]
            public Guid? AZURE_SUBSCRIPTION_ID { get; set; }

            [Required]
            public string? AZURE_TENANT_ID { get; set; }

            [Required]
            public string? AZURE_RESOURCE_GROUP { get; set; }

            [Required]
            public string? AZURE_MEDIA_SERVICES_ACCOUNT_NAME { get; set; }

            [Required]
            public string? AZURE_CLIENT_ID { get; set; }

            [Required]
            public string? AZURE_CLIENT_SECRET { get; set; }

            [Required]
            public string? AZURE_CONNECTION_STRING { get; set; }


            static public bool TryGetOptions(IConfiguration configuration, [NotNullWhen(returnValue: true)] out Options? option)
            {
                try
                {
                    option = configuration.Get<Options>() ?? throw new Exception("No configuration found. Configuration can be set in appsettings.json or using command line option.");
                    Validator.ValidateObject(option, new ValidationContext(option), true);
                    return true;
                }
                catch (Exception ex)
                {
                    option = null;
                    Console.WriteLine(ex.Message);
                    return false;
                }
            }
        }

    }
}
