using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Shapes;

namespace AI_Artist_V2
{
	internal class Install
	{
		Process process_start = null;

		List<int> target_processID = null;

		#region Check_Python_Git_SD_TensorRT
		public bool Check_Python_Git_SD()
		{
			DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
			{
				DataCenter._MainWindow.Text_Load.Text = "Install package";
			}));

			if (DataCenter.GPU_NVIDIA == true)
			{
				Install_SD();

				if (!File.Exists(DataCenter.Path_SD + "\\webui-user.bat"))
				{
					Logger.Log("Clone SD Fail");
					Process_Retry();
				}

				Copy_LoRA();
			}

			if ((DataCenter.GPU_NVIDIA == true) && (DataCenter.IsSupport_TRT == true))
			{
				Install_TesnsoRT_extension();
				Install_TensorRT();
				Firist_Switch_TRT();
			}

			if (!Directory.Exists(DataCenter.Path_BLIP))
			{
				Directory.CreateDirectory(DataCenter.Path_BLIP);
				Install_BLIP();
			}
			if (!File.Exists(DataCenter.Path_Resource + "\\XSAM.txt"))
			{
				Install_SAM();
			}

			try
			{
				Logger.Log("Close python...");
				Process[] process = Process.GetProcessesByName("python");
				foreach (var item in process)
				{
					item.Kill();
				}
			}
			catch (Exception ex)
			{
				Logger.Log("Close python " + ex.ToString());
			}

