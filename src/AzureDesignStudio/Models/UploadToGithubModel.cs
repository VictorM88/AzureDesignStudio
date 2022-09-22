using AzureDesignStudio.SharedModels.Protos;
using System.ComponentModel.DataAnnotations;

namespace AzureDesignStudio.Models
{
    public class UploadToGithubModel
    {
        [Required]
        [Range(1, long.MaxValue, ErrorMessage = "Please select a value")]
        public long RepositoryId { get; set; }

        [Required(ErrorMessage = "Please select a value")]
        public string BranchName { get; set; }

        [Required(ErrorMessage = "Please select a value")]
        public string DirectoryPath { get; set; }

        [Required(ErrorMessage = "Please add a file name")]
        public string FileName { get; set; }
    }
}
