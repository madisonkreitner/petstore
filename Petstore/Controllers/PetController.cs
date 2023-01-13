using IO.Swagger.Models;
using Microsoft.AspNetCore.Mvc;

namespace Petstore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetController : ControllerBase
    {
        private static readonly List<Pet> _pets = new List<Pet>
        { 
            new Pet { Name = "dog1", Category = new() { Name = "dog" }, Id = 1, PhotoUrls = new List<string>() { "/path/to/file1" }, Status = Pet.StatusEnum.AvailableEnum },
            new Pet { Name = "dog2", Category = new() { Name = "dog" }, Id = 2, PhotoUrls = new List<string>() { "/path/to/file1" }, Status = Pet.StatusEnum.AvailableEnum },
            new Pet { Name = "dog3", Category = new() { Name = "dog" }, Id = 3, PhotoUrls = new List<string>() { "/path/to/file1" }, Status = Pet.StatusEnum.AvailableEnum },
            new Pet { Name = "dog4", Category = new() { Name = "dog" }, Id = 4, PhotoUrls = new List<string>() { "/path/to/file1" }, Status = Pet.StatusEnum.AvailableEnum },
            new Pet { Name = "dog5", Category = new() { Name = "dog" }, Id = 5, PhotoUrls = new List<string>() { "/path/to/file1" }, Status = Pet.StatusEnum.AvailableEnum },
        };

        private readonly ILogger<PetController> _logger;

        public PetController(ILogger<PetController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetPets")]
        public IEnumerable<Pet> Get()
        {
            return _pets.ToArray();
        }

        [HttpPost(Name = "AddPet")]
        public IActionResult AddPet([FromBody] Pet pet)
        {
            // do something to add a pet
            _logger.LogDebug("Adding pet {@pet}", pet);
            return Ok();
        }
    }
}