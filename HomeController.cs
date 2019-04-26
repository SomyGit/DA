using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DA.Models;
using DA.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System.Drawing;

using System.IO;
using System.Threading;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DA
{


    public class HomeController : Controller
    {
        private IConfiguration _configuration;

        private string storageConnectionString;
        private string blobName;
        private readonly IStorageService _storageService;
        private readonly ITextService _textService;
        // GET: /<controller>/




        public HomeController(IConfiguration Configuration)
        {
            _configuration = Configuration;
            _storageService = new StorageService(Configuration);
            _textService = new TextService(Configuration);
        }




        public ActionResult Index()
        {
            return View();

        }

        [HttpPost]
        public async Task<ActionResult> Post(IFormFile photo)
        {
            var model = new Image();
            var blobPath = string.Empty;
            var text = string.Empty;
            if (photo == null || photo.Length <= 0)
            {
                return View("./Error");
            }


            using (var stream = photo.OpenReadStream())
            {
                // Upload file to BLOB Storage
                var blobUri = await _storageService.UploadToBlob(photo.FileName, null, stream);

                model.desc = photo.ContentDisposition;
                model.FileName = photo.FileName;
                var index = model.FileName.IndexOf(".");
                model.Name = model.FileName.Substring(0, index);
                model.Url = blobUri;
                model.PhotoStream = stream;
                return await Read(model);


            }


        }

        private async Task<ActionResult> Read(Image model)
        {

            var res = string.Empty;

            if (model.PhotoStream != null)
                res = await _textService.ExtractLocalPrintedTextAsync(model.Url);
            else
                throw new Exception("Stream is null");

            if (!String.IsNullOrEmpty(res))
            {

                model.outputText = res;
                return await SayIt(model);
            }
            else
                return View("./ Error");


        }

        private async Task<ActionResult> SayIt(Image model)
        {
            if (!String.IsNullOrEmpty(model.outputText))
            {
                var audioUrl = await _textService.TextToSepach(model);
                model.audioSrc = audioUrl;
                return View("Image", model);
            }
            else
                return View("./ Error");

        }

    }
}

    