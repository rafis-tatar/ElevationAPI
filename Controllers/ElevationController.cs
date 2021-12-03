using ElevationAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace ElevationAPI.Models
{
    [ApiController]
    [Route("[controller]")]
    public class ElevationController : ControllerBase
    {    
        [HttpGet]
        public Location GetElevation(double latitude, double longitude)
        {
            return new Location();
        }
    }
}