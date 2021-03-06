﻿using Blazored.Toast.Services;
using DomeTag.BlazorClient.HttpInterceptor;
using DomeTag.BlazorClient.HttpRepository;
using DomeTag.API.SharedEntities.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DomeTag.BlazorClient.Pages
{
	public partial class UpdateProduct : IDisposable
	{
		private Product _product;
		private EditContext _editContext;
		private bool formInvalid = true;

		[Inject]
		public IProductHttpRepository ProductRepo { get; set; }

		[Inject]
		public HttpInterceptorService Interceptor { get; set; }

		[Inject]
		public IToastService ToastService { get; set; }

		[Parameter]
		public Guid Id { get; set; }

		protected async override Task OnInitializedAsync()
		{
			_product = await ProductRepo.GetProduct(Id);
			_editContext = new EditContext(_product);
			_editContext.OnFieldChanged += HandleFieldChanged;
			Interceptor.RegisterEvent();
		}

		private void HandleFieldChanged(object sender, FieldChangedEventArgs e)
		{
			formInvalid = !_editContext.Validate();
			StateHasChanged();
		}

		private async Task Update()
		{
			await ProductRepo.UpdateProduct(_product);

			ToastService.ShowSuccess($"Action successful. " +
				$"Product \"{_product.Name}\" successfully updated.");
		}

		private void AssignImageUrl(string imgUrl)
			=> _product.ImageUrl = imgUrl;

		public void Dispose()
		{
			Interceptor.DisposeEvent();
			_editContext.OnFieldChanged -= HandleFieldChanged;
		}
	}
}
