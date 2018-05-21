using DimsumBot.Model.Shared.Wechat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WechatConnector.Options;

namespace WechatConnector.Client
{
    public class WechatClient : IWechatClient
    {
        private readonly WechatOptions _options;
        private WechatAccessToken _accessToken;

        private ILogger<WechatClient> _logger;

        public WechatClient(IOptions<WechatOptions> options, ILogger<WechatClient> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task PostMessage(WechatMessage msg)
        {
            var token = await GetOrRefreshToken();
            using (var client = new HttpClient())
            {
                var uriBuilder = new UriBuilder(_options.CustomerEndpoint);
                uriBuilder.Query = $"access_token={token.AccessToken}";

                var json = JsonConvert.SerializeObject(msg);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(uriBuilder.Uri, content);
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<string> UploadMedia(string type, string filePath)
        {
            try
            {
                //This weird gymnastics was done as an effort to more closely match the boundary string generated in postman
                var boundaryString = $"--------------------------{Guid.NewGuid().ToString().Replace("-", "")}";
                if (boundaryString.Length > 52)
                    boundaryString = boundaryString.Substring(0, 52);

                //file loads correctly
                var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read);

                //token is valid
                var token = await GetOrRefreshToken();

                //Getting this to work before using a singleton httpclient
                var handler = new HttpClientHandler();
                handler.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                using (var client = new HttpClient(handler))
                using (var form = new MultipartFormDataContent(boundaryString))
                {
                    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DimsumBot", "0.5"));
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                    client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue();
                    client.DefaultRequestHeaders.CacheControl.NoCache = true;

                    var uriBuilder = new UriBuilder(_options.MediaUploadEndpoint);
                    uriBuilder.Query = $"access_token={token.AccessToken}&type={type}";

                    //Quotes were added to better match the output of Postman
                    var mediaContent = new StreamContent(fileStream);
                    mediaContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                    mediaContent.Headers.ContentDisposition.Name = "\"media\"";
                    mediaContent.Headers.ContentDisposition.FileName = $"\"{Path.GetFileName(filePath)}\"";
                    mediaContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                    form.Add(mediaContent);

                    var response = await client.PostAsync(uriBuilder.Uri, form);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync();
                    var mediaResponse = JsonConvert.DeserializeObject<WechatMediaUploadResponse>(json);

                    if (!string.IsNullOrWhiteSpace(mediaResponse.ErrorMessage))
                    {
                        _logger.LogError("Could not upload media {0}: | Error Code: {1} | Message: {2}", filePath, mediaResponse.ErrorCode, mediaResponse.ErrorMessage);
                        return string.Empty;
                    }

                    return mediaResponse.MediaId ?? string.Empty;
                }
            }catch(Exception e)
            {
                _logger.LogError(e, "Could not upload media {0}", filePath);
                return string.Empty;
            }
        }

        private async Task<WechatAccessToken> GetOrRefreshToken()
        {
            if (_accessToken != null)
            {
                var age = DateTime.UtcNow - _accessToken.IssueTime;
                if (age.TotalSeconds < _accessToken.ExpiresIn)
                    return _accessToken;
            }

            using (var client = new HttpClient())
            {
                var uriBuilder = new UriBuilder(_options.TokenURI);
                uriBuilder.Query = $"grant_type=client_credential&appid={_options.AppId}&secret={_options.AppSecret}";

                var response = await client.GetAsync(uriBuilder.Uri);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                _accessToken = JsonConvert.DeserializeObject<WechatAccessToken>(json);
                _accessToken.IssueTime = DateTime.UtcNow;

                return _accessToken;
            }
        }
    }
}
