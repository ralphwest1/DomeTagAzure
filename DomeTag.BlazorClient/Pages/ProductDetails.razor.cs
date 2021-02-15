using DomeTag.BlazorClient.HttpRepository;
using DomeTag.API.SharedEntities.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomeTag.BlazorClient.Pages
{
	public partial class ProductDetails
	{
		public Product Product { get; set; } = new Product();

		[Inject]
		public IProductHttpRepository ProductRepo { get; set; }

		[Parameter]
		public Guid ProductId { get; set; }

		protected async override Task OnInitializedAsync()
		{
			Product = await ProductRepo.GetProduct(ProductId);
		}
	}
}
