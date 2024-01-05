using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TaskScheduler;
using static System.Collections.Specialized.BitVector32;

namespace AI_Artist_Install_Tool
{
	internal class Program
	{
		internal static string ExeFolder = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);

		internal static string BuildTools_path = ExeFolder + "\\Tool";
		internal static string LogFilePath
		{
			get
			{
				var folder = Path.Combine(ExeFolder, "AI_Artist_Install_log");

				if (Directory.Exists(folder) == false)
					Directory.CreateDirectory(folder);

				return Path.Combine(folder, $"AI_Artist_{DateTime.Now:yyyy-MM-dd}.txt");
			}
		}

#if DEBUG
        internal static bool HideCMD = false;
#else
		internal static bool HideCMD = true;
#endif

		static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				if (args[0] == "-debug")
				{
					HideCMD = false;
				}
			}

			#region Install package

			Install_python();

			Install_git();

			Install_BuildTools();

			try
			{
				CheckHW.GetGPUInfo();
				if (CheckHW.GetGPU_NVIDIA())
				{
					var processInfo = new ProcessStartInfo("cmd.exe")
					{
						CreateNoWindow = HideCMD,
						UseShellExecute = false,
						RedirectStandardInput = true,
						RedirectStandardOutput = true,
						WindowStyle = ProcessWindowStyle.Hidden
					};
					Process process = Process.Start(processInfo);
					process.StandardInput.WriteLine("Get_GPU_Memory.exe");
					process.StandardInput.WriteLine("exit");
					process.WaitForExit();
					process.Close();

					if (File.Exists("GPUtotalMemory.txt"))
					{
						string output = System.IO.File.ReadAllText("GPUtotalMemory.txt");
						int num = int.Parse(output) / 1000;
						if (num >= 6)
						{
							Install_cuda(1);
							Install_cuda(2);
							Install_cuda(3);
						}
						else
						{
							Log("VRAM < 6");
						}
					}
					else
					{
						Log("No GPUtotalMemory");
					}
				}
				else
				{
					Log("Not NVIDIA");
				}
			}
			catch (Exception ex)
			{
				Log(ex.ToString());
			}
			#endregion

			Install_Model();

			Thread.Sleep(1000);

			Process[] process_check = Process.GetProcessesByName("AI_Install_Model");
			while (process_check.Length > 0)
			{
				Thread.Sleep(1000);
				process_check = Process.GetProcessesByName("AI_Install_Model");
			}
		}

		#region run install model
		internal static void Install_Model()
		{
			string sUser = "";
			var suserre = WindowsBuiltInRole.User.ToString();
			SecurityIdentifier sidUser = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			sUser = sidUser.Translate(typeof(NTAccount)).ToString();
			sUser = sUser.Substring(sUser.IndexOf("\\") + 1);

			ITaskService taskService = new TaskScheduler.TaskScheduler();
			taskService.Connect();
			ITaskFolder rootFolder = taskService.GetFolder(@"\");
			IRegisteredTaskCollection tasks = rootFolder.GetTasks(1);
			ITaskDefinition taskDefinition = taskService.NewTask(0);

			taskDefinition.RegistrationInfo.Author = "MSI";
			taskDefinition.RegistrationInfo.Description = "AI_Install_Model";
			taskDefinition.Principal.RunLevel = _TASK_RUNLEVEL.TASK_RUNLEVEL_LUA;
			//taskDefinition.Principal.GroupId = string.Concat(GetAdminGroup());
			taskDefinition.Principal.GroupId = sUser;
			taskDefinition.Principal.LogonType = _TASK_LOGON_TYPE.TASK_LOGON_GROUP;

			taskDefinition.Settings.MultipleInstances = _TASK_INSTANCES_POLICY.TASK_INSTANCES_IGNORE_NEW;
			taskDefinition.Settings.Enabled = true;
			taskDefinition.Settings.Hidden = false;
			taskDefinition.Settings.StopIfGoingOnBatteries = false;
			taskDefinition.Settings.DisallowStartIfOnBatteries = false;
			ITriggerCollection triggers = taskDefinition.Triggers;
			ITrigger trigger = triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_LOGON);
			//ITrigger trigger = triggers.Create(_TASK_TRIGGER_TYPE2.TASK_TRIGGER_BOOT);
			trigger.Enabled = true;
			taskDefinition.Triggers = triggers;

			IActionCollection actions = taskDefinition.Actions;
			IAction action = actions.Create(_TASK_ACTION_TYPE.TASK_ACTION_EXEC);
			IExecAction execAction = action as IExecAction;
			//string windowsPath;
			//windowsPath = (Environment.Is64BitOperatingSystem) ? Environment.GetFolderPath(Environment.SpecialFolder.SystemX86) : Environment.GetFolderPath(Environment.SpecialFolder.System);
			//string path = Directory.GetCurrentDirectory();
			//string path = "Get_Current_User.exe";// "C:\\MSI\\TraceFPS.exe";
			//string path = "AI Artist.exe";
			string path = "AI_Install_Model.exe";

			execAction.Arguments = "";
			execAction.WorkingDirectory = ExeFolder;
			execAction.Path = path;
			taskDefinition.Actions = actions;

			try
			{
				rootFolder.RegisterTaskDefinition("AI_Install_Model", taskDefinition, 6, null, null, _TASK_LOGON_TYPE.TASK_LOGON_S4U, null);
				rootFolder.GetTask("AI_Install_Model").Run(null);
			}
			catch (Exception e)
			{
				string error = e.ToString();
			}

			Thread.Sleep(50);
			rootFolder.DeleteTask("AI_Install_Model", 0);

			System.Runtime.InteropServices.Marshal.ReleaseComObject(taskDefinition);
			System.Runtime.InteropServices.Marshal.ReleaseComObject(rootFolder);
			System.Runtime.InteropServices.Marshal.ReleaseComObject(actions);
			System.Runtime.InteropServices.Marshal.ReleaseComObject(taskService);
			Log("Create Task");
		}

		#endregion

		#region Install Python
		private static bool Install_python()
		{
			// install python
			Log("Checking for Python...");
			if (!ExecuteCommand("python --version").Contains("Python 3."))
			{
				string path = ExeFolder + "\\python-3.10.6-amd64.exe";

				if (!File.Exists(path))
				{
					Log("Download Python...");
					var processInfo = new ProcessStartInfo("cmd.exe")
					{
						CreateNoWindow = HideCMD,
						UseShellExecute = false,
						RedirectStandardInput = true,
						WorkingDirectory = ExeFolder,
					};

					Process process = Process.Start(processInfo);
					process.StandardInput.WriteLine("curl -L -o \"python-3.10.6-amd64.exe\" \"https://www.python.org/ftp/python/3.10.6/python-3.10.6-amd64.exe\"");
					process.StandardInput.WriteLine("exit");
					process.WaitForExit();
				}

				ExecuteCommand("\"" + path + "\" /quiet InstallAllUsers=1 PrependPath=1");

				Log("Python installed!");

				string newpath = Environment.ExpandEnvironmentVariables("%ProgramW6432%") + "\\Python310\\Scripts;" +
					Environment.ExpandEnvironmentVariables("%ProgramW6432%") + "\\Python310";
				string pathvariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
				pathvariable = newpath + ";" + pathvariable;
				Environment.SetEnvironmentVariable("PATH", pathvariable, EnvironmentVariableTarget.Process);

				Log("Python PATH!");
			}
			else
			{
				Log("Python already installed.");
			}
			return true;
		}
		#endregion

		#region Install git
		private static bool Install_git()
		{
			// install git
			Log("Checking for Git...");

			if (!ExecuteCommand("git --version").Contains("git version 2."))
			{
				string path = ExeFolder + "\\Git-2.41.0.2-64-bit.exe";

				if (!File.Exists(path))
				{
					Log("Download Git...");

					var processInfo = new ProcessStartInfo("cmd.exe")
					{
						CreateNoWindow = HideCMD,
						UseShellExecute = false,
						RedirectStandardInput = true,
						WorkingDirectory = ExeFolder
					};

					Process process = Process.Start(processInfo);
					process.StandardInput.WriteLine("curl -L -o \"Git-2.41.0.2-64-bit.exe\" \"https://github.com/git-for-windows/git/releases/download/v2.41.0.windows.2/Git-2.41.0.2-64-bit.exe\"");
					process.StandardInput.WriteLine("exit");
					process.WaitForExit();
				}

				ExecuteCommand("\"" + path + "\" /VERYSILENT /NORESTART");
				Log("Git installed!");

				string newpath = Get_Git_Path() + "\\cmd";
				string pathvariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
				pathvariable = newpath + ";" + pathvariable;
				Environment.SetEnvironmentVariable("PATH", pathvariable, EnvironmentVariableTarget.Process);
			}
			else
			{
				Log("Git already installed.");
			}

			return true;
		}
		#endregion

		#region Install cuda
		private static bool Install_cuda(int input)
		{
			if (input == 1)
			{
				Log("Checking for cuda...");
			}

			if (!ExecuteCommand("nvcc --version").Contains("nvcc: NVIDIA"))
			{
				string path = ExeFolder + "\\cuda_11.8.0_522.06_windows.exe";

				if (!File.Exists(path))
				{
					Log("Download Cuda...");

					var processInfo = new ProcessStartInfo("cmd.exe")
					{
						CreateNoWindow = HideCMD,
						UseShellExecute = false,
						RedirectStandardInput = true,
						WorkingDirectory = ExeFolder
					};

					Process process = Process.Start(processInfo);
					process.StandardInput.WriteLine("curl -L -o \"cuda_11.8.0_522.06_windows.exe\" \"https://developer.download.nvidia.com/compute/cuda/11.8.0/local_installers/cuda_11.8.0_522.06_windows.exe\"");
					process.StandardInput.WriteLine("exit");
					process.WaitForExit();
				}

				Log("Cuda download finish");

				ExecuteCommand("\"" + path + "\" -s");

				//CUDA
				string newpath = Environment.ExpandEnvironmentVariables("%ProgramW6432%") + "\\NVIDIA GPU Computing Toolkit\\CUDA\\v11.8\\bin;" +
					Environment.ExpandEnvironmentVariables("%ProgramW6432%") + "\\NVIDIA GPU Computing Toolkit\\CUDA\\v11.8\\libnvvp";
				string pathvariable = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);
				pathvariable = newpath + ";" + pathvariable;
				Environment.SetEnvironmentVariable("PATH", pathvariable, EnvironmentVariableTarget.Process);
				Log("Cuda installed!" + input);
			}
			else
			{
				Log("Cuda already installed.");
			}

			return true;
		}
		#endregion

		#region Install BuildTools
		private static void Install_BuildTools()
		{
			Log("Checking for BuildTools...");
			if (!Directory.Exists(BuildTools_path))
			{
				Directory.CreateDirectory(BuildTools_path);

				Log("Installing BuildTools...");
				var processInfo = new ProcessStartInfo("cmd.exe")
				{
					CreateNoWindow = HideCMD,
					UseShellExecute = false,
					RedirectStandardInput = true,
					WorkingDirectory = ExeFolder,
				};

				Process process = Process.Start(processInfo);
				process.StandardInput.WriteLine("cd /d " + ExeFolder);

				if (!File.Exists(ExeFolder + "\\vs_BuildTools.exe"))
				{
					process.StandardInput.WriteLine("curl -L -o \"vs_BuildTools.exe\" \"https://aka.ms/vs/17/release/vs_BuildTools.exe\"");
				}

				process.StandardInput.WriteLine("vs_BuildTools.exe --installPath \"" + BuildTools_path + "\" " +
					"--norestart --quiet --add Microsoft.VisualStudio.Workload.VCTools " +
					"--add Microsoft.VisualStudio.Component.VC.Tools.x86.x64 --add Microsoft.VisualStudio.Component.Windows10SDK.18362 " +
					"--add Microsoft.VisualStudio.Component.VC.CMake.Project --add Microsoft.VisualStudio.Component.TestTools.BuildTools " +
					"--add Microsoft.VisualStudio.Component.VC.ASAN");


				process.StandardInput.WriteLine("exit");
				process.WaitForExit();
				Log("BuildTools installed!");
			}
		}
		#endregion

		#region Execute Command
		private static string ExecuteCommand(string command)
		{
			Log($"Running command: {command}");

			var processInfo = new ProcessStartInfo("cmd.exe")
			{
				CreateNoWindow = HideCMD,
				UseShellExecute = false,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				RedirectStandardInput = true,
				WorkingDirectory = ExeFolder,
			};

			Process process = Process.Start(processInfo);
			process.StandardInput.WriteLine(command);

			string output = "";

			process.StandardInput.WriteLine("exit");
			process.WaitForExit();

			output = process.StandardOutput.ReadToEnd();
			output += process.StandardError.ReadToEnd();

			return output;
		}
		#endregion

		#region Log
		public static void Log(string message)
		{
			FileStream fs = null;
			if (!File.Exists(LogFilePath))
			{
				using (fs = new FileStream(LogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
				{
					sw.BaseStream.Seek(0, SeekOrigin.End);
					string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{message}";
					sw.WriteLine(logMessage);
				}
				//SetAccessRights(LogFilePath);
			}
			else
			{
				using (fs = new FileStream(LogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
				{
					sw.BaseStream.Seek(0, SeekOrigin.End);
					string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{message}";
					sw.WriteLine(logMessage);
				}
			}
		}
		#endregion

		#region File
		public static void SetAccessRights(string fileName)
		{
			FileSecurity fileSecurity = File.GetAccessControl(fileName);
			AuthorizationRuleCollection rules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));

			foreach (FileSystemAccessRule rule in rules)
			{
				string name = rule.IdentityReference.Value;
				if (rule.FileSystemRights != FileSystemRights.FullControl)
				{
					FileSecurity newFileSecurity = File.GetAccessControl(fileName);
					FileSystemAccessRule newRule = new FileSystemAccessRule(name, FileSystemRights.FullControl, AccessControlType.Allow);
					newFileSecurity.AddAccessRule(newRule);
					File.SetAccessControl(fileName, newFileSecurity);
				}
			}
		}
		#endregion

		#region Get_Git_Path
		internal static string Get_Git_Path()
		{
			try
			{
				RegistryKey registry = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
				string reg_path = @"SOFTWARE\GitForWindows";
				registry = registry.OpenSubKey(reg_path, false);

				//RegistryKey subkey = reg_base.OpenSubKey(reg_path, false);

				string temp = (string)registry.GetValue("InstallPath", "");

				return temp;
			}
			catch (Exception ex)
			{
				Log("Get_Git_Path: " + ex.ToString());
				return "";
			}
		}
		#endregion
	}
}
