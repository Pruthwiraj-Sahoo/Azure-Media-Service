
using AzureMediaServices.Models;
using AzureMediaServices.Repository;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AzureMediaServices.Controllers
{
    public class AssetController : Controller
    {
        private readonly ILogger<AssetController> _logger;
        AssetRepository assetRepository;
        EncodeRepository encodeRepository;
        IndexerRepository indexerRepository;
        public AssetController(ILogger<AssetController> logger, AssetRepository asset, EncodeRepository encodeRepos, IndexerRepository indexerRepository)
        {
            assetRepository = asset;
            _logger = logger;
            encodeRepository = encodeRepos;
            this.indexerRepository = indexerRepository;
        }

        [Route("{id}")]
        public IActionResult AssetDisplay(string id)
        {
            assetRepository.ShowAssetAsync(id);



            indexerRepository.GetAllVideos(id);
            var indexerornot = indexerRepository.indexedOrNot;
            // Sending value  through viewbag
            ViewBag.IndexedOrNot = indexerornot;
            //ViewBag.Indexerornot = true;



            return View();
        }

        public IActionResult GetJsonData()
        {
            var id = this.Request.GetEncodedUrl();
            var CurrentDirectoryPath = Directory.GetCurrentDirectory();

            string DownloadPath = Path.Combine(CurrentDirectoryPath, "MetaData", "metadata.json");
            string jsonFilePath = DownloadPath;
            string jsonData = System.IO.File.ReadAllText(jsonFilePath);
            return Json(jsonData);

        }


        [Route("metadataform/{id}")]
        public ActionResult MetaDataForm()
        {
            return View();
        }
    }
}
