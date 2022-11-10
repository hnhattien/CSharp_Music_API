using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicAppAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LikeTableController : ControllerBase
    {
        private readonly IConfiguration _iconfiguration;

        public LikeTableController(IConfiguration configuration)
        {
            _iconfiguration = configuration;
        }
    }
}
