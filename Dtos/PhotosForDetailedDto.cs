using System;

namespace DatingApp.API.Dtos
{
    public class PhotosForDetailedDto
    {
        // selected data to return for the api/users/{id} get route, as part of UserForDetailedDto
        public int Id { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsMain { get; set; }
    }
}