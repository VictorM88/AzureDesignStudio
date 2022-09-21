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

        public async Task<bool> UploadContent(string filePath, string content)
        {
            var uploadContent = new UploadGithubRequest
            {
                FilePath = filePath,
                Content = content
            };

            var response = await _githubClient.UploadAsync(uploadContent);

            return response.Succeeded;
        }
    }
}
