using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AutoActions.ProjectResources;
using AutoActions.Theming;

namespace AutoActions
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    ///
    public partial class App : Application
    {
        public static Theme Theme { get; set; } = Theme.Light;

        static Mutex mutex;

        [STAThread]
        public static void Main()
         {
         bool createNew = false;
            mutex = new Mutex(true, "{2846416C-610B-4A6B-A31C-A4AA6826E9BE}", out createNew);
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                // 设置默认语言为中文
                SetDefaultLanguage("zh-CN");

                var application = new App();
                application.InitializeComponent();
                Globals.Instance.LoadSettings();

                // 从设置中加载已保存的语言设置
                if (!string.IsNullOrEmpty(Globals.Instance.Settings.SelectedLanguage))
                {
                    SetDefaultLanguage(Globals.Instance.Settings.SelectedLanguage);
                }

                application.Run();
            }
            else
            {
                MessageBox.Show(ProjectLocales.AlreadyRunning);
            }
        }

        private static void SetDefaultLanguage(string cultureName)
        {
            try
            {
                CultureInfo culture = new CultureInfo(cultureName);
                ProjectLocales.Culture = culture;
                
                // 设置当前线程和默认线程的文化信息
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture.Name);
                Thread.CurrentThread.CurrentUICulture = culture;

                // 输出日志信息
                Globals.Logs.Add($"应用语言: {culture.DisplayName}", false);
            }
            catch (Exception ex)
            {
                // 语言设置失败，使用默认语言
                Globals.Logs.AddException(ex);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Views.AutoActionsMainView mainView = new Views.AutoActionsMainView();
            if (!Globals.Instance.Settings.StartMinimizedToTray)
                mainView.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
                mutex.ReleaseMutex();

        }
    }
}
