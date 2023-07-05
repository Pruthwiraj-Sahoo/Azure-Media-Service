using AzureMediaServices.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage.Table;



namespace AzureMediaServices.Controllers
{
    public class FormController : Controller
    {
        public static string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=videoindexstorage;AccountKey=msmQs5Ks9ZXdl8tUleuJhJHgmWCpPteSVEhfhO3Eeyjjwjw4nn59qg8D7rUPteMMdMYVqcgLXfiO+AStAK2wXQ==;EndpointSuffix=core.windows.net";

        [HttpGet]
        public IActionResult MetaDataForm()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> MetaDataForm(metadata Metadata)
        {
            var url = Request.Path;
            var indexerEntity = new IndexerEntity(Metadata.Id, Metadata);
            TableOperation insertOperation = TableOperation.Insert(indexerEntity);

            try
            {

                var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(ConnectionString);
                var tableClient = storageAccount.CreateCloudTableClient();
                var table = tableClient.GetTableReference("MetaDataTable");
                await table.ExecuteAsync(insertOperation);
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            //_metaDataFormModel.OnPost(id, metadata);
            return View("~/Views/Asset/AssetDisplay.cshtml");
        }

        [Route("metadataDisplay/{id}")]
        public async Task<IActionResult> ShowMetadataDisplay(string id)
        {
            var storageAccount = Microsoft.WindowsAzure.Storage.CloudStorageAccount.Parse(ConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("MetaDataTable");
            await table.CreateIfNotExistsAsync();
            TableContinuationToken token = null;
            TableQuery<IndexerEntity> indexerQuery = new TableQuery<IndexerEntity>().Where(
            TableQuery.GenerateFilterCondition("Id", QueryComparisons.Equal, id));
            do
            {
                TableQuerySegment<IndexerEntity> segment = await table.ExecuteQuerySegmentedAsync(indexerQuery, token);
                token = segment.ContinuationToken;




                foreach (IndexerEntity entity in segment.Results)
                {



                    ViewBag.Metadata = entity;



                }
            }
            while (token != null);



            return View("~/Views/Asset/MetadataDisplay.cshtml");
        }


        public class IndexerEntity : TableEntity
        {

            public IndexerEntity(string id, metadata metadata)
            {
                this.PartitionKey = "VideoMetaData";
                this.RowKey = id;
                this.Id = id;
                this.Language = metadata.Language;
                this.ContentCategory = metadata.ContentCategory;
                this.MainTitle = metadata.MainTitle;
                this.ChannelName = metadata.ChannelName;
                this.Genre = metadata.Genre;
                this.SubtitleLanguage = metadata.SubtitleLanguage;
                this.DubbingLanguage = metadata.DubbingLanguage;
                this.EpisodeNo = metadata.EpisodeNo;
                this.Synopsis = metadata.Synopsis;
                this.ReleaseDate = (DateTime)metadata.ReleaseDate;
                this.AlternateTitle = metadata.AlternateTitle;
                this.AudioType = metadata.AudioType;
                this.VideoStandard = metadata.VideoStandard;
                this.ContentType = metadata.ContentType;
                this.FileName = metadata.FileName;
                this.Comments = metadata.Comments;
                this.ArchivalCategory = metadata.ArchivalCategory;
                this.ArchiveLocation = metadata.ArchiveLocation;
                this.CensorCertificate = metadata.CensorCertificate;
                this.SubCategory = metadata.SubCategory;
                this.PartNo = metadata.PartNo;
                this.SegmentCount = metadata.SegmentCount;
                this.ProductionHouse = metadata.ProductionHouse;
            }

            public IndexerEntity() { }
            public string Id { get; set; }
            public string Language { get; set; }
            public string ContentCategory { get; set; }
            public string MainTitle { get; set; }
            public string ChannelName { get; set; }
            public string Genre { get; set; }
            public string SubtitleLanguage { get; set; }
            public string DubbingLanguage { get; set; }
            public string EpisodeNo { get; set; }
            public string Synopsis { get; set; }
            public DateTime ReleaseDate { get; set; }
            public string AlternateTitle { get; set; }
            public string AudioType { get; set; }
            public string VideoStandard { get; set; }
            public string ContentType { get; set; }
            public string FileName { get; set; }
            public string Comments { get; set; }
            public string ArchivalCategory { get; set; }
            public string ArchiveLocation { get; set; }
            public string CensorCertificate { get; set; }
            public string SubCategory { get; set; }
            public string PartNo { get; set; }
            public string SegmentCount { get; set; }
            public string ProductionHouse { get; set; }



        }
    }
}