			return true;
		}
		#endregion

		#region Install BLIP
		private void Install_BLIP()
		{
			Logger.Log("Install BLIP...");
			DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
			{
				DataCenter._MainWindow.Text_Load.Text = "Installing BLIP";
			}));
			List<string> list_Datas = new List<string>()
			{
				$"python -m venv venv",
				$"venv\\Scripts\\activate",
				$"pip install Pillow",
				$"cd venv\\Scripts",
				$"python.exe -m pip install --upgrade pip",
				$"cd..",
				$"cd..",
				$"pip install torch==1.13.1",
				$"pip install torchvision==0.14.1",
				$"pip install timm==0.4.12",
				$"pip install transformers",
				$"pip install fairscale==0.4.4",
				$"pip install pycocoevalcap",
			};

			if (File.Exists(DataCenter.Path_Resource + "\\BLIP\\" + DataCenter.BLIP_Name))
			{
				Directory.CreateDirectory(DataCenter.Path_App + "\\BLIP\\CheckPoints\\");
				File.Move(DataCenter.Path_Resource + "\\BLIP\\" + DataCenter.BLIP_Name,
					DataCenter.Path_App + "\\BLIP\\CheckPoints\\" + DataCenter.BLIP_Name);
			}
			else
			{
				list_Datas.Add($"cd BLIP\\CheckPoints");
				list_Datas.Add("curl -L -o \"" + DataCenter.BLIP_Name + "\" \"https://storage.googleapis.com/sfr-vision-language-research/BLIP/models/model_base_caption_capfilt_large.pth\"");//model_base_capfilt_large
			}

			list_Datas.Add("exit");

			ProcessCmd(list_Datas, DataCenter.Path_App);
		}

		#endregion Install BLIP

		#region Install SAM
		private void Install_SAM()
		{
			Logger.Log("Checking for SAM...");
			string path = DataCenter.Path_App + "\\SAM_PSD\\sam_vit_h_4b8939.pth";

			if (!File.Exists(path))
			{
				Pre_install_PSD();

				DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
				{
					DataCenter._MainWindow.Text_Load.Text = "SAM Downloading";
				}));

				if (File.Exists(DataCenter.Path_Resource + "\\SAM\\" + DataCenter.SAM_Name))
				{
					File.Move(DataCenter.Path_Resource + "\\SAM\\" + DataCenter.SAM_Name,
						DataCenter.Path_App + "\\SAM_PSD\\" + DataCenter.SAM_Name);
				}
				else
				{
					Logger.Log("Download SAM...");

					List<string> list_Datas = new List<string>()
					{
						"curl -L -o \"" + DataCenter.SAM_Name + "\" \"https://dl.fbaipublicfiles.com/segment_anything/sam_vit_h_4b8939.pth\"",
						"exit"
					};
					ProcessCmd(list_Datas, DataCenter.Path_App + "\\SAM_PSD");
				}
			}
			else
			{
				Logger.Log("SAM already installed.");
			}
		}
		#endregion

		#region Install PSD
		private bool Pre_install_PSD()
		{
			Logger.Log("Install PSD...");
			DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
			{
				DataCenter._MainWindow.Text_Load.Text = "Installing PSD";
			}));

			List<string> list_Datas = new List<string>()
			{
				$"venv\\Scripts\\activate",
				$"pip install torch==1.13.1",
				$"pip install torchvision==0.14.1",  //for cpu
				$"pip install opencv-python",
				$"pip install pycocotools",
				$"pip install pytoshop==1.1.0 -I --no-cache-dir",
				$"pip install segment_anything",
				"exit"
			};
			ProcessCmd(list_Datas, DataCenter.Path_App);

			return true;
		}
		#endregion

		#region Install SD
		private bool Install_SD()
		{
			Logger.Log("Checking for Stable Diffusion...");
			if (!File.Exists(DataCenter.Path_SD + "\\webui.bat"))
			{
				Logger.Log("Stable Diffusion not found. Cloning Git repository...");

				DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
				{
					DataCenter._MainWindow.Text_Load.Text = "SD installing";
				}));

				List<string> list_Datas = new List<string>()
				{
					$"git clone https://github.com/AUTOMATIC1111/stable-diffusion-webui.git",
					"exit"
				};
				ProcessCmd(list_Datas, DataCenter.Path_App);
			}
			Logger.Log($"SD installed in {DataCenter.Path_SD}.");
			return true;
		}
		#endregion

		#region Install TesnsoRT extension
		private bool Install_TesnsoRT_extension()
		{
			Logger.Log("Checking for TesnsoRT extension...");
			if (!Directory.Exists(DataCenter.Path_SD_TRT) && File.Exists(DataCenter.Path_SD + "\\webui-user.bat"))
			{
				Logger.Log("TesnsoRT extension not found. Cloning Git repository...");

				DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
				{
					DataCenter._MainWindow.Text_Load.Text = "TRT ex installing";
				}));

				Directory.CreateDirectory(DataCenter.Path_SD_TRT);

				List<string> list_Datas = new List<string>()
				{
					"cd..",
					$"git clone https://github.com/AUTOMATIC1111/stable-diffusion-webui-tensorrt.git",
					"exit",
				};
				ProcessCmd(list_Datas, DataCenter.Path_SD_TRT);
				Logger.Log($"TesnsoRT extension installed in {DataCenter.Path_SD_TRT}.");
			}
			else
			{
				Logger.Log($"TesnsoRT extension ok");
			}
			return true;
		}
		#endregion

		#region Install LoRA
		private bool Copy_LoRA()
		{
			//===================================================
			Logger.Log("Checking for LoRA...");
			if (!Directory.Exists(DataCenter.Path_SD_LoRA))
			{
				Logger.Log("LoRA directory not found.");
				Directory.CreateDirectory(DataCenter.Path_SD_LoRA);
			}
			try
			{
				#region LoRA
				//
				if (!File.Exists(DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_NB + ".safetensors") &&
					File.Exists(DataCenter.Path_Resource + "\\" + DataCenter.MSI_LoRA_NB + ".safetensors"))
				{
					Logger.Log("LoRA NB Copy...");
					File.Move(DataCenter.Path_Resource + "\\" + DataCenter.MSI_LoRA_NB + ".safetensors",
						DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_NB + ".safetensors");
					Logger.Log("LoRA NB OK");
				}
				else if (!File.Exists(DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_NB + ".safetensors") &&
					!File.Exists(DataCenter.Path_Resource + "\\" + DataCenter.MSI_LoRA_NB + ".safetensors"))
				{
					var client = new WebClient();
					client.DownloadFile("https://huggingface.co/MSI-AI-Group/MSI_NB/resolve/main/MSI%20NB.safetensors?download=true", DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_NB + ".safetensors");
					Logger.Log("Download MSI NB.safetensors");
				}

				if (!File.Exists(DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_CND_GNP + ".safetensors") &&
					File.Exists(DataCenter.Path_Resource + "\\" + DataCenter.MSI_LoRA_CND_GNP + ".safetensors"))
				{
					Logger.Log("LoRA CND Copy...");
					File.Move(DataCenter.Path_Resource + "\\" + DataCenter.MSI_LoRA_CND_GNP + ".safetensors",
						DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_CND_GNP + ".safetensors");
					Logger.Log("LoRA CND OK");
				}
				else if (!File.Exists(DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_CND_GNP + ".safetensors") &&
					!File.Exists(DataCenter.Path_Resource + "\\" + DataCenter.MSI_LoRA_CND_GNP + ".safetensors"))
				{
					var client = new WebClient();
					client.DownloadFile("https://huggingface.co/MSI-AI-Group/MSI_CND_GNP/resolve/main/MSI%20CND%20GNP.safetensors?download=true", DataCenter.Path_SD_LoRA + DataCenter.MSI_LoRA_CND_GNP + ".safetensors");
					Logger.Log("Download MSI CND GNP.safetensors");
				}
				
				#endregion
			}
			catch (Exception ex)
			{
				Logger.Log("LoRA Copy Fail! " + ex.Message);
			}

			//===================================================
			Logger.Log("Checking for ckpt base...");
			if (!Directory.Exists(DataCenter.Path_SD_Ckpt))
			{
				Logger.Log("ckpt base not found.");
				Directory.CreateDirectory(DataCenter.Path_SD_Ckpt);
			}
			try
			{
				#region ckpt base
				if (!File.Exists(DataCenter.Path_SD_Ckpt + "v1-5-pruned-emaonly.safetensors") &&
					File.Exists(DataCenter.Path_Resource + "\\ckpt\\v1-5-pruned-emaonly.safetensors"))
				{
					Logger.Log("Ckpt base Copy...");
					File.Move(DataCenter.Path_Resource + "\\ckpt\\v1-5-pruned-emaonly.safetensors",
					DataCenter.Path_SD_Ckpt + "v1-5-pruned-emaonly.safetensors");
					Logger.Log("Ckpt base OK");
				}
				#endregion
			}
			catch (Exception ex)
			{
				Logger.Log("Ckpt base Copy Fail! " + ex.Message);
			}
			return true;
		}
		#endregion

		#region Install TensorRT
		private bool Install_TensorRT()
		{
			Logger.Log("Checking for TensorRT...");
			if (!Directory.Exists(DataCenter.Path_SD_TRT_SDK))
			{
				if (!File.Exists(DataCenter.Path_SD_TRT + "\\" + DataCenter.TRT_SDK_zip))
				{
					DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
					{
						DataCenter._MainWindow.Text_Load.Text = "TRT Installing";
					}));
					Logger.Log("Installing TRT...");

					List<string> list_Datas = new List<string>();
					list_Datas.Add("cd /d " + DataCenter.Path_SD_TRT);
					if (File.Exists(DataCenter.Path_Resource + "\\" + DataCenter.TRT_SDK_zip))
					{
						File.Move(DataCenter.Path_Resource + "\\" + DataCenter.TRT_SDK_zip,
							DataCenter.Path_SD_TRT + "\\" + DataCenter.TRT_SDK_zip);
					}
					else
					{
						list_Datas.Add("curl -L -o \"" + DataCenter.TRT_SDK_zip + "\" \"https://developer.nvidia.com/downloads/compute/machine-learning/tensorrt/secure/8.6.1/zip/TensorRT-8.6.1.6.Windows10.x86_64.cuda-11.8.zip\"");
					}
					list_Datas.Add("tar -xf \"" + DataCenter.TRT_SDK_zip + "\"");
					list_Datas.Add("del \"" + DataCenter.TRT_SDK_zip + "\"");
					list_Datas.Add("exit");

					ProcessCmd(list_Datas, DataCenter.Path_App);
				}
				Logger.Log("TRT installed!");
			}
			else
			{
				Logger.Log("TRT already installed.");
			}
			return true;
		}
		#endregion

		#region Switch TRT
		private void Firist_Switch_TRT()
		{
			if (File.Exists(DataCenter.Path_SD + "\\webui-user.bat"))
			{
				if (!File.Exists(DataCenter.Path_SD + "\\models\\Unet-trt\\TRT_Base.trt"))
				{

					DataCenter._MainWindow.Dispatcher.Invoke(new Action(() =>
					{
						DataCenter._MainWindow.Text_Load.Text = "Switch TRT";
					}));

					#region Get Process
					Process[] pid_Console = Process.GetProcessesByName("conhost");
					Process[] pid_Python = Process.GetProcessesByName("python");
					#endregion

					OpenStableDiffusion(true);

					#region Get new Process
					Thread.Sleep(10000);

					Process[] pid_Console_new = Process.GetProcessesByName("conhost");
					Process[] pid_Python_new = Process.GetProcessesByName("python");

					FindProcess(pid_Console, pid_Console_new, ref target_processID);
					FindProcess(pid_Python, pid_Python_new, ref target_processID);
					#endregion

					while (!DataCenter._MainWindow.Check_SD())
					{
						Thread.Sleep(1000);
					}

					var processInfo = new ProcessStartInfo("cmd.exe")
					{
						CreateNoWindow = DataCenter.HideCMD,
						UseShellExecute = false,
						RedirectStandardInput = true,
					};

					Process process = Process.Start(processInfo);
					process.StandardInput.WriteLine("cd /d " + DataCenter.Path_SD);
					process.StandardInput.WriteLine("git checkout dev");
					process.StandardInput.WriteLine("cd /d " + DataCenter.Path_SD + "\\venv\\Scripts");
					process.StandardInput.WriteLine("activate.bat");

					string newpath = DataCenter.Path_SD + @"\venv\lib\site-packages\torch\lib";
					string pathvariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
					pathvariable = newpath + ";" + pathvariable;
					Environment.SetEnvironmentVariable("PATH", pathvariable, EnvironmentVariableTarget.Process);

					newpath = DataCenter.Path_SD + @"\extensions\stable-diffusion-webui-tensorrt\TensorRT-8.6.1.6\lib";
					pathvariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
					pathvariable = newpath + ";" + pathvariable;
					Environment.SetEnvironmentVariable("PATH", pathvariable, EnvironmentVariableTarget.Process);


					process.StandardInput.WriteLine("set PATH=%PATH%;" + DataCenter.Path_SD + "\\venv\\lib\\site-packages\\torch\\lib");
					process.StandardInput.WriteLine("set PATH=%PATH%;" + DataCenter.Path_SD + "\\extensions\\stable-diffusion-webui-tensorrt\\TensorRT-8.6.1.6\\lib");

					process.StandardInput.WriteLine("exit");
					process.WaitForExit();

					Close_Process();

					#region TRT Download
					Directory.CreateDirectory(DataCenter.Path_SD + "\\models\\Unet-trt");

					/* TRT Base */
					if (!File.Exists(DataCenter.Path_SD + "\\models\\Unet-trt\\TRT_Base.trt"))
					{
						if (File.Exists(DataCenter.Path_Resource + "\\Unet-trt\\TRT_Base.trt"))
						{
							File.Move(DataCenter.Path_Resource + "\\Unet-trt\\TRT_Base.trt",
							DataCenter.Path_SD + "\\models\\Unet-trt\\TRT_Base.trt");
						}
						else
						{
							var client = new WebClient();
							string downloadpath = "https://huggingface.co/MSI-AI-Group/TRT_Base_" + DataCenter.GPU_Model + "/resolve/main/TRT_Base_" + DataCenter.GPU_Model + ".trt?download=true";
							client.DownloadFile(downloadpath, DataCenter.Path_SD + "\\models\\Unet-trt\\TRT_Base.trt");
							Logger.Log("Download TRT_Base_" + DataCenter.GPU_Model + ".trt");
						}
					}
					///* TRT AllMsi */
					if (!File.Exists(DataCenter.Path_SD + "\\models\\Unet-trt\\TRT_AllMsi.trt"))
					{
						if (File.Exists(DataCenter.Path_Resource + "\\Unet-trt\\TRT_AllMsi.trt"))
						{
							File.Move(DataCenter.Path_Resource + "\\Unet-trt\\TRT_AllMsi.trt",
							DataCenter.Path_SD + "\\models\\Unet-trt\\TRT_AllMsi.trt");
						}
						else
						{
							var client = new WebClient();
							string downloadpath = "https://huggingface.co/MSI-AI-Group/TRT_AllMsi_" + DataCenter.GPU_Model + "/resolve/main/TRT_AllMsi_" + DataCenter.GPU_Model + ".trt?download=true";
							client.DownloadFile(downloadpath, DataCenter.Path_SD + "\\models\\Unet-trt\\TRT_AllMsi.trt");
							Logger.Log("Download TRT_AllMsi_" + DataCenter.GPU_Model + ".trt");
						}
					}
					#endregion
				}
			}
		}
		#endregion

		#region Get_Python_Path
		internal string Get_Python_Path()
		{
			string pythonPath = null;
			string keyName = @"Software\Python\PythonCore";

			RegistryKey registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

			using (RegistryKey pythonCoreKey = registry.OpenSubKey(keyName))
			{
				if (pythonCoreKey != null)
				{
					string[] subKeyNames = pythonCoreKey.GetSubKeyNames();

					foreach (string subKeyName in subKeyNames)
					{
						using (RegistryKey versionKey = registry.OpenSubKey(keyName + "\\" + subKeyName + "\\InstallPath"))
						{
							pythonPath = versionKey?.GetValue("ExecutablePath")?.ToString();
							break;
						}
					}
				}
			}

			return pythonPath;
		}

		#endregion

		#region Get_Git_Path
		internal string Get_Git_Path()
		{
			try
			{
				RegistryKey registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
				string reg_path = @"SOFTWARE\GitForWindows";
				registry = registry.OpenSubKey(reg_path, false);

				string temp = (string)registry.GetValue("InstallPath", "");

				return temp;
			}
			catch (Exception ex)
			{
				Logger.Log("Get_Git_Path: " + ex.ToString());
				return "";
			}
		}
		#endregion

		#region Open Stable Diffusion
		internal bool OpenStableDiffusion(bool xformers = false)
		{
			string commandlineArgs = xformers ? "--api --xformers" : "--api";
#if DEBUG
			//commandlineArgs += " --no-download-sd-model";
#endif

			// Run the webui.bat file
			Logger.Log($"Opening Stable Diffusion{(xformers ? " with Xformers" : "")}.");

			ExecuteCommand_SD($"{commandlineArgs}");
			return true;
		}
		#endregion

		#region Execute Command
		private void ExecuteCommand_SD(string command)
		{
			Logger.Log($"Running command: webui.bat {command}");

			string path = DataCenter.Path_SD + "\\webui-user.bat";

			string context = "";
			context += "@echo off\n";
			context += "\n";
			context += "set PYTHON=\n";
			context += "set GIT=\n";
			//context += "set PYTHON=\"" + Get_Python_Path() + "\"\n";
			//context += "set GIT=" + Get_Git_Path() + "\\cmd\\git.exe" + "\n";
			context += "set VENV_DIR=\n";
			context += "set COMMANDLINE_ARGS=" + command + " --port 9860\n";
			//--medvram
			context += "set SD_WEBUI_RESTARTING=1\r\n\n";
			context += "\n";
			context += "call webui.bat\n";


			using (FileStream fs = File.Create(path))
			{
				byte[] info = new UTF8Encoding(true).GetBytes(context);
				fs.Write(info, 0, info.Length);
			}

			DataCenter.SetAccessRights(path);

			var processInfo = new ProcessStartInfo(DataCenter.Path_SD + "\\webui-user.bat")
			{
				CreateNoWindow = DataCenter.HideCMD,
				UseShellExecute = false,
				WorkingDirectory = DataCenter.Path_SD,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
			};

			target_processID = new List<int>();

			Process[] pid_Console = Process.GetProcessesByName("conhost");
			Process[] pid_Python = Process.GetProcessesByName("python");

			process_start = Process.Start(processInfo);
			process_start.OutputDataReceived += Process_OutputDataReceived;
			process_start.ErrorDataReceived += Process_ErrorDataReceived;
			process_start.BeginOutputReadLine();
			process_start.BeginErrorReadLine();

			Thread.Sleep(10000);

			#region Get process
			Process[] pid_Console_new = Process.GetProcessesByName("conhost");
			Process[] pid_Python_new = Process.GetProcessesByName("python");

			FindProcess(pid_Console, pid_Console_new, ref target_processID);
			FindProcess(pid_Python, pid_Python_new, ref target_processID);
			#endregion
		}
		#endregion

		#region Close Process
		internal void Close_Process()
		{
			Logger.Log("Close Process");
			if (target_processID != null)
			{
				foreach (int id in target_processID)
				{
					try
					{
						Process process = Process.GetProcessById(id);
						process.Kill();
					}
					catch (Exception ex)
					{
						Logger.Log(ex.ToString());
					}
				}
			}
		}
		#endregion
		Process ProcessCMD = new Process();

		public void ProcessCmd(List<string> arr_Datas, string str_WorkDir)
		{
			ProcessCMD = new Process();
			ProcessCMD.StartInfo.FileName = "cmd.exe";
			ProcessCMD.StartInfo.RedirectStandardInput = true;
			ProcessCMD.StartInfo.RedirectStandardOutput = true;
			ProcessCMD.StartInfo.RedirectStandardError = true;
			ProcessCMD.StartInfo.UseShellExecute = false;
			ProcessCMD.StartInfo.CreateNoWindow = true;
			ProcessCMD.StartInfo.WorkingDirectory = str_WorkDir;
			ProcessCMD.OutputDataReceived += Process_OutputDataReceived;
			ProcessCMD.ErrorDataReceived += Process_ErrorDataReceived;
			ProcessCMD.Start();
			ProcessCMD.BeginOutputReadLine();
			ProcessCMD.BeginErrorReadLine();

			StreamWriter sw = ProcessCMD.StandardInput;
			for (int i = 0; i < arr_Datas.Count; i++)
			{
				sw.WriteLine(arr_Datas[i]);
			}

			ProcessCMD.WaitForExit();
			ProcessCMD.Close();
		}

		private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				Logger.Log_CMD("OutputData : " + e.Data);
				if (e.Data.Contains("fatal: fetch-pack:") || e.Data.Contains("RuntimeError: Couldn't clone") || e.Data.Contains("fatal: unable to access"))
				{
					Process_Retry();
				}
			}
		}

		private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (!string.IsNullOrEmpty(e.Data))
			{
				Logger.Log_CMD("ErrorData : " + e.Data);
				if (e.Data.Contains("fatal: fetch-pack:") || e.Data.Contains("RuntimeError: Couldn't clone") || e.Data.Contains("fatal: unable to access") || e.Data.Contains(": 128"))
				{
					Process_Retry();
				}
			}
		}
		private void Process_Retry()
		{
			Logger.Log_CMD("Process_Retry ");
			if (DataCenter.Retrytime > 2)
			{
				Logger.Log_CMD("Process_Retry Error Close App");
				if (process_start != null)
				{
					Close_Process();
				}
				if (ProcessCMD != null)
				{
					ProcessCMD.Close();
				}
				Environment.Exit(0);

			}
			else
			{
				DataCenter.Retrytime += 1;
				Logger.Log_CMD("Process_Retry :" + DataCenter.Retrytime);


				if (process_start != null)
				{
					process_start.Close();
				}
				if (ProcessCMD != null)
				{
					ProcessCMD.Close();
				}
				Close_Process();
				try
				{
					Logger.Log_CMD("Delete : SD");
					if (Directory.Exists(DataCenter.Path_SD))
					{
						Directory.Delete(DataCenter.Path_SD, true);
					}
				}
				catch (Exception ex)
				{
					Logger.Log_CMD("Directory.Delete fail: " + ex);
				}

				Thread.Sleep(60000);

				Check_Python_Git_SD();
			}
		}

		private void FindProcess(Process[] oldPs, Process[] newPs, ref List<int> addList)
		{
			foreach (Process item_new in newPs)
			{
				bool found = false;
				foreach (Process item in oldPs)
				{
					if (item_new.Id == item.Id)
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					addList.Add(item_new.Id);
				}
			}
		}
	}
}
