using AutoActions.Displays;
using AutoActions.Profiles;
using AutoActions.Profiles.Actions;
using CodectoryCore;
using CodectoryCore.UI.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml.Serialization;

namespace AutoActions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UserAppSettings : BaseViewModel
    {
        public static readonly object _settingsLock = new object();

        private bool _globalAutoActions = true;
        private bool _createLogFile = false;
        private bool _autoStart = false;
        private bool _autoUpdate = false;
        private bool _startMinimizedToTray;
        private bool _closeToTray;
        private bool _checkForNewVersion = false;
        private bool _hideSplashScreenOnStartup = false;
        private bool _hideSplashScreenOnAutoUpdate = false;
        private string _selectedLanguage = "zh-CN";

        readonly object _audioDevicesLock = new object();
        private Guid _defaultProfileGuid = Guid.Empty;
        private Size _windowSize = new Size(1280, 800);


        private SortableObservableCollection<ApplicationProfileAssignment> _applicationProfileAssignments;
        private DispatchingObservableCollection<Profile> _applicationProfiles;
        private DispatchingObservableCollection<Display> _displays;
        private DispatchingObservableCollection<ProfileActionShortcut> _actionShortcuts;


        [JsonProperty]
        public Guid DefaultProfileGuid { get => _defaultProfileGuid; set { _defaultProfileGuid = value; OnPropertyChanged(); OnPropertyChanged(nameof(DefaultProfile)); } }

        public Profile DefaultProfile { get => ApplicationProfiles.FirstOrDefault(p => p.GUID.Equals(DefaultProfileGuid)); set { DefaultProfileGuid = value == null ? Guid.Empty : value.GUID; } }


        [JsonProperty]
        public bool GlobalAutoActions { get => _globalAutoActions; set { _globalAutoActions = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool AutoStart { get => _autoStart; set { _autoStart = value; OnPropertyChanged(); } }
        [JsonProperty]
        public bool AutoUpdate { get => _autoUpdate; set { _autoUpdate = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool HideSplashScreenOnStartup { get => _hideSplashScreenOnStartup; set { _hideSplashScreenOnStartup = value; OnPropertyChanged(); } }


        [JsonProperty]
        public bool HideSplashScreenOnAutoUpdate { get => _hideSplashScreenOnAutoUpdate; set { _hideSplashScreenOnAutoUpdate = value; OnPropertyChanged(); } }


        [JsonProperty]
        public bool CreateLogFile { get => _createLogFile; set { _createLogFile = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool StartMinimizedToTray { get => _startMinimizedToTray; set { _startMinimizedToTray = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool CloseToTray { get => _closeToTray; set { _closeToTray = value; OnPropertyChanged(); } }

        [JsonProperty]
        public bool CheckForNewVersion { get => _checkForNewVersion; set { _checkForNewVersion = value; OnPropertyChanged(); } }

        [JsonProperty]
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (_selectedLanguage != value)
                {
                    _selectedLanguage = value;
                    OnPropertyChanged();
                    
                    // 应用语言设置
                    try
                    {
                        CultureInfo culture = new CultureInfo(value);
                        ProjectResources.ProjectLocales.Culture = culture;
                        
                        // 设置当前线程和默认线程的文化信息
                        CultureInfo.DefaultThreadCurrentCulture = culture;
                        CultureInfo.DefaultThreadCurrentUICulture = culture;
                        Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(culture.Name);
                        Thread.CurrentThread.CurrentUICulture = culture;

                        // 重新加载界面资源
                        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ResourceDictionary dict = new ResourceDictionary();
                            System.Windows.Application.Current.Resources.MergedDictionaries.Clear();
                            System.Windows.Application.Current.Resources.MergedDictionaries.Add(dict);
                        });
                        
                        Globals.Logs.Add($"已切换语言: {culture.DisplayName}", false);
                    }
                    catch (Exception ex)
                    {
                        Globals.Logs.AddException(ex);
                    }
                }
            }
        }


        [JsonProperty(Order = 2)]
        public SortableObservableCollection<ApplicationProfileAssignment> ApplicationProfileAssignments { get => _applicationProfileAssignments; set { _applicationProfileAssignments = value; OnPropertyChanged(); } }

        [JsonProperty(Order = 1)]
        public DispatchingObservableCollection<Profile> ApplicationProfiles { get => _applicationProfiles; set { _applicationProfiles = value; OnPropertyChanged(); } }


        [JsonProperty]
        public DispatchingObservableCollection<Display> Displays { get => _displays; set { _displays = value; OnPropertyChanged(); } }

        [JsonProperty]
        public DispatchingObservableCollection<ProfileActionShortcut> ActionShortcuts { get => _actionShortcuts; set { _actionShortcuts = value; OnPropertyChanged(); } }

        [JsonProperty]
        public Size WindowSize { get => _windowSize; set { _windowSize = value; OnPropertyChanged(); } }


        public UserAppSettings()
        {
            ApplicationProfileAssignments = new SortableObservableCollection<ApplicationProfileAssignment>(new ObservableCollection<ApplicationProfileAssignment>());
            ApplicationProfiles = new DispatchingObservableCollection<Profile>();
            ActionShortcuts = new DispatchingObservableCollection<ProfileActionShortcut>();
            Displays = new DispatchingObservableCollection<Display>();
        }

        public static UserAppSettings ReadSettings(string path)
        {
            UserAppSettings settings = null;

            lock (_settingsLock)
            {

                try
            {
                    string serializedJson = File.ReadAllText(path);
                    serializedJson = UpgradeJson(serializedJson);
                    settings = (UserAppSettings)JsonConvert.DeserializeObject<UserAppSettings>(serializedJson, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                    });
                }
                catch (Exception ex)
                {
                    try
                    {
                        settings = TryReadXML(path);
                        Globals.Logs.Add("Loaded deprecated xml settings.", false);
                        return settings;
                    }
                    catch (Exception)
                    {
                    }
                    Globals.Logs.AddException(ex);
                    throw;
                }
            }
            return settings;
        }

        private static string UpgradeJson(string serializedJson)
        {
            serializedJson = serializedJson.Replace("AutoHDR", "AutoActions");
            serializedJson = serializedJson.Replace("\"$type\": \"AutoActions.Displays.Display, AutoActions\"", "\"$type\": \"AutoActions.Displays.Display, AutoActions.Displays\"");
            serializedJson = serializedJson.Replace("\"Monitors\": [", "\"Displays\": [");
            serializedJson = serializedJson.Replace("\"SetHDR\":", "\"ChangeHDR\":");
            serializedJson = serializedJson.Replace("\"SetResolution\":", "\"ChangeResolution\":");
            serializedJson = serializedJson.Replace("\"SetRefreshRate\":", "\"ChangeRefreshRate\":");
            serializedJson = serializedJson.Replace("\"SetColorDepth\":", "\"ChangeColorDepth\":");
            serializedJson = serializedJson.Replace("\"SetOutput\":", "\"ChangePlaybackDevice\":");
            serializedJson = serializedJson.Replace("\"SetInput\":", "\"ChangeRecordDevice\":");
            serializedJson = serializedJson.Replace("\"OutputDeviceID\":", "\"PlaybackDeviceID\":");
            serializedJson = serializedJson.Replace("\"InputDeviceID\":", "\"RecordDeviceID\":");

            return serializedJson;
        }

        private static UserAppSettings TryReadXML(string path)
        {
            UserAppSettings settings = null;
            XmlSerializer serializer = new XmlSerializer(typeof(UserAppSettings));
            using (TextReader reader = new StreamReader(path))
            {
                settings = (UserAppSettings)serializer.Deserialize(reader);
            }
            return settings;
        }

        public static void SaveSettings(UserAppSettings settings, string path)
        {
            lock (_settingsLock)
            {
                try
                {
                    string serializedJson = JsonConvert.SerializeObject(settings, Newtonsoft.Json.Formatting.Indented, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
                    });
                    File.WriteAllText(path, serializedJson);
                }
                catch (Exception ex)
                {
                    Globals.Logs.AddException(ex);
                    throw;
                }
            }
        }
    }

    public static class UserAppSettingsExtension
    {

        public static void SaveSettings(this UserAppSettings settings, string path)
        {
            UserAppSettings.SaveSettings(settings, path);
        }

    }

}
