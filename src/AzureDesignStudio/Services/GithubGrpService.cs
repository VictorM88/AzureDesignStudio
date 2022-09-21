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

        public async Task<UploadGithubResponse> UploadContent(string repositoryName, string filePath, string content)
        {
            var uploadContent = new UploadGithubRequest
            {
                RepositoryName = repositoryName,
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
    }
}
