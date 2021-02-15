using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DomeTag.BlazorClient.Shared
{
    public partial class DomeTagImageUpload
    {
        [Inject]
        public IHttpClientFactory HttpClientFactory { get; set; }

        public string ImgUrl { get; set; }

        private async Task HandleSelected(InputFileChangeEventArgs e)
        {
            var client = HttpClientFactory.CreateClient("DomeTagAPI");
            var imageFile = e.File;

            if (imageFile == null)
                return;

            var resizedFile = await imageFile.RequestImageFileAsync("image/png", 300, 500);

            using (var ms = resizedFile.OpenReadStream(resizedFile.Size))
            {
                var content = new MultipartFormDataContent();
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
                content.Add(new StreamContent(ms, Convert.ToInt32(resizedFile.Size)), "image", imageFile.Name);

                var response = await client.PostAsync("upload", content);
                ImgUrl = await response.Content.ReadAsStringAsync();
            }
        }
    }
}
