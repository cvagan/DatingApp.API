using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    public class PhotosController : Controller
    {
        private readonly IDatingRepository _repo;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;
        public PhotosController(IDatingRepository repo, IMapper mapper,
            IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _repo = repo;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        // api/users/{userId}/photos/{id} GET route for single photo
        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        // api/users/{userId}/photos POST route
        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, PhotoForCreationDto photoDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // get user from repo, based on userId from route
            var user = await _repo.GetUser(userId);

            // bad request if user is not found
            if (user == null)
                return BadRequest("Could not find user");

            // fetches id from token that came with the Http request
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // returns unathorized if token id and route id do not match
            if (currentUserId != user.Id)
                return Unauthorized();

            // stores uploaded file from request in 'file' variable
            var file = photoDto.File;

            // creates new ImageUploadResult object (Cloudinary class)
            var uploadResult = new ImageUploadResult();

            // checks if a file was uploaded
            if (file.Length > 0)
            {
                // opens a file stream
                using (var stream = file.OpenReadStream())
                {
                    // creates a new uploadparams object and inserts the filename and stream
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill")
                            .Gravity("face")
                    };

                    // stores the result of the upload
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }
            

            photoDto.Url = uploadResult.Uri.ToString();
            photoDto.PublicId = uploadResult.PublicId;

            // maps properties from the uploaded photo to a Photo object
            var photo = _mapper.Map<Photo>(photoDto);

            // sets the user of the photo object to be the current user (found in repo)
            photo.User = user;

            // sets the currently uploaded photo to be the main photo if one isn't already set
            if (!user.Photos.Any(m => m.IsMain))
                photo.IsMain = true;

            // adds photo object to database
            user.Photos.Add(photo);

            var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);

            // saves changes to database, and gives a CreatedAtRoute response if successful
            if (await _repo.SaveAll())
            {
                return CreatedAtRoute("GetPhoto", new { id = photo.Id }, photoToReturn);    
            }

            // returns BadRequest if changes were not saved
            return BadRequest("Could not add photo");
        }

        // api/users/{userId}/photos/{id}/setMain POST route
        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMain(int userId, int id) {
            // check if userid from route equals userid from auth token
            if (userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();

            // find photo from repo using id
            var photoFromRepo = await _repo.GetPhoto(id);

            if (photoFromRepo == null)
                return NotFound();

            // check if photo is main
            if (photoFromRepo.IsMain)
                return BadRequest("This is already the main photo");

            // find the current main photo for user
            var currentMainPhoto = await _repo.GetMainPhoto(userId);

            // set the current main photo to false
            if (currentMainPhoto != null)
                currentMainPhoto.IsMain = false;

            // set new photo to main
            photoFromRepo.IsMain = true;

            // save changes and return 204 no content
            if (await _repo.SaveAll())
                return NoContent();

            // return bad request if something went wrong
            return BadRequest("Could not set photo to main");
        }
    }
}
