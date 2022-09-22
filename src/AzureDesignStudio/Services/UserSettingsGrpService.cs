using AzureDesignStudio.SharedModels.Protos;
using AzureDesignStudio.SharedModels.User;
using Grpc.Net.ClientFactory;

namespace AzureDesignStudio.Services
{
    public class UserSettingsGrpService
    {
        private readonly UserSettings.UserSettingsClient _userSettingsClient = null!;
        private readonly GrpcClientFactory _clientFactory = null!;

        public UserSettingsGrpService(GrpcClientFactory grpcClientFactory)
        {
            _clientFactory = grpcClientFactory;
            _userSettingsClient = _clientFactory.CreateClient<UserSettings.UserSettingsClient>("UserSettingsClientWithAuth");
        }

        public async Task<AddOrUpdateUserSettingResponse> AddOrUpdateUserSetting(UserSettingType userSettingType, string value)
        {
            var uploadContent = new AddOrUpdateUserSettingRequest
            {
                Type = (int)userSettingType,
                Value = value ?? string.Empty
            };

            try
            {
                var response = await _userSettingsClient.AddOrUpdateUserSettingAsync(uploadContent);
                return response;
            }
            catch (Exception)
            {
                return new AddOrUpdateUserSettingResponse { StatusCode = 500 };
            }
        }

        public async Task<GetUserSettingResponse> GetUserSetting(UserSettingType userSettingType)
        {
            var uploadContent = new GetUserSettingRequest
            {
                Type = (int)userSettingType                
            };

            try
            {
                var response = await _userSettingsClient.GetUserSettingAsync(uploadContent);                
                return response;
            }
            catch (Exception)
            {
                return new GetUserSettingResponse { StatusCode = 500 };
            }
        }
    }
}
