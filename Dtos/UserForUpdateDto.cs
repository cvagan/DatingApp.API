using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.Dtos
{
    public class UserForUpdateDto
    {
        public string Introduction { get; set; }
        public string LookingFor { get; set; }
        public string Interests { get; set; }
        [Required]
        public string City { get; set; }
        public string Country { get; set; }
    }
}