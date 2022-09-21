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
            var (owner, branch) = ("VictorM88", "main");

            try
            {
                var fileDetails = await gitHubClient.Repository.Content.GetAllContentsByRef(owner, request.RepositoryName, request.FilePath, branch);
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
                    new CreateFileRequest($"Creating config {request.FilePath}", request.Content, branch)
                    ).ConfigureAwait(false);
            }
            catch (Exception)
            {
                return new UploadGithubResponse { StatusCode = 500 };
            }

            return new UploadGithubResponse { StatusCode = 200 };
        }

        public override async Task<GetRepositoriesResponse> GetRepositories(GetRepositoriesRequest request, ServerCallContext context)
        {
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
            var response = new GetBranchesResponse();
            var branches = await gitHubClient.Repository.Branch.GetAll(request.RepositoryId).ConfigureAwait(false);

            response.Branch.AddRange(branches.Select(r => r.Name).ToList());

            return response;
        }
    }
}
