using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace AI_Artist_V2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        System.Threading.Mutex _mutex = null;
        public static readonly string sAppTitleName = ((AssemblyTitleAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), true)[0]).Title;

        private void App_Startup_Fn(object sender, StartupEventArgs e)
        {
            #region Check AP Open
            try
            {
                this._mutex = System.Threading.Mutex.OpenExisting(sAppTitleName);

                Environment.Exit(0);
                return;
            }
            catch (System.Threading.WaitHandleCannotBeOpenedException)
            {
                this._mutex = new System.Threading.Mutex(true, sAppTitleName);
            }
            #endregion

            #region Args (-debug/-release)
            try
            {
                if (e.Args.Length > 0)
                {
                    foreach (var arg in e.Args)
                    {
                        arg.ToLower();
                        switch (arg)
                        {
                            case "-debug":
                                DataCenter.HideCMD = false;
                                break;
                        }
                    }
                }
            }
            catch
            {
                DataCenter.HideCMD = false;
            }
            #endregion
        }
    }
}
