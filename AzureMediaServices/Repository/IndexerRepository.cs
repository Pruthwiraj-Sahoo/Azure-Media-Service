using Azure.Core;
using Azure.Identity;
using AzureMediaServices.Res;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.VisualStudio.Services.DelegatedAuthorization;

using System.Collections.Generic;
using System.Globalization;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using System.Windows.Forms;
using System.Xml;




namespace AzureMediaServices.Repository
{



    public class IndexerRepository
    {

        private const string ApiVersion = "2022-08-01";
        private const string AzureResourceManager = "https://management.azure.com";
        //private const string AzureResourceManager = "https://management.azure.com/providers/Microsoft.Authorization/operations?api-version=2016-09-01";
        private const string SubscriptionId = "d83d7f2f-70ba-43e6-8b05-c4b31a70a8e7";
        private const string ResourceGroup = "AzureMediaServices";
        private const string AccountName = "MediaServicesIndexer";
        //private const string VideoUrl = "https://videoindexstorage.blob.core.windows.net/asset-2660d581-ef29-4410-8ac6-72b32fd4f124/sample-mp4-file-small.mp4";
        private const string ApiUrl = "https://api.videoindexer.ai";
        private const string Location = "westus";
        private const string AccountId = "86e4bddc-c653-43a0-8cd8-a0a55ed1ed88";
        private const string ApiKey = "e40d120040cd498d92b9d94b7e26203f";
        public static string ClientID = "351d1395-d41d-4151-a31c-ffc8ec689694";
        public static string ClientSecret = "VYH8Q~IIG4rH16hiPodfT2BCUyV02EuOjGQfhaE_";
        public static string TenantID = "77428205-87ff-4048-a645-91b337240228";
        public static string Endpoint = "https://westus.api.videoindexer.ai";
        public static string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=videoindexstorage;AccountKey=msmQs5Ks9ZXdl8tUleuJhJHgmWCpPteSVEhfhO3Eeyjjwjw4nn59qg8D7rUPteMMdMYVqcgLXfiO+AStAK2wXQ==;EndpointSuffix=core.windows.net";
        public static string AccessToken { get; set; }

        public static IDictionary<string, string> AssetNameK_VideoIdV = new Dictionary<string, string>();



        public static List<string> VideoIdList = new List<string>();



        public static string insights;
        public static string player;




