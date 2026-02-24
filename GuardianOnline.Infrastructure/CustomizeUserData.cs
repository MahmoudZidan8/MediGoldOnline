using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace GuardianOnline.Infrastructure
{
    [MetadataType(typeof(ProviderUserDatametadata))]
 

    
    public partial class ProviderUserData
    {
        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Confirm Password is Required")]
        [Compare("UserPassword",ErrorMessage ="Password is not Matched")]
        public string confUserPassword { get; set; }

    }
    public class ProviderUserDatametadata
    {


        public int UserID { get; set; }
        [Required(ErrorMessage = "User Name is Required")]
        [Display(Name = "User Name")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Email Address is Required")]
        [RegularExpression(@"^([0-9a-zA-Z]([\+\-_\.][0-9a-zA-Z]+)*)+@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,3})$", ErrorMessage = "Your email address is not in a valid format")]
        [Display(Name = "Email Address")]
        [DataType(DataType.EmailAddress)]
        public string UserEmail { get; set; }

        [Required(ErrorMessage = "Password is Required")]
        [Display(Name = "Password")]
        //[StringLength(15,MinimumLength =8,ErrorMessage ="Should Between 8 and 15 Digits")]
        //[RegularExpression(@"^((?=.*[a-z])(?=.*[A-Z])(?=.*\d)).+$",ErrorMessage ="Password is not a Strong Password")]
        public string UserPassword { get; set; }
        public string IPAddress { get; set; }
        [Display(Name = "Create Date")]
        public System.DateTime CreateDate { get; set; }
        public System.DateTime LastLoginDate { get; set; }
        [Required(ErrorMessage = "First Name is Required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is Required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        //[Required(ErrorMessage = "User Locked is Required")]
        [Display(Name = "User Locked")]
        public bool IsLockedOut { get; set; }
        public bool IsFirstLogin { get; set; }
        public int InvalidPasswordAttempts { get; set; }
        public bool IsAdmin { get; set; }

        [Display(Name = "User Type")]
        public int UserTypeID { get; set; }

        [Display(Name = "Customer User")]
        public int UserTypeCustomerID { get; set; }

        [Display(Name = "Provider Name")]
        public int ProviderID { get; set; }

        [Display(Name = "Provider Branch")]
        public int BranchID { get; set; }



    }
}