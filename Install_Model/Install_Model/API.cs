using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AI_Artist_V2
{
    internal class API
    {        
        class AutomaticJsonSetModels
        {
            public string sd_model_checkpoint = "";
        }

        public static async Task API_SetModel(string IP, string model)
        {
            AutomaticJsonSetModels payload = new AutomaticJsonSetModels();
            payload.sd_model_checkpoint = model;

            string json = JsonConvert.SerializeObject(payload);

            var client = new HttpClient();
            var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("http://" + IP + "/sdapi/v1/options"),
                Method = HttpMethod.Post,
                Content = content
            };
            try
            {
                var response = await client.SendAsync(request);
            }
            catch { }
        }

        class AutomaticJsonSetUnet
        {
            // "Automatic" | "None"
            public string sd_unet = "Automatic";
        }

        public static async Task API_SetUnet(string IP, string unet) //For TRT
        {
            Thread.Sleep(1000);
            AutomaticJsonSetUnet payload = new AutomaticJsonSetUnet();
            payload.sd_unet = unet;

            string json = JsonConvert.SerializeObject(payload);

            var client = new HttpClient();
            var content = new System.Net.Http.StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri("http://" + IP + "/sdapi/v1/options"),
                Method = HttpMethod.Post,
                Content = content
            };
            try
            {
                var response = await client.SendAsync(request);
            }
            catch { }
        }
    }
}
