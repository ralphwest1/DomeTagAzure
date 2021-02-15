using DomeTag.BlazorClient.Features;
using DomeTag.API.SharedEntities.Models;
using DomeTag.API.SharedEntities.RequestFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DomeTag.BlazorClient.HttpRepository
{
	public interface IDomeTagHttpRepository
	{
		Task<PagingResponse<Product>> GetProducts(ProductParameters productParameters);
		Task<Product> GetProduct(Guid id);
		Task CreateProduct(Product product);
		Task<string> UploadProductImage(MultipartFormDataContent content);
		Task UpdateProduct(Product product);
		Task DeleteProduct(Guid id);
	}
}
