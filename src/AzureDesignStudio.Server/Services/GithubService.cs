using AzureDesignStudio.Server.Models;
using AzureDesignStudio.SharedModels.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web.Resource;
using Octokit;

namespace AzureDesignStudio.Server.Services
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    public class GithubService : Github.GithubBase
    {
        private readonly DesignDbContext designContext;
        private readonly ILogger<GithubService> logger;
        private readonly GitHubClient gitHubClient;

        public GithubService(DesignDbContext context, ILogger<GithubService> logger)
        {
            this.logger = logger;
            gitHubClient = new GitHubClient(new ProductHeaderValue("AzureDesignStudio"));

            designContext = context;
            designContext.Database.EnsureCreated();            
        }

        public override async Task<UploadGithubResponse> Upload(UploadGithubRequest request, ServerCallContext context)
        {
            await SetGithubClientCredentialsAsync(context).ConfigureAwait(false);
            var githubUser = await gitHubClient.User.Current().ConfigureAwait(false);
            var owner = githubUser.Login;

            try
            {
                var fileDetails = await gitHubClient.Repository.Content.GetAllContentsByRef(owner, request.RepositoryName, request.FilePath, request.BranchName);
                await gitHubClient.Repository.Content.UpdateFile(
                    owner,
                    request.RepositoryName,
                    request.FilePath,
                    new UpdateFileRequest($"Updating config {request.FilePath}", request.Content, fileDetails.Last().Sha)
                    ).ConfigureAwait(false);
            }
            catch (NotFoundException)
            {
                await gitHubClient.Repository.Content.CreateFile(
                    owner,
                    request.RepositoryName,
                    request.FilePath,
                    new CreateFileRequest($"Creating config {request.FilePath}", request.Content, request.BranchName)
                    ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Uploading failed");
                return new UploadGithubResponse { StatusCode = 500 };
            }

            return new UploadGithubResponse { StatusCode = 200 };
        }

        public override async Task<GetRepositoriesResponse> GetRepositories(GetRepositoriesRequest request, ServerCallContext context)
        {
            await SetGithubClientCredentialsAsync(context).ConfigureAwait(false);

            var response = new GetRepositoriesResponse();
            var repositories = await gitHubClient.Repository.GetAllForCurrent().ConfigureAwait(false);

            response.Repository.AddRange(repositories.Select(r => new GithubRepository
            {
                Id = r.Id,
                Name = r.Name,
            }).ToList());

            return response;
        }

        public override async Task<GetBranchesResponse> GetBranches(GetBranchesRequest request, ServerCallContext context)
        {
            await SetGithubClientCredentialsAsync(context).ConfigureAwait(false);

            var response = new GetBranchesResponse();
            var branches = await gitHubClient.Repository.Branch.GetAll(request.RepositoryId).ConfigureAwait(false);

            response.Branch.AddRange(branches.Select(r => r.Name).ToList());

            return response;
        }

        public override async Task<GetBranchDirectoriesResponse> GetBranchDirectories(GetBranchDirectoriesRequest request, ServerCallContext context)
        {
            await SetGithubClientCredentialsAsync(context).ConfigureAwait(false);

            var response = new GetBranchDirectoriesResponse();
            var repoContent = await gitHubClient.Git.Tree.GetRecursive(request.RepositoryId, request.BranchName).ConfigureAwait(false);

            foreach (var treeItem in repoContent.Tree)
            {
                if (treeItem.Type != TreeType.Tree)
                    continue;

                response.Direcotry.Add(treeItem.Path);
            }

            return response;
        }

        private async Task SetGithubClientCredentialsAsync(ServerCallContext context)
        {
            var userIdClaim = ServiceTools.GetUserId(context);
            if (string.IsNullOrWhiteSpace(userIdClaim))
            {
                return;
            }
            var userId = new Guid(userIdClaim);

            var githubPatSetting = await designContext.UserSettings.FirstOrDefaultAsync(us => us.Type == SharedModels.User.UserSettingType.GithubPat && us.UserId == userId).ConfigureAwait(false);
            if (githubPatSetting != null)
            {
                gitHubClient.Credentials = new Credentials(githubPatSetting.Value);
            }
        }
    }
}
