using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace AI_Artist_V2
{
    internal static class DataCenter
    {
        #region Path
        internal static string Path_App = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        internal static string Path_SD = Path.Combine(Path_App, "stable-diffusion-webui");
        internal static string Path_BLIP = Path_App + "\\BLIP\\CheckPoints\\";

        internal static string Path_PD_AIArtist = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + "\\AI_Artist\\";
        internal static string Path_Resource = Path.Combine(Path_App, "Resource");

        internal static string MSI_LoRA_NB = "MSI NB";
        internal static string MSI_LoRA_CND_GNP = "MSI CND GNP";

        internal static string Path_SD_LoRA = Path_SD + "\\models\\Lora\\";
        internal static string Path_SD_Ckpt = Path_SD + "\\models\\Stable-diffusion\\";
        internal static string Path_LoRA = Path_Resource + "\\" + MSI_LoRA_NB + ".safetensors";
        public static string File_SD_LoRA_NB = Path_SD_LoRA + MSI_LoRA_NB + ".safetensors";
        public static string File_SD_LoRA_CND_GNP = Path_SD_LoRA + MSI_LoRA_CND_GNP + ".safetensors";

        internal static string Path_SD_TRT = Path_SD + "\\extensions\\stable-diffusion-webui-tensorrt";
        internal static string Path_SD_TRT_SDK = Path_SD + "\\extensions\\stable-diffusion-webui-tensorrt\\TensorRT-8.6.1.6";
        internal static string TRT_SDK_zip = "TensorRT-8.6.1.6.Windows10.x86_64.cuda-11.8.zip";

        internal static string TRT_Cpp = Path_App + "\\Tool";

        public static string BLIP_Name = "model_base_capfilt_large.pth";
        public static string SAM_Name = "sam_vit_h_4b8939.pth";

        public static string GPU_Model = "4060";

        public static int Retrytime = 0;


        public static string Image_path //Output
        {
            get
            {
                var folder = Path.Combine(DataCenter.Path_App, "Output"); //Output
                if (Directory.Exists(folder) == false)
                {
                    Directory.CreateDirectory(folder);
                }
                return folder;
            }
        }
        #endregion

        #region Log
        internal static string LogFilePath
        {
            get
            {
                var folder = Path.Combine(Path_App, "AI_Artist_Install_log");

                if (Directory.Exists(folder) == false)
                    Directory.CreateDirectory(folder);

                return Path.Combine(folder, $"AI_Artist_{DateTime.Now:yyyy-MM-dd}.txt");
            }
        }
        #endregion
        internal static string LogCMDFilePath
        {
            get
            {
                string folder = "";

                    folder = Path.Combine(Path_App, "AI_Artist_Install_log");


                if (Directory.Exists(folder) == false)
                    Directory.CreateDirectory(folder);

                return Path.Combine(folder, $"AI_Artist_CMD_{DateTime.Now:yyyy-MM-dd}.txt");
            }
        }
        #region HW Check

        internal static CheckHW chw = null;
        internal static bool GPU_NVIDIA = false; //support PyTorch, xformers, Olive
        internal static bool IsSupport_TRT = false;

        #endregion

        #region DEBUG
#if DEBUG
        internal static bool HideCMD = false; //open console for debug
#else
        internal static bool HideCMD = true;
#endif
        #endregion

        #region Other
        public static Install _Install = null;
        public static MainWindow _MainWindow = null;

        public static string Url_API = "127.0.0.1:9860";
        public static string Http_url_API = "http://127.0.0.1:9860";
        public static string Negative_prompt = @"nsfw, text, close up, cropped, out of frame, worst quality, low quality, jpeg artifacts, ugly, duplicate, deformed, blurry, bad proportions";

        public static List<Style_data> Style_list = null;

        public static List<Lora_data> Lora_list = null;

        public struct Style_data
        {
            public string path;       
            public string displayName;
            public bool deletable;
        }
        public struct Lora_data
        {
            public string path;
            public string displayName;
            public string weight;
            public bool changeable;
            public bool deletable;
        }
        #endregion

        #region Set File Access Rights
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
    }
}
