using System.ComponentModel.DataAnnotations;

namespace MVAManagement.ViewModels.Account
{
    public class UserProfileView
    {
        [Display(Name = "Username")]
        public string UserName { get; set; }  

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }    

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}
