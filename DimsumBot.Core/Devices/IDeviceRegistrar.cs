using DimsumBot.Core.Devices.Model;
using System.Threading.Tasks;

namespace DimsumBot.Core.Devices
{
    public interface IDeviceRegistrar
    {
        Task<DeviceRegistration> GetDeviceRegistrationAsync(string userId, string channelId, string subchannelId = null);
        Task RegisterDeviceAsync(DeviceRegistration registration);
    }
}
