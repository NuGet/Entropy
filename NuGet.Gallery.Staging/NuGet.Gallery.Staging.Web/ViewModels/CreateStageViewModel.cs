using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NuGet.Gallery.Staging.Web.ViewModels
{
    public class CreateStageViewModel
    {
        [Required]
        [RegularExpression("[a-zA-Z0-9-_]+")]
        [MinLength(4)]
        [MaxLength(100)]
        [DisplayName("Stage URL")]
        public string StageName { get; set; }
    }
}