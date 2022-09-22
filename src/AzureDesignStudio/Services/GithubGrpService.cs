using AzureDesignStudio.SharedModels.Protos;
using Grpc.Net.ClientFactory;

namespace AzureDesignStudio.Services
{
    public class GithubGrpService
    {
        private readonly Github.GithubClient _githubClient = null!;
        private readonly GrpcClientFactory _clientFactory = null!;

        public GithubGrpService(GrpcClientFactory grpcClientFactory)
        {
            _clientFactory = grpcClientFactory;
            _githubClient = _clientFactory.CreateClient<Github.GithubClient>("GithubClientWithAuth");
        }

        public async Task<UploadGithubResponse> UploadContent(string repositoryName, string branchName, string filePath, string content)
        {
            var uploadContent = new UploadGithubRequest
            {
                RepositoryName = repositoryName,
                BranchName = branchName,
                FilePath = filePath,
                Content = content
            };

            try
            {
                var response = await _githubClient.UploadAsync(uploadContent);
                response.StatusCode = 200;

                return response;

            }
            catch (Exception)
            {
                return new UploadGithubResponse { StatusCode = 500 };

            }
        }

        public async Task<GetRepositoriesResponse> GetRepositories()
        {
            var repositoriesRequest = new GetRepositoriesRequest();

            try
            {
                var response = await _githubClient.GetRepositoriesAsync(repositoriesRequest);
                response.StatusCode = 200;

                return response;
            }
            catch (Exception)
            {
                return new GetRepositoriesResponse { StatusCode = 500 };
            }
        }

        public async Task<GetBranchesResponse> GetBranches(long repoId)
        {
            var branchRequest = new GetBranchesRequest
            {
                RepositoryId = repoId
            };

            try
            {
                var response = await _githubClient.GetBranchesAsync(branchRequest);
                response.StatusCode = 200;

                return response;
            }
            catch (Exception)
            {
                return new GetBranchesResponse { StatusCode = 500 };
            }
        }

        public async Task<GetBranchDirectoriesResponse> GetBranchDirectories(long repoId, string branchName)
        {
            var branchDirectoriesRequest = new GetBranchDirectoriesRequest
            {
                RepositoryId = repoId,
                BranchName = branchName
            };

            try
            {
                var response = await _githubClient.GetBranchDirectoriesAsync(branchDirectoriesRequest);
                response.StatusCode = 200;

                return response;
            }
            catch (Exception)
            {
                return new GetBranchDirectoriesResponse { StatusCode = 500 };
            }
        }
    }
}