        public string? indexedOrNot = "";
        public async Task IndexAsync(string FileSourceUri, string id)
        {

            var videoIndexerResourceProviderClient = await VideoIndexerResourceProviderClient.BuildVideoIndexerResourceProviderClient();



            // Get account details
            var account = await videoIndexerResourceProviderClient.GetAccount();
            var accountLocation = account.Location;
            var accountId = account.Properties.Id;



            // Get account level access token for Azure Video Indexer 
            var accountAccessToken = await videoIndexerResourceProviderClient.GetAccessToken(ArmAccessTokenPermission.Owner, ArmAccessTokenScope.Account);



            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;



            // Create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);



            var list = GetAllVideos(id);
            string videoId = null;
            if (list.Count > 0)
            {
                videoId = list[0];
                indexedOrNot = list[1];
            }
            else
            {
                indexedOrNot = "notIndexed";
            }




            if (indexedOrNot == "notIndexed")
            {
                // Upload a video
                videoId = await UploadVideo(accountId, accountLocation, accountAccessToken, ApiUrl, client, FileSourceUri);



                var indexerEntity = new IndexerEntity(id, id, videoId);
                TableOperation insertOperation = TableOperation.Insert(indexerEntity);



                try
                {



                    //var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(ConnectionString);
                    //var tableClient = storageAccount.CreateCloudTableClient();
                    //var table = tableClient.GetTableReference("AssetNameAndVideoId");
                    var acc = new CloudStorageAccount(
                                    new StorageCredentials("videoindexstorage", "msmQs5Ks9ZXdl8tUleuJhJHgmWCpPteSVEhfhO3Eeyjjwjw4nn59qg8D7rUPteMMdMYVqcgLXfiO+AStAK2wXQ=="), true);
                    var tableClient = acc.CreateCloudTableClient();
                    var table = tableClient.GetTableReference("AssetNameAndVideoId");
                    table.Execute(insertOperation);
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }





                //Console.WriteLine(insertOperation.ToString());
                //Console.WriteLine(table);



                // Wait for the video index to finish
                await WaitForIndex(accountId, accountLocation, accountAccessToken, ApiUrl, client, videoId, id);




            }
            else
            {
                indexedOrNot = "Indexed";
                Console.WriteLine("Already indexed");
            }
            // Get video level access token for Azure Video Indexer 
            //var videoAccessToken = await videoIndexerResourceProviderClient.GetAccessToken(ArmAccessTokenPermission.Contributor, ArmAccessTokenScope.Video, videoId);
            // Search for the video
            //await GetVideo(accountId, accountLocation, videoAccessToken, ApiUrl, client, videoId);



            // Get insights widget url
            //await GetInsightsWidgetUrl(accountId, accountLocation, videoAccessToken, ApiUrl, client, videoId);



            // Get player widget url
            //await GetPlayerWidgetUrl(accountId, accountLocation, videoAccessToken, ApiUrl, client, videoId);



            //Console.WriteLine("\nPress Enter to exit...");
            //String line = Console.ReadLine();
            //if (line == "enter")
            //{
            //    System.Environment.Exit(0);
            //}



        }
        public List<string> GetAllVideos(string id)
        {
            var videoId = "";
            List<string> list = new List<string>();



            // var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(ConnectionString);
            // var tableClient = storageAccount.CreateCloudTableClient();
            // var table = tableClient.GetTableReference("AssetNameAndVideoId");
            // table.CreateIfNotExistsAsync();
            //Console.WriteLine(table.ToString());



            // TableContinuationToken token = null;
            // TableQuery<IndexerEntity> indexerQuery = new TableQuery<IndexerEntity>().Where(
            // TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "VideoIndexer"));
            var acc = new CloudStorageAccount(
                                     new StorageCredentials("videoindexstorage", "msmQs5Ks9ZXdl8tUleuJhJHgmWCpPteSVEhfhO3Eeyjjwjw4nn59qg8D7rUPteMMdMYVqcgLXfiO+AStAK2wXQ=="), true);
            var tableClient = acc.CreateCloudTableClient();
            var table = tableClient.GetTableReference("AssetNameAndVideoId");
            TableContinuationToken token = null;
            var entities = new List<IndexerEntity>();
            do
            {
                var queryResult = table.ExecuteQuerySegmented(new TableQuery<IndexerEntity>(), token);
                entities.AddRange(queryResult.Results);
                token = queryResult.ContinuationToken;



                foreach (IndexerEntity entity in queryResult.Results)
                {



                    Console.WriteLine(entity.AssetName);
                    if (entity.AssetName != id)
                    {
                        videoId = null;
                    }
                    else
                    {
                        indexedOrNot = "Indexed";
                        videoId = entity.VideoId;
                        list.Add(videoId);
                        list.Add(indexedOrNot.ToString());
                    }




                }
            } while (token != null);



            //do
            //{
            //    TableQuerySegment<IndexerEntity> segment = await table.ExecuteQuerySegmentedAsync(indexerQuery, token);
            //    token = segment.ContinuationToken;

            //    foreach(IndexerEntity entity in segment.Results)
            //    {

            //        Console.WriteLine(entity.AssetName);
            //        if (entity.AssetName != id)
            //        {
            //           videoId = null;
            //        }
            //        else
            //        {
            //            indexedOrNot = "Indexed";
            //            videoId =  entity.VideoId;
            //            list.Add(videoId);
            //            list.Add(indexedOrNot.ToString());
            //        }




            //    }
            //}
            //while(token != null);
            return list;
        }
        public class IndexerEntity : TableEntity
        {



