using DA.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DA.Services
{

    public interface ITextService
    {

        //Task ReadTextFromImageStream(Stream photoStream);
        Task<string> ExtractLocalPrintedTextAsync(string photoUrl);
        Task<string> TextToSepach(Image model);


    }
    public class TextService : ITextService
    {
        private IConfiguration _configuration;
        private readonly string blobName;
        private readonly IStorageService _storageService;
        private readonly string speechSubKey;
        private readonly string speechUribase;
        private readonly string speechEndpoint;
        private readonly string computervisionEndpoint;
        private readonly string visionSubKey;
        private readonly string visionUribase;
        ComputerVisionClient _computerVision;

        public TextService(IConfiguration Configuration)
        {
            _configuration = Configuration;
            visionSubKey = _configuration["visionSubscriptionKey"];
            visionUribase = _configuration["visionUriBase"];
            speechSubKey = _configuration["speechsubsKey"];
            speechUribase = _configuration["speechUriBase"];
            speechEndpoint= _configuration["speechEndpoint"];
            computervisionEndpoint = _configuration["computerVisionEndpoint"];
            blobName = _configuration["blobName"];
            _storageService = new StorageService(Configuration);
            _computerVision = new ComputerVisionClient(
             new ApiKeyServiceClientCredentials(visionSubKey),
             new System.Net.Http.DelegatingHandler[] { });
            _computerVision.Endpoint = computervisionEndpoint;
        }
       
        public async Task<string> ExtractLocalPrintedTextAsync(string photoUrl)
        {
            try
            {
                // TextRecognitionMode.Printed or TextRecognitionMode.Handwritten
                var result = await _computerVision.BatchReadFileWithHttpMessagesAsync(photoUrl, TextRecognitionMode.Printed);
                
                var res = await GetTextAsync(_computerVision, result.Headers.OperationLocation);
                return res;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        private static async Task<string> GetTextAsync(ComputerVisionClient computerVision, string operationLocation)
        {
            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);


            ReadOperationResult result = await computerVision.GetReadOperationResultAsync(operationId);
            int i = 0;
            int maxRetries = 10;
            while ((result.Status == TextOperationStatusCodes.Running || result.Status == TextOperationStatusCodes.NotStarted) && i++ < maxRetries)
            {
                await Task.Delay(1000);
                result = await computerVision.GetReadOperationResultAsync(operationId);
            }
            
            var lines = result.RecognitionResults;
            var outputtext = new System.Text.StringBuilder();
            foreach (TextRecognitionResult recResult in lines)
            {
                foreach (Line line in recResult.Lines)
                {
                    outputtext.AppendLine(line.Text);
                }
            }

            if (!string.IsNullOrEmpty(outputtext.ToString()))
            {
                return await Task.FromResult(outputtext.ToString());
                // await TextToSepach(outputtext.ToString());
            }
            else
                return string.Empty;
        }

        /// <summary>
        /// Text to Speech   
        /// </summary>
        /// <returns></returns>
        public async Task<string> TextToSepach(Image model)
        {
            // Gets an access token
            string accessToken;
            Authentication auth = new Authentication(speechUribase, speechSubKey);

            try
            {
                accessToken = await auth.FetchTokenAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            string host = speechEndpoint;

            //Create SSML document.
            XDocument body = new XDocument(
                    new XElement("speak",
                        new XAttribute("version", "1.0"),
                        new XAttribute(XNamespace.Xml + "lang", "en-US"),
                        new XElement("voice",
                            new XAttribute(XNamespace.Xml + "lang", "en-US"),
                            new XAttribute(XNamespace.Xml + "gender", "Female"),
                            new XAttribute("name", "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)"),
                            model.outputText)));


            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Set the HTTP method
                    request.Method = HttpMethod.Post;
                    // Construct the URI
                    request.RequestUri = new Uri(host);
                    // Set the content type header
                    request.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/ssml+xml");
                    // Set additional header, such as Authorization and User-Agent
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    request.Headers.Add("Connection", "Keep-Alive");
                    // Update your resource name
                    request.Headers.Add("User-Agent", "mySpeech");

                    request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                    // Create a request
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        // Asynchronously read the response
                        using (var dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            var fileName = model.Name + ".mp3";
                            var blobAudioUrl = await _storageService.UploadToBlob(fileName, null, dataStream);
                            return await Task.FromResult(blobAudioUrl);
                          
                            // Play the audio file
                            //PlayMe("sample1.wav");
                        }
                    }
                }
            }
        }

        private static void PlayMe(string v)
        {

            try
            {
                using (var audioFile = new AudioFileReader(v))
                using (var outputDevice = new WaveOutEvent())
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                    while (outputDevice.PlaybackState == PlaybackState.Playing)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private async Task MakeAnalysisRequest(Image imageModel)
        {
            try
            {
                HttpClient client = new HttpClient();

                // Request headers.
                client.DefaultRequestHeaders.Add(
                    "Ocp-Apim-Subscription-Key", visionSubKey);

                // Request parameters. A third optional parameter is "details".
                // The Analyze Image method returns information about the following
                // visual features:
                // Categories:  categorizes image content according to a
                //              taxonomy defined in documentation.
                // Description: describes the image content with a complete
                //              sentence in supported languages.
                // Color:       determines the accent color, dominant color, 
                //              and whether an image is black & white.
                string requestParameters =
                    "visualFeatures=Categories,Description,Color&details=Celebrities,Landmarks";

                // Assemble the URI for the REST API method.
                string uri = visionUribase + "?" + requestParameters;

                HttpResponseMessage response;


                // Add the byte array as an octet stream to the request body.
                using (ByteArrayContent content = new ByteArrayContent(imageModel.Data))
                {
                    // This example uses the "application/octet-stream" content type.
                    // The other content types you can use are "application/json"
                    // and "multipart/form-data".
                    content.Headers.ContentType =
                        new MediaTypeHeaderValue("application/octet-stream");

                    // Asynchronously call the REST API method.
                    response = await client.PostAsync(uri, content);
                }

                // Asynchronously get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n\n{0}\n",
                    JToken.Parse(contentString).ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e.Message);
            }
        }

    }
}

