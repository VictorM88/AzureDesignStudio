﻿using AntDesign;
using AzureDesignStudio.Models;
using AzureDesignStudio.SharedModels.Protos;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text;

namespace AzureDesignStudio.Components.MenuDrawer
{
    internal record StepsStatus
    {
        public int CurrentStep { get; set; }
        public string Status { get; set; } = "starting";
        public string ErrorMessage { get; set; } = default!;
    }
    public partial class CodeDrawerTemplate
    {
        private CodeDrawerContent _drawerContent = null!;
        private string _codeClass = "line-numbers language-json";
        private const string upperRight = "position:absolute;top:86px;right:41px;z-index:10";
        private string _buttonStyle = upperRight;
        private bool _showDeployButton = false;
        private bool _showDeployParams = false;
        private DeploymentParameters _deployParams = null!;
        private Form<DeploymentParameters> _paramsForm = null!;
        private SubscriptionRes? _linkedSubscription = null;
        private IList<string> _resourceGroupNames = null!;
        private bool _paramFormLoading = true;
        private bool _showDeployStatus = false;
        private bool _showUploadToGithubModal = false;
        private StepsStatus _stepsStatus = new();
        private AuthenticationState _authState;
        private Button UploadToGitButton;
        private Form<UploadToGithubModel> _uploadToGithubForm;
        private List<GithubRepository> _githubRepositories = new List<GithubRepository>();
        private List<string> _githubBranchNames = new List<string>();
        private List<string> _githubBranchDirectories = new List<string>();
        private UploadToGithubModel _uploadToGithubModel = new UploadToGithubModel { FileName = "azure-design-studio" };

        #region Button style and download
        protected override async Task OnInitializedAsync()
        {
            _drawerContent = Options;
            _codeClass = _drawerContent.Type switch
            {
                CodeDrawerContentType.Bicep => "line-numbers language-bicep",
                CodeDrawerContentType.Json => "line-numbers language-json",
                _ => "line-numbers language-json"
            };

            _authState = await authenticationStateTask;

            if (_drawerContent.Type == CodeDrawerContentType.Json)
            {
                var user = _authState.User;
                if (user.Identity?.IsAuthenticated ?? false)
                {
                    var subscriptions = await _deployService.GetLinkedSubscriptions();
                    if (subscriptions?.Count > 0)
                    {
                        _showDeployButton = true;
                        _linkedSubscription = subscriptions[0];
                    }
                }
            }

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            await JS.InvokeVoidAsync("Prism.highlightAll");
        }

        void HandleAffixChange(bool status)
        {
            if (status)
            {
                _buttonStyle = "float:right;";
            }
            else
            {
                _buttonStyle = upperRight;
            }
        }

        async Task HandleDownload()
        {
            await DownloadFileAsync(_drawerContent.Content, $"{_uploadToGithubModel.FileName}{GetFileExtension()}", "application/json");
        }

        // According to: https://docs.microsoft.com/en-us/aspnet/core/blazor/file-downloads?view=aspnetcore-6.0
        private async Task DownloadFileAsync(string content, string filename, string contentType)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            using var streamRef = new DotNetStreamReference(stream);
            await JS.InvokeVoidAsync("downloadFileFromStream", filename, contentType, streamRef);
        }


        async Task HandleUpload()
        {
            var repos = await _githubService.GetRepositories().ConfigureAwait(false);
            _githubRepositories = repos.Repository.ToList();
            _showUploadToGithubModal = true;
        }

        async Task HandleUploadCancel()
        {
            _showUploadToGithubModal = false;
        }


        async Task HandleUploadToGithub()
        {
            if (!_uploadToGithubForm.Validate())
                return;

            UploadToGitButton.Disabled = true;
            _showUploadToGithubModal = false;

            _message.Info("Uploading to GitHub");

            var selectedSepository = _githubRepositories.First(r => r.Id == _uploadToGithubModel.RepositoryId);
            var response = await _githubService.UploadContent(
                selectedSepository.Name,
                _uploadToGithubModel.BranchName,
                $"{_uploadToGithubModel.DirectoryPath}/{_uploadToGithubModel.FileName}{GetFileExtension()}",
                _drawerContent.Content
                ).ConfigureAwait(false);

            if (response.StatusCode == 200)
                _message.Info("Upload complete");
            else
                _message.Error("Something went wrong. Try again.");

            UploadToGitButton.Disabled = false;
        }

        private string GetFileExtension()
        {
            return _drawerContent.Type switch
            {
                CodeDrawerContentType.Json => ".json",
                CodeDrawerContentType.Bicep => ".bicep",
                _ => ".json"
            };
        }

        private async Task OnSelectedRepositoryChangedHandler(long repoId)
        {
            var branches = await _githubService.GetBranches(repoId).ConfigureAwait(false);
            _githubBranchNames = branches.Branch.ToList();
        }

        private async Task OnSelectedBranchChangedHandler(string branchName)
        {
            var directories = await _githubService.GetBranchDirectories(_uploadToGithubModel.RepositoryId, branchName).ConfigureAwait(false);
            _githubBranchDirectories = directories.Direcotry.ToList();
        }

        #endregion

        async Task HandleDeploy()
        {
            if (_linkedSubscription == null)
                return;

            if (_stepsStatus.Status == "starting")
            {
                _paramFormLoading = true;
                _deployParams = new DeploymentParameters();
                _showDeployParams = true;

                _resourceGroupNames = await _deployService.GetResourceGroups(_linkedSubscription.SubscriptionId);

                _paramFormLoading = false;
            }
            else
            {
                _showDeployStatus = true;
            }
        }
        private void ParamsFormCancel(MouseEventArgs e)
        {
            _showDeployParams = false;
        }
        private void ParamsFormOk(MouseEventArgs e)
        {
            _paramsForm.Submit();
        }
        private async Task ParamsFormFinish(EditContext editContext)
        {
            _showDeployParams = false;

            _stepsStatus.Status = "started";
            _showDeployStatus = true;

            // TODO: work on the parameters.
            await _deployService.CreateDeployment(_linkedSubscription!.SubscriptionId,
                _deployParams.ResourceGroup, _drawerContent.Content, "{}",
                async (deploymentStatus, errorMessage) =>
                {
                    var stateHasChanged = false;

                    var step = deploymentStatus switch
                    {
                        "started" => 0,
                        "processing" => 1,
                        "completed" => 2,
                        _ => _stepsStatus.CurrentStep
                    };
                    if (step != _stepsStatus.CurrentStep)
                    {
                        _stepsStatus.CurrentStep = step;
                        stateHasChanged = true;
                    }
                    if (deploymentStatus == "error" && deploymentStatus != _stepsStatus.Status)
                    {
                        _stepsStatus.Status = "error";
                        _stepsStatus.ErrorMessage = errorMessage;
                        stateHasChanged = true;
                    }
                    else if (deploymentStatus == "processing" && _stepsStatus.Status != "process")
                    {
                        _stepsStatus.Status = "process";
                        stateHasChanged = true;
                    }
                    else if (deploymentStatus == "completed")
                    {
                        _stepsStatus.Status = "finish";
                        stateHasChanged = true;
                    }

                    if (stateHasChanged)
                        await InvokeAsync(StateHasChanged);
                });
        }
        private void CloseDeployStatus(MouseEventArgs e)
        {
            _showDeployStatus = false;
            if (_stepsStatus.Status == "error" || _stepsStatus.Status == "finish")
            {
                _stepsStatus = new();
            }
        }
    }
}