            public IndexerEntity(string id, string AssetName, string VideoId)
            {
                this.PartitionKey = "VideoIndexer";
                this.RowKey = id;
                this.AssetName = AssetName;
                this.VideoId = VideoId;
            }



            public IndexerEntity() { }



            public string AssetName { get; set; }
            public string VideoId { get; set; }

        }
        public async Task GetPlayerAndInsights(string id)
        {
            var videoIndexerResourceProviderClient = await VideoIndexerResourceProviderClient.BuildVideoIndexerResourceProviderClient();



            // Get account details
            var account = await videoIndexerResourceProviderClient.GetAccount();
            var accountLocation = account.Location;
            var accountId = account.Properties.Id;

            // Get account level access token for Azure Video Indexer 
            //var accountAccessToken = await videoIndexerResourceProviderClient.GetAccessToken(ArmAccessTokenPermission.Contributor, ArmAccessTokenScope.Account);



            System.Net.ServicePointManager.SecurityProtocol |= System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;



            // Create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);
            var list = GetAllVideos(id);
            string videoId = null;
            if (list.Count > 0)
            {
                videoId = list[0];
                indexedOrNot = list[1];
            }



            var videoAccessToken = await videoIndexerResourceProviderClient.GetAccessToken(ArmAccessTokenPermission.Contributor, ArmAccessTokenScope.Video, videoId);



            // Search for the video
            //await GetVideo(accountId, accountLocation, videoAccessToken, ApiUrl, client, videoId);



            // Get insights widget url
            insights = await GetInsightsWidgetUrl(accountId, accountLocation, videoAccessToken, ApiUrl, client, videoId);



            // Get player widget url
            player = await GetPlayerWidgetUrl(accountId, accountLocation, videoAccessToken, ApiUrl, client, videoId);
        }
        private static async Task<string> UploadVideo(string accountId, string accountLocation, string acountAccessToken, string apiUrl, HttpClient client, string videoUrl)
        {
            Console.WriteLine($"Video for account {accountId} is starting to upload.");
            var content = new MultipartFormDataContent();



            try
            {
                // Get the video from URL
                var queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
                    {"accessToken", acountAccessToken},
                    {"name", "video sample"},
                    {"description", "video_description"},
                    {"privacy", "private"},
                    {"partition", "partition"},
                    {"videoUrl", videoUrl},
                });
                var uploadRequestResult = await client.PostAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos?{queryParams}", content);
                VerifyStatus(uploadRequestResult, System.Net.HttpStatusCode.OK);
                var uploadResult = await uploadRequestResult.Content.ReadAsStringAsync();



                // Get the video ID from the upload result
                var videoId = JsonSerializer.Deserialize<Video>(uploadResult).Id;
                Console.WriteLine($"\nVideo ID {videoId} was uploaded successfully");
                return videoId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
        private static async Task WaitForIndex(string accountId, string accountLocation, string acountAccessToken, string apiUrl, HttpClient client, string videoId, string id)
        {
            Console.WriteLine($"\nWaiting for video {videoId} to finish indexing.");
            string queryParams;
            while (true)
            {
                queryParams = CreateQueryString(
                    new Dictionary<string, string>()
                    {
                            {"accessToken", acountAccessToken},
                            {"language", "English"},
                    });



                var videoGetIndexRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/{videoId}/Index?{queryParams}");



                VerifyStatus(videoGetIndexRequestResult, System.Net.HttpStatusCode.OK);
                var videoGetIndexResult = await videoGetIndexRequestResult.Content.ReadAsStringAsync();
                string processingState = JsonSerializer.Deserialize<Video>(videoGetIndexResult).State;
                //string processingProgress = JsonSerializer.Deserialize<Video>(videoGetIndexResult).videos[0].processingProgress;



                // If job is finished
                if (processingState == ProcessingState.Processed.ToString())
                {
                    VideoIdList.Add(videoId);
                    AssetNameK_VideoIdV.Add(id, videoId);
                    Console.WriteLine($"The video index has completed. Here is the full JSON of the index for video ID {videoId}: \n{videoGetIndexResult}");
                    return;
                }
                else if (processingState == ProcessingState.Failed.ToString())
                {
                    Console.WriteLine($"\nThe video index failed for video ID {videoId}.");
                    throw new Exception(videoGetIndexResult);
                }



                // Job hasn't finished
                Console.WriteLine($"\nThe video index state is {processingState}");
                await Task.Delay(10000);
            }
        }



