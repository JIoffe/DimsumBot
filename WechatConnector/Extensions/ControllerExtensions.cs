using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Threading.Tasks;

namespace WechatConnector.Extensions
{
    public static class ControllerExtensions
    {
        public static async Task<string> ReadRequestContentAsync(this Controller controller)
        {
            var request = controller.Request;
            byte[] buffer = new byte[request.ContentLength.Value];
            await request.Body.ReadAsync(buffer, 0, (int)request.ContentLength.Value);
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
