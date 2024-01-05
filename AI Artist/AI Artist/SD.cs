
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AI_Artist_V2
{
	internal class SD
	{
		public static JsonResponseBase Send(JsonRequestBase request)
		{
			string orgPrompt = request.prompt;
			string orgNegative = request.negative_prompt;

			try
			{
				//wildcards convert
				request.negative_prompt = Wildcards.Convert(request.negative_prompt);
				if (orgNegative != request.negative_prompt)
				{

				}

				request.prompt = Wildcards.Convert(request.prompt);
				if (orgPrompt != request.prompt)
				{

				}

				if (request.GetType() == typeof(JsonRequestImg2Img))
				{
					var reqImg2Img = request as JsonRequestImg2Img;
					return SendImg2Img(reqImg2Img);
				}

				if (request.GetType() == typeof(JsonRequestTxt2Img))
				{
					var reqTxt2Img = request as JsonRequestTxt2Img;
					return SendTxt2Img(reqTxt2Img);
				}
			}
			finally
			{
				request.prompt = orgPrompt;
				request.negative_prompt = orgNegative;
			}
			return null;
		}


		public static JsonResponseTxt2Img SendTxt2Img(JsonRequestTxt2Img objJson)
		{
			string jsonString = JsonSerializer.Serialize(objJson);

			var url = $"{DataCenter.Http_url_API}/sdapi/v1/txt2img";

			var request = WebRequest.Create(url);
			request.Method = "POST";

			string json = jsonString;
			byte[] byteArray = Encoding.UTF8.GetBytes(json);

			request.ContentType = "application/json";
			request.ContentLength = byteArray.Length;

			using (var reqStream = request.GetRequestStream())
			{
				reqStream.Write(byteArray, 0, byteArray.Length);

				using (var response = request.GetResponse())
				{
					Debug.WriteLine(((HttpWebResponse)response).StatusDescription);

					using (var respStream = response.GetResponseStream())
					using (var reader = new StreamReader(respStream))
					{

						string jsonresponse = reader.ReadToEnd();
						//Debug.WriteLine(data);

						var ret = JsonSerializer.Deserialize<JsonResponseTxt2Img>(jsonresponse);
						ret.Info = JsonSerializer.Deserialize<JsonResponseInfo>(ret.info);
						return ret;
					}
				}
			}
		}


		public static JsonResponseImg2Img SendImg2Img(JsonRequestImg2Img objJson)
		{
			string jsonString = JsonSerializer.Serialize(objJson);

			var url = $"{DataCenter.Http_url_API}/sdapi/v1/img2img";

			var request = WebRequest.Create(url);
			request.Method = "POST";

			string json = jsonString;
			byte[] byteArray = Encoding.UTF8.GetBytes(json);

			request.ContentType = "application/json";
			request.ContentLength = byteArray.Length;

			using (var reqStream = request.GetRequestStream())
			{
				reqStream.Write(byteArray, 0, byteArray.Length);

				using (var response = request.GetResponse())
				{
					Debug.WriteLine(((HttpWebResponse)response).StatusDescription);

					using (var respStream = response.GetResponseStream())
					using (var reader = new StreamReader(respStream))
					{
						string jsonresponse = reader.ReadToEnd();

						var ret = JsonSerializer.Deserialize<JsonResponseImg2Img>(jsonresponse);
						ret.Info = JsonSerializer.Deserialize<JsonResponseInfo>(ret.info);
						return ret;
					}
				}
			}
		}

		public static string SendImg2tag(JsonRequestImg2tag objJson)
		{

			string jsonString = JsonSerializer.Serialize(objJson);

			var url = $"{DataCenter.Http_url_API}/sdapi/v1/interrogate";

			var request = WebRequest.Create(url);
			request.Method = "POST";

			string json = jsonString;
			byte[] byteArray = Encoding.UTF8.GetBytes(json);

			request.ContentType = "application/json";
			request.ContentLength = byteArray.Length;
			request.Timeout = 600000;

			using (var reqStream = request.GetRequestStream())
			{
				reqStream.Write(byteArray, 0, byteArray.Length);

				using (var response = request.GetResponse())
				{
					Debug.WriteLine(((HttpWebResponse)response).StatusDescription);

					using (var respStream = response.GetResponseStream())
					using (var reader = new StreamReader(respStream))
					{
						string jsonresponse = reader.ReadToEnd();

						var ret = JsonSerializer.Deserialize<JsonResponseImg2tagInfo>(jsonresponse);
						return ret.caption;

					}
				}
			}
		}


		public static string SendRefreshCKPT(string objJson)
		{
			string jsonString = JsonSerializer.Serialize(objJson);

			var url = $"{DataCenter.Http_url_API}/sdapi/v1/refresh-checkpoints";

			var request = WebRequest.Create(url);
			request.Method = "POST";

			string json = jsonString;
			byte[] byteArray = Encoding.UTF8.GetBytes(json);

			request.ContentType = "application/json";
			request.ContentLength = byteArray.Length;
			request.Timeout = 600000;

			using (var reqStream = request.GetRequestStream())
			{
				reqStream.Write(byteArray, 0, byteArray.Length);

				using (var response = request.GetResponse())
				{
					Debug.WriteLine(((HttpWebResponse)response).StatusDescription);

					using (var respStream = response.GetResponseStream())
					using (var reader = new StreamReader(respStream))
					{
						string jsonresponse = reader.ReadToEnd();
						return null;

					}
				}
			}
		}

		public static string SendRefreshLora(string objJson)
		{
			string jsonString = JsonSerializer.Serialize(objJson);

			var url = $"{DataCenter.Http_url_API}/sdapi/v1/refresh-loras";

			var request = WebRequest.Create(url);
			request.Method = "POST";

			string json = jsonString;
			byte[] byteArray = Encoding.UTF8.GetBytes(json);

			request.ContentType = "application/json";
			request.ContentLength = byteArray.Length;
			request.Timeout = 600000;

			using (var reqStream = request.GetRequestStream())
			{
				reqStream.Write(byteArray, 0, byteArray.Length);

				using (var response = request.GetResponse())
				{
					Debug.WriteLine(((HttpWebResponse)response).StatusDescription);

					using (var respStream = response.GetResponseStream())
					using (var reader = new StreamReader(respStream))
					{
						string jsonresponse = reader.ReadToEnd();
						return null;
					}
				}
			}
		}


		public static JsonResponseProgress SendGetProgress()
		{
			var url = $"{DataCenter.Http_url_API}/sdapi/v1/progress";

			var request = WebRequest.Create(url);
			request.Method = "GET";

			using (var response = request.GetResponse())
			{
				using (var respStream = response.GetResponseStream())
				using (var reader = new StreamReader(respStream))
				{
					string jsonresponse = reader.ReadToEnd();

					return JsonSerializer.Deserialize<JsonResponseProgress>(jsonresponse);
				}
			}
		}


		public static string ImageSourceToBase64String(ImageSource imageSource)
		{
			if (imageSource == null)
				throw new Exception("Unknown error in image processing. Try again.");

			var encoder = new PngBitmapEncoder(); // You can change the format as needed

			var bitmapSource = (BitmapSource)imageSource;
			encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

			using (MemoryStream ms = new MemoryStream())
			{
				encoder.Save(ms);
				var rawdata = ms.ToArray();
				return "data:image/png;base64," + Convert.ToBase64String(rawdata);
			}
		}

		public static bool SaveBase64EncodingData(string text, string file)
		{
			//TODO: fixed image type
			text = text.Replace("data:image/png;base64,", "");
			var rawdata = Convert.FromBase64String(text);

			using (FileStream fs = new FileStream(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
			{
				fs.SetLength(0);
				fs.Write(rawdata, 0, rawdata.Length);
				return true;
			}
		}
	}

	public class JsonRequestImg2tag
	{

		public string image { get; set; }
		public string model { get; set; }

	}
	public class JsonResponseImg2tagInfo
	{
		public string caption { get; set; }
	}


	public class JsonRequestBase
	{
		public string prompt { get; set; } = "";

		public string[] styles { get; set; }
		public float denoising_strength { get; set; } = 0.75f;

		public decimal seed { get; set; } = -1;

		public decimal subseed { get; set; } = -1;
		public float subseed_strength { get; set; } = 0.0f;
		public int seed_resize_from_h { get; set; } = -1;
		public int seed_resize_from_w { get; set; } = -1;

		public int batch_size { get; set; } = 1;
		public int n_iter { get; set; } = 1;
		public int steps { get; set; } = 20;
		public float cfg_scale { get; set; } = 7.0f;
		public int width { get; set; } = 512;
		public int height { get; set; } = 512;


		public bool restore_faces { set; get; } = false;
		public bool tiling { set; get; } = false;

		public string negative_prompt { get; set; } = "";

		public float eta { get; set; } = 0;

		public float s_churn { get; set; } = 0;
		public float s_tmax { get; set; } = 0;

		public float s_tmin { get; set; } = 0;
		public float s_noise { get; set; } = 1;

		public Override_Settings override_settings { get; set; }

		public string sampler_index { get; set; } = "Euler a";

	}


	public class Override_Settings
	{
		public int eta_noise_seed_delta { get; set; } = 0;
		public int CLIP_stop_at_last_layers { get; set; } = 2;
	}




	public class JsonRequestTxt2Img : JsonRequestBase
	{
		public bool enable_hr { set; get; } = false;
		public int firstphase_width { get; set; } = 0;
		public int firstphase_height { get; set; } = 0;

	}



	public class JsonRequestImg2Img : JsonRequestBase
	{
		public string[] init_images { get; set; }

		public int resize_mode { get; set; } = 0;


		public string mask { get; set; }

		public int mask_blur { get; set; } = 4;


		public int inpainting_fill { get; set; } = 0;
		public bool inpaint_full_res { get; set; } = false;
		public int inpaint_full_res_padding { get; set; } = 0;
		public int inpainting_mask_invert { get; set; } = 0;


		public bool include_init_images { get; set; } = false;
	}

	public class JsonResponseBase
	{
		public string[] images { get; set; }
		public string info { get; set; }
		public JsonResponseInfo Info { get; set; }
	}

	public class JsonResponseTxt2Img : JsonResponseBase
	{
		public ParametersTxt2Img parameters { get; set; }
	}

	public class ParametersBase
	{
		public float denoising_strength { get; set; }

		public string prompt { get; set; }
		public object styles { get; set; }
		public decimal seed { get; set; }
		public decimal subseed { get; set; }
		public float subseed_strength { get; set; }
		public int seed_resize_from_h { get; set; }
		public int seed_resize_from_w { get; set; }
		public int batch_size { get; set; }
		public int n_iter { get; set; }
		public int steps { get; set; }
		public float cfg_scale { get; set; }
		public int width { get; set; }
		public int height { get; set; }

		public bool restore_faces { get; set; }
		public bool tiling { get; set; }
		public string negative_prompt { get; set; }
		public float eta { get; set; }
		public float s_churn { get; set; }
		public float s_tmax { get; set; }
		public float s_tmin { get; set; }
		public float s_noise { get; set; }
		public Override_Settings override_settings { get; set; }
		public string sampler_index { get; set; }
	}

	public class ParametersTxt2Img : ParametersBase
	{
		public bool enable_hr { get; set; }
		public int firstphase_width { get; set; }
		public int firstphase_height { get; set; }
	}

	public class JsonResponseImg2Img : JsonResponseBase
	{
		public ParametersResponseImg2Img parameters { get; set; }
	}

	public class ParametersResponseImg2Img : ParametersBase
	{
		public object init_images { get; set; }
		public int resize_mode { get; set; }
		public object mask { get; set; }
		public int mask_blur { get; set; }
		public int inpainting_fill { get; set; }
		public bool inpaint_full_res { get; set; }
		public int inpaint_full_res_padding { get; set; }
		public int inpainting_mask_invert { get; set; }

		public bool include_init_images { get; set; }
	}

	public class Extra_Generation_Params
	{
	}

	public class JsonResponseInfo
	{
		public string prompt { get; set; }
		public string[] all_prompts { get; set; }
		public string negative_prompt { get; set; }
		public decimal seed { get; set; }
		public decimal[] all_seeds { get; set; }
		public decimal subseed { get; set; }
		public decimal[] all_subseeds { get; set; }
		public float subseed_strength { get; set; }
		public int width { get; set; }
		public int height { get; set; }
		public int sampler_index { get; set; }
		public string sampler { get; set; }
		public float cfg_scale { get; set; }
		public int steps { get; set; }
		public int batch_size { get; set; }
		public bool restore_faces { get; set; }
		public object face_restoration_model { get; set; }
		public string sd_model_hash { get; set; }
		public int seed_resize_from_w { get; set; }
		public int seed_resize_from_h { get; set; }
		public float denoising_strength { get; set; }
		public Extra_Generation_Params extra_generation_params { get; set; }
		public int index_of_first_image { get; set; }
		public string[] infotexts { get; set; }
		public object[] styles { get; set; }
		public string job_timestamp { get; set; }
		public int clip_skip { get; set; }
	}

	public class JsonResponseProgress
	{
		public float progress { get; set; }
		public float eta_relative { get; set; }
		public JsonResponseProgressState state { get; set; }
		//public object current_image { get; set; }
	}

	public class JsonResponseProgressState
	{
		public bool skipped { get; set; }
		public bool interrupted { get; set; }
		public string job { get; set; }
		public int job_count { get; set; }
		public int job_no { get; set; }
		public int sampling_step { get; set; }
		public int sampling_steps { get; set; }
	}
}
