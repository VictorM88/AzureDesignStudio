using AzureDesignStudio.Server.Models;
using AzureDesignStudio.SharedModels.Protos;
using AzureDesignStudio.SharedModels.User;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web.Resource;

namespace AzureDesignStudio.Server.Services
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    public class UserSettingsService : UserSettings.UserSettingsBase
    {
        private readonly DesignDbContext designContext;
        private readonly ILogger<UserSettingsService> logger;

        public UserSettingsService(DesignDbContext context, ILogger<UserSettingsService> logger)
        {
            designContext = context;
            designContext.Database.EnsureCreated();

            this.logger = logger;
        }

        public override async Task<AddOrUpdateUserSettingResponse> AddOrUpdateUserSetting(AddOrUpdateUserSettingRequest request, ServerCallContext context)
        {
            var response = new AddOrUpdateUserSettingResponse();
            var requestSettingType = (UserSettingType)request.Type;

            // Get user id
            var userIdClaim = ServiceTools.GetUserId(context);
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                logger.LogWarning("Access denied. userIdClaim is empty.");
                response.StatusCode = StatusCodes.Status401Unauthorized;
                return response;
            }

            var userId = new Guid(userIdClaim);
            var existingSetting = await designContext.UserSettings.FirstOrDefaultAsync(us => us.Type == requestSettingType && us.UserId == userId).ConfigureAwait(false);

            if (existingSetting == null)
            {
                await designContext.UserSettings.AddAsync(new UserSettingModel
                {
                    Type = requestSettingType,
                    Value = request.Value,
                    UserId = userId
                }).ConfigureAwait(false);
            }
            else
            {
                existingSetting.Value = request.Value;
            }

            try
            {
                await designContext.SaveChangesAsync();
                response.StatusCode = StatusCodes.Status200OK;
            }
            catch (DbUpdateException dbex)
            {
                logger.LogWarning(dbex, "DbUpdateException occurred.");
                response.StatusCode = StatusCodes.Status400BadRequest;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Exception occurred.");
                response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            return response;            
        }

        public override async Task<GetUserSettingResponse> GetUserSetting(GetUserSettingRequest request, ServerCallContext context)
        {
            var response = new GetUserSettingResponse();
            var requestSettingType = (UserSettingType)request.Type;

            // Get user id
            var userIdClaim = ServiceTools.GetUserId(context);
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                logger.LogWarning("Access denied. userIdClaim is empty.");
                response.StatusCode = StatusCodes.Status401Unauthorized;
                return response;
            }

            var userId = new Guid(userIdClaim);
            var userSetting= await designContext.UserSettings.FirstOrDefaultAsync(us => us.Type == requestSettingType && us.UserId == userId).ConfigureAwait(false);

            if (userSetting != null)
            {
                response.Value = userSetting.Value;
            }
            response.StatusCode = StatusCodes.Status200OK;

            return response;
        }
    }
}
