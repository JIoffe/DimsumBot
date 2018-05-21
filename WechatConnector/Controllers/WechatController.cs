using System;
using System.Threading.Tasks;
using DimsumBot.Core.Devices;
using DimsumBot.Core.Devices.Model;
using DimsumBot.DirectLineClient.Client;
using DimsumBot.Model.Shared.Wechat;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WechatConnector.Client;
using WechatConnector.Cryptography;
using WechatConnector.Extensions;
using WechatConnector.Serialization;

namespace WechatConnector.Controllers
{
    [Produces("text/plain")]
    [Route("wechat")]
    public class WechatController : Controller
    {
        private static readonly string SUCCESS = "success";
        private static readonly string WECHAT_TOKEN = "wechat_token";
        private static readonly string WECHAT_SUBCHANNEL = "wechat";
        private static readonly string DIRECTLINE_CHANNEL = "directline";

        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ISHA1Encryptor _encryptor;
        private readonly IWechatClient _connector;
        private readonly IDirectLineConnector _directLineConnector;
        private readonly IDeviceRegistrar _deviceRegistrar;
        private readonly ILogger _logger;

        public WechatController(IHostingEnvironment hostingEnvironment, ISHA1Encryptor encryptor, IWechatClient connector, IDirectLineConnector directLineConnector, IDeviceRegistrar deviceRegistrar, ILogger<WechatController> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _encryptor = encryptor;
            _connector = connector;
            _directLineConnector = directLineConnector;
            _deviceRegistrar = deviceRegistrar;
            _logger = logger;
        }

        [Route("testmedia")]
        [HttpGet]
        public async Task<IActionResult> TestImageUpload()
        {
            var imgPath = $"{_hostingEnvironment.WebRootPath}/imgs/star.png";
            var mediaId = await _connector.UploadMedia(WechatMessageTypes.IMAGE, imgPath);

            return Ok(mediaId);
        }
        /// <summary>
        /// WeChat sends a GET request to verify that the bot code is configured correctly
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Got WeChat Verification request");

            string signature = Request.Query["signature"];
            string timestamp = Request.Query["timestamp"];
            string nonce = Request.Query["nonce"];
            string echostr = Request.Query["echostr"];

            if(signature == null || timestamp == null || nonce == null || echostr == null)
            {
                _logger.LogWarning("Received bad verification request - expected query params were null");
                return BadRequest("Missing query params");
            }

            var verificationElements = new string[] { WECHAT_TOKEN, timestamp, nonce };
            Array.Sort(verificationElements);
            var verificationString = string.Join("", verificationElements);

            verificationString = _encryptor.Encrypt(verificationString);
            if (signature == verificationString)
            {
                _logger.LogInformation("WeChat Verified!");
                return Ok(echostr);
            }
            else
            {
                _logger.LogWarning("Couldn't Verify: Received [{0}] but expected [{1}]", verificationString, signature);
                return Ok(string.Empty);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            //var r = Request;
            //Immediately returns a "success" OK response back to We Chat.
            //Although this one is directly calling the DirectLine, we could
            //route this through an auxiliary connector as well.

            var xml = await this.ReadRequestContentAsync();
            _logger.LogInformation("Incoming Wechat Message: {0}", xml);

            Parallel.Invoke(async () =>
            {
                try
                {
                    var incomingMessage = XmlUtils.Deserialize<WechatMessage>(xml);

                    var deviceRegistration = await _deviceRegistrar.GetDeviceRegistrationAsync(incomingMessage.FromUserName, DIRECTLINE_CHANNEL, WECHAT_SUBCHANNEL);
                    if(deviceRegistration == null)
                    {
                        //Not registered - create a new one!
                        var conversation = await _directLineConnector.StartConversationAsync();
                        deviceRegistration = new DeviceRegistration
                        {
                            ChannelId = DIRECTLINE_CHANNEL,
                            ConversationId = conversation.ConversationId,
                            UserId = incomingMessage.FromUserName
                        };

                        await _directLineConnector.JoinConversationAsync(conversation.ConversationId, incomingMessage.FromUserName, WECHAT_SUBCHANNEL);
                    }

                    await _directLineConnector.PostAsync(deviceRegistration.ConversationId, incomingMessage.Content, userId: incomingMessage.FromUserName, subchannel: WECHAT_SUBCHANNEL);
                }catch(Exception ex)
                {
                    //TODO - Post some failure notice to the user
                    _logger.LogError(ex, "Could not post message to direct line");
                }   
            });

            return Ok(SUCCESS);
        }

        [Route("outgoingmessage")]
        public async Task<IActionResult> PostOutgoingMessage()
        {
            var json = await this.ReadRequestContentAsync();
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("/wechat/outgoingmessage invoked without a message in post body");
                return BadRequest("No message sent to post!");
            }

            WechatMessage message;
            try
            {
                message = JsonConvert.DeserializeObject<WechatMessage>(json);
            }catch(JsonSerializationException ex)
            {
                _logger.LogError(ex, "Bad JSON for outgoing WeChat Message: {0}", json);
                return BadRequest("JSON not properly formatted for WeChat message");
            }

            _logger.LogInformation("Outgoing message received | Type: {0}, Contents: \"{1}\", ToUserId: {2}", message.MessageType, message.Content, message.ToUserName);

            try
            {
                ////Test image sending
                //var imgPath = $"{_hostingEnvironment.WebRootPath}/imgs/star.png";
                //var mediaId = await _connector.UploadMedia(WechatMessageTypes.IMAGE, imgPath);

                //var mediaMessage = new WechatMessage
                //{
                //    MessageType = WechatMessageTypes.IMAGE,
                //    MediaId = mediaId,
                //    ToUserName = message.ToUserName
                //};

                //await _connector.PostMessage(mediaMessage);
                await _connector.PostMessage(message);
                return Ok(SUCCESS);
            }
            catch(Exception ex)
            {
                var errorMessage = "Could not post message to WeChat customer service";
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, errorMessage);
            }
        }
    }
}