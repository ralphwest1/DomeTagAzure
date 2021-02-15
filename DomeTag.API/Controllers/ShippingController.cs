using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DomeTag.ExteranlServices.ShipEngine;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DomeTag.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShippingController : ControllerBase
    {
        // GET: api/<Shipping>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var rates = ShipEngineService.GetRates();
            return new OkObjectResult(rates);
        }
    }
}
