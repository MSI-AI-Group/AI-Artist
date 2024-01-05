using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AI_Artist_Install_Tool
{
    internal class CheckHW
    {
        public static List<string> VGA_Caption = new List<string>(); // Win32_VideoController > Caption
        [DllImport("nvcuda")]
        public static extern int cuInit(int flags);

        [DllImport("nvcuda")]
        public static extern int cuDeviceGetCount(out int count);


        public static T GetValueFromWMIManagementObject<T>(string sName, ManagementObject _o)
        {
            try
            {
                return (T)Convert.ChangeType(_o.Properties[sName].Value, typeof(T));
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }

        public static void GetGPUInfo()
        {
            System.Management.ManagementObjectSearcher searcher = null;

            searcher = new System.Management.ManagementObjectSearcher("root\\CIMV2", "SELECT Caption, PNPDeviceID, Name FROM Win32_VideoController");
            if (searcher != null)
            {
                try
                {
                    foreach (System.Management.ManagementObject obj in searcher.Get())
                    {
                        string Caption = GetValueFromWMIManagementObject<string>("Caption", obj) ?? "";

                        if (!string.IsNullOrEmpty(Caption))
                            VGA_Caption.Add(Caption);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public string GetCPUManufacturer()
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["Manufacturer"].ToString().Trim();
                }
            }
            return string.Empty;
        }
        public static bool GetGPU_NVIDIA()
        {
            foreach (string VGA in VGA_Caption)
            {
                if (VGA.Contains("NVIDIA"))
                {
                    return true;
                }
            }
            return false;


        }
        public bool GetGPU_TRT()
        {

            foreach (string VGA in VGA_Caption)
            {
                if (VGA.Contains("NVIDIA"))
                {
                
                    int deviceCount;
                    int result = cuInit(0);

                    if (result != 0)
                    {
                        Program.Log("Unable to initialize CUDA");
                        return false;
                    }
                    result = cuDeviceGetCount(out deviceCount);
                    if (result != 0)
                    {
                        Program.Log("Unable to get the number of graphics cards");
                        return false;
                    }
                    try
                    {
                        var processInfo = new ProcessStartInfo("cmd.exe")
                        {
                            CreateNoWindow = Program.HideCMD,
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
                        string output = System.IO.File.ReadAllText("GPUtotalMemory.txt");
                        Match match_text = Regex.Match(output, @"\d+");
                        if (match_text.Success)
                        {
                            string lastDigits = match_text.Value;
                            int num = int.Parse(output) / 1000;
                            if (num < 6)
                            {
                                Program.Log("graphics cards total memory is " + num + ",TRT must have at least 6G VRAM");
                                return false;
                            }
                            else
                            {
                                Program.Log("graphics cards total memory is " + num );
                            }
                        }
                        else
                        {
                            Program.Log("graphics cards total memory:"+output);
                            return false;
                        }
                     
                    }
                    catch (Exception ex)
                    {
                        Program.Log($"Error: {ex.Message}");
                    }
                    Match match = Regex.Match(VGA, @"\d+");
                    if (match.Success)
                    {
                        string lastDigits = match.Value;
                        int num = int.Parse(lastDigits);
                        if (num < 4050)
                        {
                            Program.Log("graphics cards is " + num + ",TRT must have at least RTX4060 series or above");
                            return false;
                        }
                    }
                    if (VGA.Contains("RTX"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool GetCPUGeneration()
        {
            string cpuManufacturer = GetCPUManufacturer();
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"].ToString();
                    if (name.Contains("Intel"))
                    {
                        Match match = Regex.Match(name, @"i\d+");
                        if (match.Success)
                        {
                            match = Regex.Match(name, @"\d+$");//ex 13700
                            if (match.Success)
                            {
                                string lastDigits = match.Value;
                                if (int.Parse(lastDigits.Substring(0, 1)) != 1)
                                    return false;
                                int generation = int.Parse(lastDigits.Substring(0, 2));

                                if (cpuManufacturer == "GenuineIntel" && generation >= 13 || name.Contains("Ultra"))
                                {
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                            else//ex 13700KF
                            {
                                name = name.Substring(0, name.Length - 2);
                                match = Regex.Match(name, @"\d+$");
                                if (match.Success)
                                {
                                    string lastDigits = match.Value;
                                    if (int.Parse(lastDigits.Substring(0, 1)) != 1)
                                        return false;
                                    int generation = int.Parse(lastDigits.Substring(0, 2));

                                    if (cpuManufacturer == "GenuineIntel" && generation >= 13 || name.Contains("Ultra"))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                                else return false;
                            }
                        }
                        else return true;
                    }
                }
            }
            return false;
        }

        public bool GetCPUMemory()
        {
            ManagementObjectSearcher Searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_MemoryArray");
            int MemorySize = 0;
            foreach (ManagementObject QueryObj in Searcher.Get())
            {
                string EndingAddress = QueryObj["EndingAddress"].ToString();

                try
                {
                    MemorySize = (Convert.ToInt32(EndingAddress) + 1) / 1024 / 1024;
                }
                catch
                {
                    MemorySize = 0;
                }
            }
            if (MemorySize < 32)
                return false;
            else return true;
        }
    }
}
