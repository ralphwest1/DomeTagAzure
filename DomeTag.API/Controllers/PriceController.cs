using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomeTag.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class PriceController : Controller
    {
		[HttpGet]
		public async Task<IActionResult> Get([FromQuery] PriceService.PriceRequest priceRequest)
		{
			var priceService = new PriceService();
			var priceResponse = priceService.CalculatePrice(priceRequest);


			return Ok(priceResponse);
		}
	}
}
