using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using static AI_Artist_V2.API;
using static AI_Artist_V2.DataCenter;

namespace AI_Artist_V2
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		string[] Args;

		Storyboard Story = new Storyboard();

		bool IsDragging = false;
		Point StartPoint;

		DispatcherTimer Start_SD_Timer;

		bool Switch_opt = false;
		bool Progress_Run = false;
		bool Progress_Run_Abort = false;
		DispatcherTimer ProssceTimer;
		double ProssceTimer_temp = 0;
		double Total_Second = 0;
		int Image_count = 1;
		bool GenerateImg_Finish = true;

		List<string> Seeds_OutputImg = new List<string>();
		List<string> Paths_OutputImg = new List<string>();
		string Path_OutputImg_Grid = "";
		string Path_ImportImg = "";
		int Num_SelectedImg = 0;

		string GeneratedTag = "";

		bool Delete_Is_Style = true;
		string Delete_File_Path = "";

		public MainWindow()
		{
			InitializeComponent();
			Args = Environment.GetCommandLineArgs();
			for (int i = 0; i < Args.Length; i++)
			{
				Args[i] = Args[i].ToLower();
			}
			_MainWindow = this;

			#region Loading Animation
			DoubleAnimation da = new DoubleAnimation(0, 3600, new Duration(TimeSpan.FromSeconds(20)));
			RotateTransform rotate = new RotateTransform();
			Image_Loading.RenderTransform = rotate;
			Storyboard.SetTarget(da, Image_Loading);

			Storyboard.SetTargetProperty(da, new PropertyPath("RenderTransform.Angle"));
			da.RepeatBehavior = RepeatBehavior.Forever;
			Story.Children.Add(da);
			#endregion Initial Loading Animation

			#region Check HW + Start SD
			Story.Begin();
			Canvas_Loading.Visibility = Visibility.Visible;
			Text_Load.Text = "Loading";

			Logger.Log("");
			Logger.Log("##################################");
			Logger.Log("##### Starting MSI AI Artist #####");
			Logger.Log("##################################");

			if (File.Exists(DataCenter.Path_Resource + "\\default.txt"))
			{
				string[] default_txt = File.ReadAllLines(DataCenter.Path_Resource + "\\default.txt");

				if (default_txt.Length > 1)
				{
					if (default_txt[0] == "[Prompt_Default]")
					{
						TextBox_Prompt.Text = default_txt[1];
					}
				}
			}

			Start_SD_Timer = new DispatcherTimer();
			Start_SD_Timer.Tick += new EventHandler(Start_SD_Timer_Tick);

			Thread thread_Init_SD = new Thread(Init_SD);
			thread_Init_SD.Start();
			#endregion

			#region General Img Timer
			ProssceTimer = new DispatcherTimer();
			ProssceTimer.Interval = TimeSpan.FromMilliseconds(100);
			ProssceTimer.Tick += new EventHandler(ProssceTimer_Tick);
			#endregion
		}


		#region Denosing slider
		private void Slider_Denosing_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (Label_Denosing != null)
				Label_Denosing.Content = Slider_Denosing.Value + "%";
		}
		#endregion

		#region Text Modify 
		private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if (!char.IsDigit(e.Text, e.Text.Length - 1) || e.Text == "-")
			{
				e.Handled = true;
			}
		}

		private void TextBox_PreviewTextInput_Weight(object sender, TextCompositionEventArgs e)
		{
			try
			{
				if ((!char.IsDigit(e.Text, e.Text.Length - 1)) && (e.Text != "."))
				{
					e.Handled = true;
				}
			}
			catch { }
		}

		private void TextBox_PreviewTextInput_Seed(object sender, TextCompositionEventArgs e)
		{
			if ((!char.IsDigit(e.Text, e.Text.Length - 1)) && (e.Text != "-"))
			{
				e.Handled = true;
			}
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = (TextBox)sender;

			if (int.TryParse(textBox.Text, out int inputValue))
			{
				if (inputValue > 3)
				{
					textBox.Text = "3";
					textBox.CaretIndex = textBox.Text.Length;
				}
			}
		}
		#endregion

		#region Mouse Move
		private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			IsDragging = true;
			StartPoint = e.GetPosition(this);
			CaptureMouse();
		}

		private void Grid_MouseMove(object sender, MouseEventArgs e)
		{
			if (IsDragging)
			{
				Point currentPoint = e.GetPosition(this);
				double offsetX = currentPoint.X - StartPoint.X;
				double offsetY = currentPoint.Y - StartPoint.Y;

				Window window = Window.GetWindow(this);
				window.Left += offsetX;
				window.Top += offsetY;
			}
		}

		private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			IsDragging = false;
			ReleaseMouseCapture();
		}
		#endregion

		#region Unload
		private void Windows_Unload(object sender, RoutedEventArgs e)
		{

		}
		#endregion

		#region Button_mini_Click Button_close_Click
		private void Button_mini_Click(object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}

		private void Button_close_Click(object sender, RoutedEventArgs e)
		{
			if (DataCenter._Install != null)
			{
				DataCenter._Install.Close_Process();
			}
			Close();
		}
		#endregion

		#region Switch Opt
		private async void SwitchOpt_Checked(object sender, RoutedEventArgs e)
		{
			Switch_opt = true;

			CB_Style.IsEnabled = false;
			Btn_Style_Add.Visibility = Visibility.Hidden;
			Btn_Style_Add.IsEnabled = false;
			Btn_Style_Delete.Visibility = Visibility.Hidden;
			Btn_Style_Delete.IsEnabled = false;

			CB_LoRA.IsEnabled = false;
			Btn_LoRA_Add.IsEnabled = false;
			Btn_LoRA_Add.Visibility = Visibility.Hidden;
			CB_LoRA.SelectedIndex = 0;
			Btn_LoRA_Delete.Visibility = Visibility.Hidden;
			Btn_LoRA_Delete.IsEnabled = false;

			Label_LoRA_Weight.Visibility = Visibility.Hidden;
			Label_LoRA_Weight.Foreground = new SolidColorBrush(Colors.Gray);
			TextBox_LoRA_Weight.Visibility = Visibility.Hidden;
			TextBox_LoRA_Weight.IsEnabled = false;
			TextBox_LoRA_Weight.Text = "";

			Btn_ImportImg.IsEnabled = true;
			if (Path_ImportImg != "")
			{
				Btn_Replace.IsEnabled = true;
			}

			DataCenter._Install.Close_Process();

			CB_Style.SelectedIndex = 0;

			if (Btn_ImportImg.Content == null)
			{
				Btn_ImportImg_Text.Visibility = Visibility.Visible;
			}

			if (Opt_Pytorch.IsChecked == true)
			{
				CB_Style.IsEnabled = true;
				Read_Style("pytorch");
				Btn_Style_Add.IsEnabled = true;
				Read_Lora("pytorch");

				CB_LoRA.IsEnabled = true;
				Btn_LoRA_Add.IsEnabled = true;

				Label_LoRA_Weight.Visibility = Visibility.Visible;
				TextBox_LoRA_Weight.Visibility = Visibility.Visible;
				Btn_Style_Add.Visibility = Visibility.Visible;
				Btn_LoRA_Add.Visibility = Visibility.Visible;
				Btn_LoRA_Delete.Visibility = Visibility.Visible;
				Btn_Style_Delete.Visibility = Visibility.Visible;

				Story.Begin();
				Canvas_Loading.Visibility = Visibility.Visible;
				DataCenter._MainWindow.Text_Load.Text = "Switch Loading";

				await Task.Run(() =>
				{
					Thread.Sleep(1000);
					DataCenter._Install.OpenStableDiffusion(false);
				});
				await API_SetUnet(DataCenter.Url_API, "None");

				Start_SD_Timer.Interval = new TimeSpan(0, 0, 5);
				Start_SD_Timer.Start();
			}
			else if (Opt_xformers.IsChecked == true)
			{
				CB_Style.IsEnabled = true;
				Read_Style("xformers");
				Btn_Style_Add.IsEnabled = true;
				Read_Lora("xformers");

				CB_LoRA.IsEnabled = true;
				Btn_LoRA_Add.IsEnabled = true;

				Label_LoRA_Weight.Visibility = Visibility.Visible;
				TextBox_LoRA_Weight.Visibility = Visibility.Visible;
				Btn_Style_Add.Visibility = Visibility.Visible;
				Btn_LoRA_Add.Visibility = Visibility.Visible;
				Btn_LoRA_Delete.Visibility = Visibility.Visible;
				Btn_Style_Delete.Visibility = Visibility.Visible;

				Story.Begin();
				Canvas_Loading.Visibility = Visibility.Visible;
				DataCenter._MainWindow.Text_Load.Text = "Switch Loading";

				await Task.Run(() =>
				{
					Thread.Sleep(1000);
					DataCenter._Install.OpenStableDiffusion(true);
				});
				await API_SetUnet(DataCenter.Url_API, "None");

				Start_SD_Timer.Interval = new TimeSpan(0, 0, 5);
				Start_SD_Timer.Start();
			}
			else if (Opt_TensorRT.IsChecked == true)
			{
				CB_Style.IsEnabled = false;
				Read_Style("trt");
				Btn_Style_Add.IsEnabled = false;
				Read_Lora("trt");

				CB_LoRA.IsEnabled = true;
				Btn_LoRA_Add.IsEnabled = false;

				Story.Begin();
				Canvas_Loading.Visibility = Visibility.Visible;
				DataCenter._MainWindow.Text_Load.Text = "Switch Loading";

				await Task.Run(() =>
				{
					Thread.Sleep(1000);
					DataCenter._Install.OpenStableDiffusion(true);
				});
				await API_SetUnet(DataCenter.Url_API, "[TRT] TRT_Base");

				Start_SD_Timer.Interval = new TimeSpan(0, 0, 5);
				Start_SD_Timer.Start();
			}

			Switch_opt = false;
		}
		#endregion

		#region Generate Image
		private async void Button_Generate_Click(object sender, RoutedEventArgs e)
		{
			Progress_Run = false;

			if ((TextBox_Prompt.Text == "") && (Btn_ImportImg.Content == null))
			{
				return;
			}

			Btn_Generate.Content = "Generating";//"Interrupt";
			Btn_Generate.IsEnabled = false;
			Total_Second = 0.0;

			Paths_OutputImg = new List<string>();
			Seeds_OutputImg = new List<string>();

			#region Count
			if (RB_Count_1.IsChecked == true)
			{
				Image_count = 1;
			}
			else if (RB_Count_4.IsChecked == true)
			{
				Image_count = 4;
			}
			else if (RB_Count_9.IsChecked == true)
			{
				Image_count = 9;
			}
			else if (RB_Count_16.IsChecked == true)
			{
				Image_count = 16;
			}
			#endregion

			Btn_OutputImg.Content = null;
			GenerateImg_Finish = false;
			OutputImage_Grid.Visibility = Visibility.Collapsed;

			if (TextBox_Prompt.Text == "")
			{
				Story.Begin();
				Canvas_Loading.Visibility = Visibility.Visible;
				Text_Load.Text = "Tag Loading";

				await Task.Run(() =>
				{
					Generate_Tag();
				});

				TextBox_Prompt.Text = GeneratedTag;
				Canvas_Loading.Visibility = Visibility.Collapsed;
				Story.Stop();
			}

			#region Generate Image
			for (int i = 0; i < Image_count; i++)
			{
				while (Progress_Run)
				{
					await Task.Delay(100);
				}

				Count_Number.Text = (i) + " / " + Image_count;

				if (i + 1 == Image_count)
				{
					GenerateImg_Finish = true;
				}

				StartProgress();

				#region To Image
				_ = Task.Factory.StartNew(() =>
				{
					try
					{
						string file = "";

						#region Web-UI

						#region Txt or Img
						JsonRequestBase request = null;
						if (Path_ImportImg == "")
						{
							request = new JsonRequestTxt2Img();
						}
						else
						{
							request = new JsonRequestImg2Img();
						}
						#endregion

						#region request (prompt...)
						Dispatcher.Invoke(new Action(() =>
						{
							request.prompt = TextBox_Prompt.Text;
							//if (File.Exists(DataCenter.Path_SD_LoRA))
							//{
							string temp = "";
							string selectedValue = CB_LoRA.SelectedItem.ToString().Substring(38);
							if (selectedValue == "None")
							{

							}
							else if (selectedValue == "All MSI")
							{
								if (Opt_TensorRT.IsChecked == false)
								{
									if (File.Exists(DataCenter.File_SD_LoRA_NB))
									{
										temp = "<lora:" + MSI_LoRA_NB + ":0.7>, <lora:" + MSI_LoRA_CND_GNP + ":0.8>,";
									}
									else
									{
										Logger.Log("LoRA file not exist:" + MSI_LoRA_NB + MSI_LoRA_CND_GNP);
									}
								}
							}
							else
							{
								temp = "<lora:" + selectedValue + ":" + TextBox_LoRA_Weight.Text + " >, ";
							}
							request.prompt = temp + request.prompt;

							if (!DataCenter.HideCMD)
							{
								Logger.Log(request.prompt);
							}
							//}

							request.negative_prompt = DataCenter.Negative_prompt;

							request.denoising_strength = float.Parse((Slider_Denosing.Value / 100).ToString());
							request.cfg_scale = (float)7;
							request.steps = int.Parse((string)Steps_text.Text);
							request.sampler_index = "Euler a";
							try
							{
								if (uint.Parse((string)Seed_text.Text) >= 0)
									request.seed = uint.Parse((string)Seed_text.Text);
							}
							catch
							{
								request.seed = -1;
							}

							request.batch_size = 1;
							if (Size_1.IsChecked == true)
							{
								request.width = 512;
								request.height = 512;
							}
							else if (Size_2.IsChecked == true)
							{
								request.width = 512;
								request.height = 768;
							}
							else if (Size_3.IsChecked == true)
							{
								request.width = 768;
								request.height = 512;
							}

							request.restore_faces = false;
							request.tiling = false;

							request.override_settings = new Override_Settings();
							request.override_settings.CLIP_stop_at_last_layers = 2;
							request.override_settings.eta_noise_seed_delta = 0;

							try
							{
								request.subseed = uint.Parse((string)Seed_text.Text);
							}
							catch
							{
								request.subseed = -1;
							}
							request.subseed_strength = 0;
							request.seed_resize_from_w = 0;
							request.seed_resize_from_h = 0;

							if (request.GetType() == typeof(JsonRequestImg2Img))
							{
								var reqImg2Img = request as JsonRequestImg2Img;
								reqImg2Img.init_images = new string[1];

								System.Windows.Controls.Image img = new System.Windows.Controls.Image();
								BitmapImage bitmapImage = new BitmapImage(new Uri(Path_ImportImg));
								img.Source = bitmapImage;
								reqImg2Img.init_images[0] = SD.ImageSourceToBase64String(img.Source);
							}
						}));
						#endregion

						var responseObj = SD.Send(request);

						#region Get Image
						int index = 0;

						Dispatcher.Invoke(new Action(() =>
						{
							foreach (var encodedimg in responseObj.images)
							{
								Btn_OutputImg.Content = null;
								var Seedtext = responseObj.Info.all_seeds[index];

								string output_data = DateTime.Now.ToString("yyMMdd_HHmmss");
								file = System.IO.Path.Combine(DataCenter.OutputImage_path,
									output_data + "_" + Seedtext.ToString() + ".png");
								SD.SaveBase64EncodingData(encodedimg, file);
								Btn_OutputImg.Content = file;

								Seeds_result.Content = "Seeds: " + Seedtext;
								index++;

								Paths_OutputImg.Add(file);
								Seeds_OutputImg.Add(Seedtext.ToString());
							}
						}));
						#endregion

						#endregion
						Logger.Log("ok: generate maybe succeeded");
						return;
					}
					catch (Exception ex)
					{
						if (ex.Message.IndexOf("This functionality may not") < 0)
							Logger.Log(ex.Message);
						Logger.Log("error: generate failed. try again");
						return;
					}
					finally
					{
						Progress_Run_Abort = true;
					}
				});
				#endregion
			}
			#endregion

			while (Progress_Run)
			{
				await Task.Delay(100);
			}

			StackPanel_OutputImg_Preview.Children.Clear();
			OutputImage_Grid.Children.Clear();

			#region Image_Grid Size
			int temp_width = 480;
			int temp_height = 480;

			switch (Paths_OutputImg.Count)
			{
				case 1:
					break;
				case 4:
					temp_width = temp_width / 2;
					temp_height = temp_height / 2;

					break;
				case 9:
					temp_width = temp_width / 3;
					temp_height = temp_height / 3;
					break;
				case 16:
					temp_width = temp_width / 4;
					temp_height = temp_height / 4;
					break;
			}
			#endregion

			#region Image Grid Create
			Path_OutputImg_Grid = "";
			for (int i = 0; i < Paths_OutputImg.Count; i++)
			{
				Button temp_btn_grid = new Button();
				temp_btn_grid.Style = (Style)FindResource("Style_Button_Grid_Image");
				//temp_btn_grid.Tag = i;
				temp_btn_grid.Content = Paths_OutputImg[i];
				temp_btn_grid.Width = temp_width;
				temp_btn_grid.Height = temp_height;
				//temp_btn_grid.Click += Btn_temp_btn_Click;

				OutputImage_Grid.Children.Add(temp_btn_grid);
			}
			#endregion

			#region Image Grid Display?
			if (Paths_OutputImg.Count > 1)
			{
				OutputImage_Grid.Visibility = Visibility.Visible;

				await Task.Delay(500);

				string output_data = DateTime.Now.ToString("yyMMdd_HHmmss");
				Path_OutputImg_Grid = System.IO.Path.Combine(DataCenter.OutputImage_path, output_data + "_Grid.png");
				RenderToPNGFile(OutputImage_Grid, Path_OutputImg_Grid);

				RadioButton temp_btn = new RadioButton();
				temp_btn.Style = (Style)FindResource("Style_Button_Output_Little_Image");
				temp_btn.Tag = -1;
				temp_btn.Content = Path_OutputImg_Grid;
				temp_btn.Width = 60;
				temp_btn.Click += Btn_SelectPreviewImg_Click;
				//temp_btn.Click += Btn_grid_Click;

				OutputImage_Grid.Visibility = Visibility.Collapsed;

				StackPanel_OutputImg_Preview.Children.Add(temp_btn);
			}
			#endregion

			#region output little img
			for (int i = 0; i < Paths_OutputImg.Count; i++)
			{
				RadioButton temp_btn = new RadioButton();
				temp_btn.Style = (Style)FindResource("Style_Button_Output_Little_Image");
				temp_btn.Tag = i;
				temp_btn.Content = Paths_OutputImg[i];
				temp_btn.Width = 60;
				temp_btn.Checked += Btn_SelectPreviewImg_Click;

				StackPanel_OutputImg_Preview.Children.Add(temp_btn);
			}

			if (StackPanel_OutputImg_Preview.Children.Count > 0 && StackPanel_OutputImg_Preview.Children[0] is RadioButton btn)
			{
				btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
				btn.IsChecked = true;
			}

			#endregion

			Btn_Generate.Content = "Generate";
			Btn_Generate.IsEnabled = true;
			Btn_OutputImg.IsEnabled = true;

			Btn_PSD.IsEnabled = true;
			Btn_PSD_In.IsEnabled = true;

			Btn_Replace.IsEnabled = true;
			Btn_Replace_In.IsEnabled = true;
		}
		#endregion

		#region Generate Tag
		private void Generate_Tag()
		{
			var processInfo = new ProcessStartInfo("cmd.exe")
			{
				CreateNoWindow = true,
				UseShellExecute = false,
				RedirectStandardInput = true,
				WorkingDirectory = DataCenter.Path_App
			};

			if (File.Exists(Path_ImportImg))
			{
				Process process = Process.Start(processInfo);
				process.StandardInput.WriteLine($"venv\\Scripts\\activate");
				process.StandardInput.WriteLine($"cd BLIP");
				File.Copy(Path_ImportImg, "BLIP\\temp_image.png", true);
				process.StandardInput.WriteLine("python blip_V1.py --image \"temp_image.png\"");
				process.StandardInput.WriteLine("exit");
				process.WaitForExit();
				if (File.Exists($@"{DataCenter.Path_App}\BLIP\tagger_prompt.txt"))
				{
					string output = System.IO.File.ReadAllText($@"{DataCenter.Path_App}\BLIP\tagger_prompt.txt");
					GeneratedTag = output;
				}
				else
				{
					GeneratedTag = "Error";
					Logger.Log("File tagger_prompt.txt is not exist");
				}
			}
		}
		#endregion

		#region Select Preview Image
		private void Btn_SelectPreviewImg_Click(object sender, RoutedEventArgs e)
		{
			if (GenerateImg_Finish)
			{
				if (sender is RadioButton btn)
				{
					int Num_Selected = (int)btn.Tag;
					Num_SelectedImg = Num_Selected;

					if (Num_Selected != -1)
					{
						Btn_OutputImg.Content = Paths_OutputImg[Num_Selected];
						//Seconds_result.Visibility = Visibility.Collapsed;
						if (Paths_OutputImg.Count > 1)
						{
							Seeds_result.Content = "Seed: " + Seeds_OutputImg[Num_Selected];
						}
					}
					else
					{
						Btn_OutputImg.Content = Path_OutputImg_Grid;
						//Seconds_result.Visibility = Visibility.Visible;
						Seconds_result.Content = "Second: " + Total_Second.ToString("0.0");
						Seeds_result.Content = "Seed: N/A";
					}
					OutputImage_Grid.Visibility = Visibility.Collapsed;
				}
			}
		}
		#endregion

		#region StartProgress
		private void StartProgress()
		{
			ProgressBar_State.Value = 0;
			if (Progress_Run)
				return;
			Progress_Run = true;
			Progress_Run_Abort = false;
			Dispatcher.BeginInvoke(new Action(() =>
			{
				Stackpanel_Progress.Visibility = Visibility.Visible;
			}));
			ProssceTimer_temp = 0.0;
			ProssceTimer.Start();

			Task.Factory.StartNew(() =>
			{
				try
				{
					while (Progress_Run_Abort == false)
					{
						var response = SD.SendGetProgress();
						if (Progress_Run_Abort)
							break;

						Dispatcher.BeginInvoke(new Action(() =>
						{
							ProgressBar_State.Value = (int)(response.progress * 100.0);
						}));

						Thread.Sleep(100);
					}
					Dispatcher.BeginInvoke(new Action(() =>
					{
						Seconds_result.Content = "Seconds: " + Seconds_text.Text;
						ProgressBar_State.Value = 100;
					}));
					ProssceTimer.Stop();
				}
				catch (Exception ex)
				{
					ProssceTimer.Stop();
					Dispatcher.BeginInvoke(new Action(() =>
					{
						Stackpanel_Progress.Visibility = Visibility.Collapsed;
					}));
					Logger.Log(ex.Message);
					Logger.Log("error: get progress failed");
				}
				finally
				{
					ProssceTimer.Stop();
					Progress_Run = false;

					if (GenerateImg_Finish)
					{
						Dispatcher.BeginInvoke(new Action(() =>
						{
							Stackpanel_Progress.Visibility = Visibility.Collapsed;
						}));
					}
				}
			});
		}
		#endregion

		#region Style Add (Base model)
		private async void Btn_Style_Add_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Safetensors Files|*.safetensors";

			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					Story.Begin();
					Canvas_Loading.Visibility = Visibility.Visible;
					Text_Load.Text = "Style Loading";

					await Task.Run(() =>
					{
						File.Copy(openFileDialog.FileName,
						DataCenter.Path_SD + "\\models\\Stable-diffusion\\" + openFileDialog.SafeFileName, true);

						SD.SendRefreshCKPT("string");

						Thread.Sleep(1000);

						Dispatcher.BeginInvoke(new Action(() =>
						{
							Read_Style("xformers");

							string[] temp = openFileDialog.SafeFileName.ToString().Split('.');
							ComboBoxItem cbItem = CB_Style.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content?.ToString() == temp[0]);
							CB_Style.SelectedItem = cbItem;

							Thread.Sleep(2000);
						}));
					});
					await API_SetModel(DataCenter.Url_API, openFileDialog.SafeFileName);

					Canvas_Loading.Visibility = Visibility.Collapsed;
					Story.Stop();
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion

		#region LoRA Add
		private void Btn_LoRA_Add_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Safetensors Files|*.safetensors";

			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					File.Copy(openFileDialog.FileName,
						DataCenter.Path_SD + "\\models\\Lora\\" + openFileDialog.SafeFileName, true);
					SD.SendRefreshLora("string");
					Read_Lora("xformers");

					string[] temp = openFileDialog.SafeFileName.ToString().Split('.');
					ComboBoxItem cbItem = CB_LoRA.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Content?.ToString() == temp[0]);
					CB_LoRA.SelectedItem = cbItem;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}
		#endregion

		#region Check_SD
		internal void Init_SD()
		{
			#region Check HW
			DataCenter.chw = new CheckHW();
			DataCenter.chw.GetGPUInfo();
			DataCenter.GPU_NVIDIA = DataCenter.chw.GetGPU_NVIDIA();
			DataCenter.IsSupport_TRT = DataCenter.chw.GetGPU_TRT();
			if (Args.Contains("-trt"))
			{
				DataCenter.IsSupport_TRT = true; //for test
			}

			this.Dispatcher.Invoke(new Action(() =>
			{
				if (DataCenter.GPU_NVIDIA == true)
				{
					Opt_Pytorch.Visibility = Visibility.Visible;
					Opt_xformers.Visibility = Visibility.Visible;
				}

				if (DataCenter.IsSupport_TRT == true)
				{
					Opt_TensorRT.Visibility = Visibility.Visible;
				}
			}));
			#endregion

			DataCenter._Install = new Install();
			DataCenter._Install.Check_Python_Git_SD();

			DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
			{
				DataCenter._MainWindow.Text_Load.Text = " Init Loading";
			}));

			#region Default Select
			this.Dispatcher.Invoke(new Action(() =>
			{
				if (DataCenter.IsSupport_TRT == true)
				{
					Opt_TensorRT.IsChecked = true;
				}
				else if (DataCenter.GPU_NVIDIA == true)
				{
					Opt_xformers.IsChecked = true;
				}
				else
				{
					DataCenter._MainWindow.Text_Load.Text = " Not Support";
				}
			}));
			#endregion
		}

		private async void Start_SD_Timer_Tick(object sender, EventArgs e)
		{
			if (Check_SD())
			{
				Start_SD_Timer.Stop();

				bool istrt = false;

				Dispatcher.Invoke(new Action(() =>
				{
					if (Opt_TensorRT.IsChecked == true)
					{
						istrt = true;
					}
					else
					{
						istrt = false;
					}
				}));

				if (istrt)
				{
					await API_SetModel(DataCenter.Url_API, "v1-5-pruned-emaonly.safetensors");
					await API_SetUnet(DataCenter.Url_API, "[TRT] TRT_Base");
				}
				else
				{
					await API_SetModel(DataCenter.Url_API, "v1-5-pruned-emaonly.safetensors");
				}

				Dispatcher.Invoke(new Action(() =>
				{
					Canvas_Loading.Visibility = Visibility.Collapsed;
				}));
				Story.Stop();
			}
		}

		internal bool Check_SD()
		{
			try
			{
				var response = SD.SendGetProgress();
				return true;
			}
			catch (Exception ex)
			{
				if (ex.ToString().Contains("Not Found"))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		#endregion

		#region ProssceTimer_Tick
		private void ProssceTimer_Tick(object sender, EventArgs e)
		{
			ProssceTimer_temp += 0.1;
			Seconds_text.Text = ProssceTimer_temp.ToString("0.0") + "s";
			Total_Second += 0.1;
		}
		#endregion

		#region Read Stable Diffusion Model
		internal void Read_Style(string input)
		{
			Style_list = new List<Style_data>();

			CB_Style.Items.Clear();

			if ((input == "pytorch") || (input == "xformers"))
			{
				try
				{
					string fileExtension = "*.safetensors";
					string[] fileNames = Directory.GetFiles(Path_SD + "\\models\\Stable-diffusion", fileExtension)
												  .Select(System.IO.Path.GetFileName)
												  .ToArray();

					foreach (string fileName in fileNames)
					{
						Style_data sd = new Style_data
						{
							path = fileName,
							deletable = true
						};
						if (fileName == "v1-5-pruned-emaonly.safetensors")
						{
							sd.displayName = "Default";
							sd.deletable = false;
							Style_list.Insert(0, sd);
						}
						else
						{
							string[] temp = fileName.Split('.');
							sd.displayName = temp[0];
							Style_list.Add(sd);
						}
					}

					foreach (var item in Style_list)
					{
						ComboBoxItem CBI = new ComboBoxItem
						{
							Tag = item.path,
							Content = item.displayName
						};
						CB_Style.Items.Add(CBI);
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error: " + ex.Message);
				}
			}
			else if (input == "trt")
			{
				ComboBoxItem CBI = new ComboBoxItem
				{
					Tag = "Default",
					Content = "Default"
				};
				CB_Style.Items.Add(CBI);
			}
			CB_Style.SelectedIndex = 0;
		}
		#endregion

		#region Change Style
		private async void CB_Style_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (((Opt_Pytorch.IsChecked == true) || (Opt_xformers.IsChecked == true)) && (Switch_opt != true))
			{
				Story.Begin();
				Canvas_Loading.Visibility = Visibility.Visible;
				Text_Load.Text = "Style Loading";

				if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
				{
					string selectedValue = comboBox.SelectedItem.ToString().Substring(38);

					if (selectedValue == "Default")
					{
						Btn_Style_Delete.IsEnabled = false;
						await API_SetModel(DataCenter.Url_API, "v1-5-pruned-emaonly.safetensors");
					}
					else
					{
						Btn_Style_Delete.IsEnabled = true;
						await API_SetModel(DataCenter.Url_API, selectedValue + ".safetensors");
					}
				}

				Canvas_Loading.Visibility = Visibility.Collapsed;
				Story.Stop();
			}
		}
		#endregion

		#region Read Lora
		internal void Read_Lora(string input)
		{
			Lora_list = new List<Lora_data>();

			CB_LoRA.Items.Clear();

			if (Directory.Exists(Path_SD_LoRA))
			{
				if ((input == "pytorch") || (input == "xformers"))
				{
					string fileExtension = "*.safetensors";
					string[] fileNames = Directory.GetFiles(Path_SD_LoRA, fileExtension)
												  .Select(System.IO.Path.GetFileName)
												  .ToArray();

					Lora_data ld_None = new Lora_data
					{
						displayName = "None",
						path = "",
						weight = "",
						changeable = false
					};
					Lora_list.Add(ld_None);

					foreach (string fileName in fileNames)
					{
						Lora_data ld = new Lora_data();

						string temp = fileName.Replace(".safetensors", "");

						if (temp == DataCenter.MSI_LoRA_NB)
						{
							ld.path = temp;
							ld.displayName = "All MSI";
							ld.weight = "0.7";
							ld.changeable = false;
							Lora_list.Insert(1, ld);
						}
						else if (temp == DataCenter.MSI_LoRA_CND_GNP)
						{
						}
						else
						{
							ld.path = temp;
							ld.displayName = temp;
							ld.weight = "1.0";
							ld.changeable = true;
							Lora_list.Add(ld);
						}
					}

					foreach (var item in Lora_list)
					{
						ComboBoxItem CBI = new ComboBoxItem();
						CBI.Tag = item.path;
						CBI.Content = item.displayName;
						CB_LoRA.Items.Add(CBI);
					}
				}
				else if (input == "trt")
				{
					string fileExtension = "*.safetensors";
					string[] fileNames = Directory.GetFiles(Path_SD_LoRA, fileExtension)
												  .Select(System.IO.Path.GetFileName)
												  .ToArray();

					ComboBoxItem cbi = new ComboBoxItem
					{
						Tag = "None",
						Content = "None"
					};
					CB_LoRA.Items.Add(cbi);

					foreach (string fileName in fileNames)
					{
						string temp = fileName.Replace(".safetensors", "");

						if (temp == DataCenter.MSI_LoRA_NB)
						{
							ComboBoxItem CBI2 = new ComboBoxItem
							{
								Tag = "All MSI",
								Content = "All MSI"
							};
							CB_LoRA.Items.Add(CBI2);
						}
						else if (temp == DataCenter.MSI_LoRA_CND_GNP)
						{}
					}
				}
			}
			CB_LoRA.SelectedIndex = 0;
		}
		#endregion

		#region Change LoRA
		private async void LoRA_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if ((Opt_TensorRT.IsChecked == true) && (Switch_opt != true))
			{
				Story.Begin();
				Canvas_Loading.Visibility = Visibility.Visible;
				DataCenter._MainWindow.Text_Load.Text = "Plugin Loading";

				if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
				{
					string selectedItem = ((ComboBoxItem)comboBox.SelectedItem).Content.ToString();

					if (selectedItem == "None")
					{
						await API_SetUnet(DataCenter.Url_API, "[TRT] TRT_Base");
					}
					else if (selectedItem == "All MSI")
					{
						await API_SetUnet(DataCenter.Url_API, "[TRT] TRT_AllMsi");
					}
				}

				Canvas_Loading.Visibility = Visibility.Collapsed;
				Story.Stop();
			}
			else if ((Opt_Pytorch.IsChecked == true) || (Opt_xformers.IsChecked == true) && (Switch_opt != true))
			{
				if (sender is ComboBox comboBox && comboBox.SelectedItem != null)
				{
					string selectedItem = ((ComboBoxItem)comboBox.SelectedItem).Content.ToString();

					if ((selectedItem != "None") && (selectedItem != "All MSI"))
					{
						Label_LoRA_Weight.Foreground = new SolidColorBrush(Colors.White);
						TextBox_LoRA_Weight.IsEnabled = true;
						Btn_LoRA_Delete.IsEnabled = true;
						TextBox_LoRA_Weight.Text = "1.0";
						Btn_LoRA_Delete.IsEnabled = true;
					}
					else
					{
						Label_LoRA_Weight.Foreground = new SolidColorBrush(Colors.Gray);
						TextBox_LoRA_Weight.IsEnabled = false;
						TextBox_LoRA_Weight.Text = "";
						Btn_LoRA_Delete.IsEnabled = false;
					}
				}
			}
		}
		#endregion

		#region Input Image
		private void Btn_ImportImg_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

			if (openFileDialog.ShowDialog() == true)
			{
				try
				{
					// Create a BitmapImage from the selected image file
					BitmapImage bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));

					// Set the BitmapImage as the source of the Image control
					Btn_ImportImg.Content = bitmapImage;

					Path_ImportImg = openFileDialog.FileName;

					Btn_RemoveImportImg.Visibility = Visibility.Visible;
					Btn_ImportImg_Text.Visibility = Visibility.Collapsed;
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void Btn_Remove_Image_Click(object sender, RoutedEventArgs e)
		{
			Btn_ImportImg.Content = null;
			Btn_RemoveImportImg.Visibility = Visibility.Collapsed;
			Btn_ImportImg_Text.Visibility = Visibility.Visible;
			Path_ImportImg = "";
		}
		#endregion

		#region Open Output Image Folder
		private void Btn_OpenOutputDir_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("explorer.exe", DataCenter.OutputImage_path);
		}
		#endregion

		#region SendToImportImg
		private void Btn_SendToImportImg_Click(object sender, RoutedEventArgs e)
		{
			if (Canvas_Pop_LargeImg.Visibility == Visibility.Collapsed)
			{
				if (Btn_OutputImg.Content != null)
				{
					Btn_ImportImg.Content = Btn_OutputImg.Content;
					Path_ImportImg = Btn_OutputImg.Content.ToString();
					Btn_ImportImg_Text.Visibility = Visibility.Collapsed;
					Btn_RemoveImportImg.Visibility = Visibility.Visible;
				}
			}
			else
			{
				Btn_OutputImg.Content = OutputImg_Large.Content;
				Btn_ImportImg.Content = OutputImg_Large.Content;
				Path_ImportImg = OutputImg_Large.Content.ToString();
				Btn_ImportImg_Text.Visibility = Visibility.Collapsed;

				if (StackPanel_OutputImg_Preview.Children.Count == 1 && StackPanel_OutputImg_Preview.Children[0] is RadioButton btn)
				{
					btn.IsChecked = true;
				}
				else if (StackPanel_OutputImg_Preview.Children.Count > 0 && StackPanel_OutputImg_Preview.Children[Num_SelectedImg + 1] is RadioButton btn2)
				{
					btn2.IsChecked = true;
				}
				Canvas_Pop_LargeImg.Visibility = Visibility.Collapsed;
				Btn_RemoveImportImg.Visibility = Visibility.Visible;
			}
		}
		#endregion

		#region Btn_OutputImg_Click PoP
		private void Btn_OutputImg_Click(object sender, RoutedEventArgs e)
		{
			Canvas_Pop_LargeImg.Visibility = Visibility.Visible;
			Btn_L.IsEnabled = true;
			Btn_R.IsEnabled = true;
			Btn_Replace_In.IsEnabled = true;

			if (Paths_OutputImg.Count == 1)
			{
				OutputImg_Large.Content = Paths_OutputImg[0];
				Btn_L.IsEnabled = false;
				Btn_R.IsEnabled = false;
			}
			else if (Num_SelectedImg == -1)
			{
				OutputImg_Large.Content = Path_OutputImg_Grid;
				Btn_L.IsEnabled = false;
			}
			else
			{
				OutputImg_Large.Content = Paths_OutputImg[Num_SelectedImg];
				if ((Paths_OutputImg.Count - 1) == Num_SelectedImg)
				{
					Btn_R.IsEnabled = false;
				}
			}
		}
		#endregion

		#region Pop Close
		private void Btn_pop_close_Click(object sender, RoutedEventArgs e)
		{
			Canvas_Pop_LargeImg.Visibility = Visibility.Collapsed;

			if (StackPanel_OutputImg_Preview.Children.Count == 1 && StackPanel_OutputImg_Preview.Children[0] is RadioButton btn)
			{
				btn.IsChecked = true;
			}
			else if (StackPanel_OutputImg_Preview.Children.Count > 0 &&
				StackPanel_OutputImg_Preview.Children[Num_SelectedImg + 1] is RadioButton btn2)
			{
				btn2.IsChecked = true;
			}
		}
		#endregion

		#region btn_L / btn_R
		private void Btn_L_Click(object sender, RoutedEventArgs e)
		{
			Num_SelectedImg--;

			if (Num_SelectedImg == -1)
			{
				OutputImg_Large.Content = Path_OutputImg_Grid;
				Btn_L.IsEnabled = false;
			}
			else
			{
				OutputImg_Large.Content = Paths_OutputImg[Num_SelectedImg];
			}
			Btn_R.IsEnabled = true;
		}

		private void Btn_R_Click(object sender, RoutedEventArgs e)
		{
			Num_SelectedImg++;

			OutputImg_Large.Content = Paths_OutputImg[Num_SelectedImg];
			if ((Paths_OutputImg.Count - 1) == Num_SelectedImg)
			{
				Btn_R.IsEnabled = false;
			}
			Btn_L.IsEnabled = true;
		}
		#endregion

		#region PSD
		private async void Btn_OutputPSD_Click(object sender, RoutedEventArgs e)
		{
			if (Btn_OutputImg.Content != null)
			{
				SaveFileDialog saveFileDialog = new SaveFileDialog();
				saveFileDialog.Filter = "PSD files (*.psd)|*.psd|All files (*.*)|*.*";
				saveFileDialog.DefaultExt = ".psd";
				saveFileDialog.FileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".psd";//"temp.psd";

				if (saveFileDialog.ShowDialog() == true)
				{
					Story.Begin();
					Canvas_Loading.Visibility = Visibility.Visible;
					DataCenter._MainWindow.Text_Load.Text = "Save PSD";

					string path = "";

					if (Canvas_Pop_LargeImg.Visibility == Visibility.Visible)
					{
						path = OutputImg_Large.Content.ToString();
					}
					else
					{
						path = Btn_OutputImg.Content.ToString();
					}

					if (sender is Button btn)
					{
						if (btn.Name == "Btn_PSD_GPU")
						{
							await Task.Run(() =>
							{
								DataCenter._Install.Close_Process();

								Save_PSD(path, saveFileDialog.FileName, "cuda");

								Dispatcher.Invoke(new Action(() =>
								{
									RoutedEventArgs newEvent = new RoutedEventArgs(RadioButton.CheckedEvent);

									if (Opt_Pytorch.IsChecked == true)
									{
										Opt_Pytorch.RaiseEvent(newEvent);
									}
									else if (Opt_xformers.IsChecked == true)
									{
										Opt_xformers.RaiseEvent(newEvent);
									}
									else if (Opt_TensorRT.IsChecked == true)
									{
										Opt_TensorRT.RaiseEvent(newEvent);
									}
								}));
							});
						}
						else
						{
							await Task.Run(() =>
							{
								Save_PSD(path, saveFileDialog.FileName, "cpu");
							});

							Canvas_Loading.Visibility = Visibility.Collapsed;
							Story.Stop();
						}
					}
				}
			}
		}

		private void Save_PSD(string input_path, string output_path, string device)
		{
			var processInfo = new ProcessStartInfo("cmd.exe")
			{
				CreateNoWindow = DataCenter.HideCMD,
				UseShellExecute = false,
				RedirectStandardInput = true,
				WorkingDirectory = DataCenter.Path_App
			};

			Process process = Process.Start(processInfo);
			process.StandardInput.WriteLine($"venv\\Scripts\\activate");
			process.StandardInput.WriteLine($"cd SAM_PSD");
			process.StandardInput.WriteLine($"python SAM_V1.py --device " + device + " --image \"" + input_path +
				"\" --output \"" + output_path + "\"");
			process.StandardInput.WriteLine("exit");
			process.WaitForExit();
		}
		#endregion

		#region Catch_PNG
		private const double defaultDpi = 96.0;

		public static ImageSource RenderToPNGImageSource(Visual targetControl)
		{
			var renderTargetBitmap = GetRenderTargetBitmapFromControl(targetControl);

			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

			var result = new BitmapImage();

			using (var memoryStream = new MemoryStream())
			{
				encoder.Save(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);

				result.BeginInit();
				result.CacheOption = BitmapCacheOption.OnLoad;
				result.StreamSource = memoryStream;
				result.EndInit();
			}

			return result;
		}

		public static void RenderToPNGFile(Visual targetControl, string filename)
		{
			var renderTargetBitmap = GetRenderTargetBitmapFromControl(targetControl);

			var encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(renderTargetBitmap));

			var result = new BitmapImage();

			try
			{
				using (var fileStream = new FileStream(filename, FileMode.Create))
				{
					encoder.Save(fileStream);
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"There was an error saving the file: {ex.Message}");
			}
		}

		private static BitmapSource GetRenderTargetBitmapFromControl(Visual targetControl, double dpi = defaultDpi)
		{
			if (targetControl == null) return null;

			var bounds = VisualTreeHelper.GetDescendantBounds(targetControl);
			var renderTargetBitmap = new RenderTargetBitmap((int)(bounds.Width * dpi / 96.0),
															(int)(bounds.Height * dpi / 96.0),
															dpi,
															dpi,
															PixelFormats.Pbgra32);

			var drawingVisual = new DrawingVisual();

			using (var drawingContext = drawingVisual.RenderOpen())
			{
				var visualBrush = new VisualBrush(targetControl);
				drawingContext.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));
			}

			renderTargetBitmap.Render(drawingVisual);
			return renderTargetBitmap;
		}
		#endregion

		#region Delete File
		private void Btn_Style_Delete_Click(object sender, RoutedEventArgs e)
		{
			Canvas_Delete.Visibility = Visibility.Visible;

			foreach (var style in Style_list)
			{
				string selectedItem = ((ComboBoxItem)CB_Style.SelectedItem).Content.ToString();

				if (selectedItem == style.displayName)
				{
					Delete_File_Path = style.path;
				}
			}
			Delete_Is_Style = true;
		}

		private void Btn_LoRA_Delete_Click(object sender, RoutedEventArgs e)
		{
			Canvas_Delete.Visibility = Visibility.Visible;

			foreach (var lora in Lora_list)
			{
				string selectedItem = ((ComboBoxItem)CB_LoRA.SelectedItem).Content.ToString();

				if (selectedItem == lora.displayName)
				{
					Delete_File_Path = lora.path + ".safetensors";
				}
			}

			Delete_Is_Style = false;
		}

		private void Btn_Pop_Yes_Click(object sender, RoutedEventArgs e)
		{
			Canvas_Delete.Visibility = Visibility.Collapsed;
			if (Delete_Is_Style)
			{
				Text_Load.Text = "Style Loading";
				File.Delete(DataCenter.Path_SD + "\\models\\Stable-diffusion\\" + Delete_File_Path);
				Read_Style("xformers");
				Btn_Style_Delete.IsEnabled = false;
			}
			else
			{
				Text_Load.Text = "Plugin Loading";
				File.Delete(DataCenter.Path_SD + "\\models\\Lora\\" + Delete_File_Path);
				Read_Lora("xformers");
				Btn_LoRA_Delete.IsEnabled = false;
			}
		}

		private void Btn_Pop_No_Click(object sender, RoutedEventArgs e)
		{
			Canvas_Delete.Visibility = Visibility.Collapsed;
		}
		#endregion

		#region Tooltip
		private void Open_Tootip(object sender, MouseEventArgs e)
		{
			if (sender is Button btn && btn.Tag != null)
			{
				switch (btn.Tag.ToString())
				{
					case "Dir":
						TextBlock_Tooltip.Text = "Open saved folder";
						Canvas_Tooltip.Margin = new Thickness(880, 865, 0, 0);
						break;

					case "Replace":
						TextBlock_Tooltip.Text = "Import as reference image";
						Canvas_Tooltip.Margin = new Thickness(1055, 865, 0, 0);
						break;

					case "PSD":
						TextBlock_Tooltip.Text = "Generate layered PSD";
						Canvas_Tooltip.Margin = new Thickness(1230, 865, 0, 0);
						break;

					case "Dir_In":
						TextBlock_Tooltip.Text = "Open saved folder";
						Canvas_Tooltip.Margin = new Thickness(442, 860, 0, 0);
						break;

					case "Replace_In":
						TextBlock_Tooltip.Text = "Import as reference image";
						Canvas_Tooltip.Margin = new Thickness(642, 860, 0, 0);
						break;

					case "PSD_In":
						TextBlock_Tooltip.Text = "Generate layered PSD";
						Canvas_Tooltip.Margin = new Thickness(842, 860, 0, 0);
						break;
					default:
						break;
				}
			}
			Canvas_Tooltip.Visibility = Visibility.Visible;
		}

		private void Close_Tootip(object sender, MouseEventArgs e)
		{
			Canvas_Tooltip.Visibility = Visibility.Collapsed;
		}
		#endregion
	}
}
