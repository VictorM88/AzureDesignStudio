using AzureDesignStudio.SharedModels.Protos;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using Octokit;

namespace AzureDesignStudio.Server.Services
{
    [Authorize]
    [RequiredScope(RequiredScopesConfigurationKey = "AzureAdB2C:Scopes")]
    public class GithubService : Github.GithubBase
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<GithubService> logger;
        private readonly GitHubClient gitHubClient;

        public GithubService(IConfiguration configuration, ILogger<GithubService> logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            gitHubClient = new GitHubClient(new ProductHeaderValue("AzureDesignStudio"));
            gitHubClient.Credentials = new Credentials(configuration["GithubPAT"]);
        }

        public override async Task<UploadGithubResponse> Upload(UploadGithubRequest request, ServerCallContext context)
        {
            var (owner, repoName, branch) = ("VictorM88", "Testing", "main");

            try
            {
                var fileDetails = await gitHubClient.Repository.Content.GetAllContentsByRef(owner, repoName, request.FilePath, branch);
                await gitHubClient.Repository.Content.UpdateFile(
                    owner,
                    repoName,
                    request.FilePath,
                    new UpdateFileRequest($"Updating config {request.FilePath}", request.Content, fileDetails.Last().Sha)
                    );
            }
            catch (NotFoundException)
            {
                await gitHubClient.Repository.Content.CreateFile(
                    owner,
                    repoName,
                    request.FilePath,
                    new CreateFileRequest($"Creating config {request.FilePath}", request.Content, branch)
                    );
            }
            catch (Exception)
            {
                return new UploadGithubResponse { Succeeded = false };
            }

            return new UploadGithubResponse { Succeeded = true };
        }
    }
}
