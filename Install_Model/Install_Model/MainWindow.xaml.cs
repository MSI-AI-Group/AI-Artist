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
using static System.Net.Mime.MediaTypeNames;

namespace AI_Artist_V2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Args;

        private bool IsDragging = false;
        private Point StartPoint;

        Storyboard Story = new Storyboard();
        Thread Thread_WebUI = null;
        DispatcherTimer Start_WebUI_Timer = null;

        public int Image_width = 512;
        public int Image_height = 512;
        public int Image_count = 1;

        public List<string> Output_img_path = new List<string>();
        public List<string> Output_img_seed = new List<string>();
        bool Switch_opt = false;

        public MainWindow()
        {
            InitializeComponent();
            Args = Environment.GetCommandLineArgs();
            for (int i = 0; i < Args.Length; i++)
            {
                Args[i] = Args[i].ToLower();
            }

            _MainWindow = this;

            if (!Directory.Exists(Path_PD_AIArtist))
            {
                Directory.CreateDirectory(Path_PD_AIArtist);
            }

            #region Loading Animation
            DoubleAnimation da = new DoubleAnimation(0, 3600, new Duration(TimeSpan.FromSeconds(20)));
            RotateTransform rotate = new RotateTransform();
            Image_Loading.RenderTransform = rotate;
            Storyboard.SetTarget(da, Image_Loading);

            Storyboard.SetTargetProperty(da, new PropertyPath("RenderTransform.Angle"));
            da.RepeatBehavior = RepeatBehavior.Forever;
            Story.Children.Add(da);

            #endregion Initial Loading Animation

            #region Check HW + Start WebUI
            Story.Begin();
            Canvas_Loading.Visibility = Visibility.Visible;
            Text_Load.Text = "Loading";

            Logger.Log("");
            Logger.Log("##################################");
            Logger.Log("##### Starting MSI AI Artist #####");
            Logger.Log("##################################");

            Start_WebUI_Timer = new DispatcherTimer();
            Start_WebUI_Timer.Tick += new EventHandler(Start_WebUI_Timer_Tick);

            Thread_WebUI = new Thread(Start_WebUI);
            Thread_WebUI.Start();
            #endregion
        }
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

            DataCenter._Install.Close_Process();

            if (Opt_Pytorch.IsChecked == true)
            {
                DataCenter._MainWindow.Text_Load.Text = "Switch Loading";

                Story.Begin();
                Canvas_Loading.Visibility = Visibility.Visible;
                await Task.Run(() =>
                {
                    DataCenter._Install.OpenStableDiffusion(false);
                });
                await API_SetUnet(DataCenter.Url_API, "None");
                
                Start_WebUI_Timer.Interval = new TimeSpan(0, 0, 5);
                Start_WebUI_Timer.Start();
            }
            else if (Opt_xformers.IsChecked == true)
            {
                DataCenter._MainWindow.Text_Load.Text = "Switch Loading";

                Story.Begin();
                Canvas_Loading.Visibility = Visibility.Visible;
                await Task.Run(() =>
                {
                    DataCenter._Install.OpenStableDiffusion(true);
                });
                await API_SetUnet(DataCenter.Url_API, "None");

                Start_WebUI_Timer.Interval = new TimeSpan(0, 0, 5);
                Start_WebUI_Timer.Start();
            }
            else if (Opt_TensorRT.IsChecked == true)
            {
                DataCenter._MainWindow.Text_Load.Text = "Switch Loading";

                Story.Begin();
                Canvas_Loading.Visibility = Visibility.Visible;
                await Task.Run(() =>
                {
                    DataCenter._Install.OpenStableDiffusion(true);
                });
                await API_SetUnet(DataCenter.Url_API, "[TRT] TRT_Base");

                Start_WebUI_Timer.Interval = new TimeSpan(0, 0, 5);
                Start_WebUI_Timer.Start();
            }

            Switch_opt = false;

            DataCenter._Install.Close_Process();
            Close();
            Logger.Log("Exit Install Model");
            Environment.Exit(0);
        }
        #endregion

        #region Check_WebUI
        internal void Start_WebUI()
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
#if DEBUG
            //DataCenter.GPU_NVIDIA = false;
            //DataCenter.IsSupport_TRT = false;
            //DataCenter.IsSupport_OV = false;
#endif

            this.Dispatcher.Invoke(new Action(() =>
            {
                if (DataCenter.GPU_NVIDIA != true)//Collapsed > Visibilty
                {
                    Opt_Pytorch.Visibility = Visibility.Collapsed;
                    Opt_xformers.Visibility = Visibility.Collapsed;
                }

                if (DataCenter.IsSupport_TRT != true)
                {
                    Opt_TensorRT.Visibility = Visibility.Collapsed;
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

        private async void Start_WebUI_Timer_Tick(object sender, EventArgs e)
        {
            if (Check_SD())
            {
                Start_WebUI_Timer.Stop();

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
                    await API_SetUnet(DataCenter.Url_API, "[TRT] TRT_Base_v1");
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

    }
}
