﻿using AzureDesignStudio.SharedModels.Protos;

namespace AzureDesignStudio.Models
{
    public class UploadToGithubModel
    {
        public long RepositoryId{ get; set; }
        public string BranchName { get; set; }
    }
}
