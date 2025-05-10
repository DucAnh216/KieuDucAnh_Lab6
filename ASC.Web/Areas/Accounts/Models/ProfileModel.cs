using System.ComponentModel.DataAnnotations;

namespace ASC.Web.Areas.Accounts.Models
{
    public class ProfileModel
    {
        [Required(ErrorMessage = "Tên người dùng là bắt buộc.")]
        [Display(Name = "Tên người dùng")]
        [StringLength(50, ErrorMessage = "Tên người dùng phải dưới 50 ký tự.")]
        public string UserName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}