        private static async Task GetVideo(string accountId, string accountLocation, string videoAccessToken, string apiUrl, HttpClient client, string videoId)
        {
            Console.WriteLine($"\nSearching videos in account {AccountName} for video ID {videoId}.");
            var queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
                        {"accessToken", videoAccessToken},
                        {"id", videoId},
                });



            try
            {
                var searchRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/Search?{queryParams}");



                VerifyStatus(searchRequestResult, System.Net.HttpStatusCode.OK);
                var searchResult = await searchRequestResult.Content.ReadAsStringAsync();
                Console.WriteLine($"Here are the search results: \n{searchResult}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }



        private static async Task<string> GetInsightsWidgetUrl(string accountId, string accountLocation, string videoAccessToken, string apiUrl, HttpClient client, string videoId)
        {
            Console.WriteLine($"\nGetting the insights widget URL for video {videoId}");
            var queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
                    {"accessToken", videoAccessToken},
                    {"widgetType", "Keywords"},
                    {"allowEdit", "true"},
                });
            var insightsWidgetRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/{videoId}/InsightsWidget?{queryParams}");
            var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
            try
            {




                VerifyStatus(insightsWidgetRequestResult, System.Net.HttpStatusCode.MovedPermanently);

                Console.WriteLine($"Got the insights widget URL: \n{insightsWidgetLink}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return insightsWidgetLink.ToString();
        }



        private static async Task<string> GetPlayerWidgetUrl(string accountId, string accountLocation, string videoAccessToken, string apiUrl, HttpClient client, string videoId)
        {
            Console.WriteLine($"\nGetting the player widget URL for video {videoId}");
            var queryParams = CreateQueryString(
                new Dictionary<string, string>()
                {
                    {"accessToken", videoAccessToken},
                });
            var playerWidgetRequestResult = await client.GetAsync($"{apiUrl}/{accountLocation}/Accounts/{accountId}/Videos/{videoId}/PlayerWidget?{queryParams}");



            var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
            try
            {

                VerifyStatus(playerWidgetRequestResult, System.Net.HttpStatusCode.MovedPermanently);
                Console.WriteLine($"Got the player widget URL: \n{playerWidgetLink}");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            return playerWidgetLink.ToString();
        }



        static string CreateQueryString(IDictionary<string, string> parameters)
        {
            var queryParameters = HttpUtility.ParseQueryString(string.Empty);
            foreach (var parameter in parameters)
            {
                queryParameters[parameter.Key] = parameter.Value;
            }



            return queryParameters.ToString();
        }



        public class VideoIndexerResourceProviderClient
        {
            private readonly string armAccessToken;

            async public static Task<VideoIndexerResourceProviderClient> BuildVideoIndexerResourceProviderClient()
            {
                //var handler = new HttpClientHandler();
                //handler.AllowAutoRedirect = false;
                //var client = new HttpClient(handler);
                //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);

                //var tokenRequestContext = new TokenRequestContext(new[] { $"{AzureResourceManager}/.default" });
                //var tokenRequestResult = await new DefaultAzureCredential().GetTokenAsync(tokenRequestContext).ConfigureAwait(false);

                ////using (tokenRequestResult)
                //    //{
                //    return new VideoIndexerResourceProviderClient(tokenRequestResult.Token);
                ////}




                //var accountAccessTokenRequestResult = client.GetAsync($"{ApiUrl}/auth/{Location}/Accounts/{AccountId}/AccessToken?allowEdit=true").Result;
                //var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");
                //client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
                //return new VideoIndexerResourceProviderClient(accountAccessToken);




                //ClientCredential cc = new ClientCredential(ClientID, ClientSecret);
                //var context = new AuthenticationContext("https://login.microsoftonline.com/" + TenantID);
                //var result = context.AcquireTokenAsync("https://management.core.windows.net/", cc);
                //if (result == null)
                //{
                //    throw new InvalidOperationException("Failed to obtain the Access token");
                //}
                //var AccessToken = result.Result.AccessToken;

                //ClientCredential clientCredential = new ClientCredential("351d1395-d41d-4151-a31c-ffc8ec689694","VYH8Q~IIG4rH16hiPodfT2BCUyV02EuOjGQfhaE_" );
                //ServiceClientCredentials v= await ApplicationTokenProvider.LoginSilentAsync("77428205-87ff-4048-a645-91b337240228", clientCredential, ActiveDirectoryServiceSettings.Azure);



                var tenantId = "77428205-87ff-4048-a645-91b337240228";
                var clientId = "351d1395-d41d-4151-a31c-ffc8ec689694";
                var secret = "VYH8Q~IIG4rH16hiPodfT2BCUyV02EuOjGQfhaE_";
                var resourceUrl = "https://management.azure.com/";
                var requestUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/token";

                // in real world application, please use Typed HttpClient from ASP.NET Core DI
                var httpClient = new HttpClient();

                var dict = new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", clientId },
                    { "client_secret", secret },
                    { "resource", resourceUrl }
                };

                var requestBody = new FormUrlEncodedContent(dict);
                var response = await httpClient.PostAsync(requestUrl, requestBody);

                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                var aadToken = JsonSerializer.Deserialize<AzureADToken>(responseContent);

                Console.WriteLine(aadToken?.AccessToken);



                return new VideoIndexerResourceProviderClient(aadToken?.AccessToken);






            }
            public VideoIndexerResourceProviderClient(string armAaccessToken)
            {
                this.armAccessToken = armAaccessToken;
            }




            /// <summary>
            /// Generates an access token. Calls the generateAccessToken API  (https://github.com/Azure/azure-rest-api-specs/blob/main/specification/vi/resource-manager/Microsoft.VideoIndexer/stable/2022-08-01/vi.json#:~:text=%22/subscriptions/%7BsubscriptionId%7D/resourceGroups/%7BresourceGroupName%7D/providers/Microsoft.VideoIndexer/accounts/%7BaccountName%7D/generateAccessToken%22%3A%20%7B)
            /// </summary>
            /// <param name="permission"> The permission for the access token</param>
            /// <param name="scope"> The scope of the access token </param>
            /// <param name="videoId"> if the scope is video, this is the video Id </param>
            /// <param name="projectId"> If the scope is project, this is the project Id </param>
            /// <returns> The access token, otherwise throws an exception</returns>
            public async Task<string> GetAccessToken(ArmAccessTokenPermission permission, ArmAccessTokenScope scope, string videoId = null, string projectId = null)
            {
                var accessTokenRequest = new AccessTokenRequest
                {
                    PermissionType = permission,
                    Scope = scope,
                    VideoId = videoId,
                    ProjectId = projectId
                };



                Console.WriteLine($"\nGetting access token: {JsonSerializer.Serialize(accessTokenRequest)}");



                // Set the generateAccessToken (from video indexer) http request content
                try
                {
                    var jsonRequestBody = JsonSerializer.Serialize(accessTokenRequest);
                    var httpContent = new StringContent(jsonRequestBody, System.Text.Encoding.UTF8, "application/json");



                    // Set request uri
                    var requestUri = $"{AzureResourceManager}/subscriptions/{SubscriptionId}/resourcegroups/{ResourceGroup}/providers/Microsoft.VideoIndexer/accounts/{AccountName}/generateAccessToken?api-version={ApiVersion}";
                    var client = new HttpClient(new HttpClientHandler());
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", armAccessToken);



                    var result = await client.PostAsync(requestUri, httpContent);



                    VerifyStatus(result, System.Net.HttpStatusCode.OK);
                    var jsonResponseBody = await result.Content.ReadAsStringAsync();
                    Console.WriteLine($"Got access token: {scope} {videoId}, {permission}");
                    return JsonSerializer.Deserialize<GenerateAccessTokenResponse>(jsonResponseBody).AccessToken;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }



            /// <summary>
            /// Gets an account. Calls the getAccount API (https://github.com/Azure/azure-rest-api-specs/blob/main/specification/vi/resource-manager/Microsoft.VideoIndexer/stable/2022-08-01/vi.json#:~:text=%22/subscriptions/%7BsubscriptionId%7D/resourceGroups/%7BresourceGroupName%7D/providers/Microsoft.VideoIndexer/accounts/%7BaccountName%7D%22%3A%20%7B)
            /// </summary>
            /// <returns> The Account, otherwise throws an exception</returns>
            public async Task<Account> GetAccount()
            {
                Console.WriteLine($"Getting account {AccountName}.");
                Account account;
                try
                {
                    // Set request uri
                    var requestUri = $"{AzureResourceManager}/subscriptions/{SubscriptionId}/resourcegroups/{ResourceGroup}/providers/Microsoft.VideoIndexer/accounts/{AccountName}?api-version={ApiVersion}";
                    var client = new HttpClient(new HttpClientHandler());
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", armAccessToken);



                    var result = await client.GetAsync(requestUri);
                    //var accessToken = (await result.Content.ReadAsStringAsync()).Trim('"');



                    VerifyStatus(result, System.Net.HttpStatusCode.OK);
                    var jsonResponseBody = await result.Content.ReadAsStringAsync();
                    account = JsonSerializer.Deserialize<Account>(jsonResponseBody);
                    VerifyValidAccount(account);
                    Console.WriteLine($"The account ID is {account.Properties.Id}");
                    Console.WriteLine($"The account location is {account.Location}");
                    return account;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    throw;
                }
            }



            private static void VerifyValidAccount(Account account)
            {
                if (string.IsNullOrWhiteSpace(account.Location) || account.Properties == null || string.IsNullOrWhiteSpace(account.Properties.Id))
                {
                    Console.WriteLine($"{nameof(AccountName)} {AccountName} not found. Check {nameof(SubscriptionId)}, {nameof(ResourceGroup)}, {nameof(AccountName)} ar valid.");
                    throw new Exception($"Account {AccountName} not found.");
                }
            }
        }



        public class AccessTokenRequest
        {
            [JsonPropertyName("permissionType")]
            public ArmAccessTokenPermission PermissionType { get; set; }



            [JsonPropertyName("scope")]
            public ArmAccessTokenScope Scope { get; set; }



            [JsonPropertyName("projectId")]
            public string ProjectId { get; set; }



            [JsonPropertyName("videoId")]
            public string VideoId { get; set; }
        }



        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ArmAccessTokenPermission
        {
            Reader,
            Contributor,
            MyAccessAdministrator,
            Owner,
        }



        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ArmAccessTokenScope
        {
            Account,
            Project,
            Video
        }



        public class GenerateAccessTokenResponse
        {
            [JsonPropertyName("accessToken")]
            public string AccessToken { get; set; }
        }



        public class AccountProperties
        {
            [JsonPropertyName("accountId")]
            public string Id { get; set; }
        }



        public class Account
        {
            [JsonPropertyName("properties")]
            public AccountProperties Properties { get; set; }



            [JsonPropertyName("location")]
            public string Location { get; set; }
        }



        public class Video
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }



            [JsonPropertyName("state")]
            public string State { get; set; }
        }



        public enum ProcessingState
        {
            Uploaded,
            Processing,
            Processed,
            Failed
        }



        public static void VerifyStatus(HttpResponseMessage response, System.Net.HttpStatusCode excpectedStatusCode)
        {
            if (response.StatusCode != excpectedStatusCode)
            {
                throw new Exception(response.ToString());
            }
        }
    }
}