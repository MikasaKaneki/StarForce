using GameFramework;
using GameFramework.Event;
using GameFramework.Resource;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce
{
    public class ProcedureCheckVersion : ProcedureBase
    {
        private bool m_InitResourcesComplete = false;

        public override bool UseNativeDialog
        {
            get { return true; }
        }

        protected internal override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);

            m_InitResourcesComplete = false;

            GameEntry.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
            GameEntry.Event.Subscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

            RequestVersion();
        }

        protected internal override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(WebRequestSuccessEventArgs.EventId, OnWebRequestSuccess);
            GameEntry.Event.Unsubscribe(WebRequestFailureEventArgs.EventId, OnWebRequestFailure);

            base.OnLeave(procedureOwner, isShutdown);
        }

        protected internal override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds,
            float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (!m_InitResourcesComplete)
            {
                return;
            }

            ChangeState<ProcedurePreload>(procedureOwner);
        }

        private void RequestVersion()
        {
            string deviceId = SystemInfo.deviceUniqueIdentifier;
            string deviceName = SystemInfo.deviceName;
            string deviceModel = SystemInfo.deviceModel;
            string processorType = SystemInfo.processorType;
            string processorCount = SystemInfo.processorCount.ToString();
            string memorySize = SystemInfo.systemMemorySize.ToString();
            string operatingSystem = SystemInfo.operatingSystem;
            string iOSGeneration = string.Empty;
            string iOSSystemVersion = string.Empty;
            string iOSVendorIdentifier = string.Empty;
#if UNITY_IOS && !UNITY_EDITOR
            iOSGeneration = UnityEngine.iOS.Device.generation.ToString();
            iOSSystemVersion = UnityEngine.iOS.Device.systemVersion;
            iOSVendorIdentifier = UnityEngine.iOS.Device.vendorIdentifier ?? string.Empty;
#endif
            string gameVersion = Version.GameVersion;
            string platform = Application.platform.ToString();
            string language = GameEntry.Localization.Language.ToString();
            string unityVersion = Application.unityVersion;
            string installMode = Application.installMode.ToString();
            string sandboxType = Application.sandboxType.ToString();
            string screenWidth = Screen.width.ToString();
            string screenHeight = Screen.height.ToString();
            string screenDpi = Screen.dpi.ToString();
            string screenOrientation = Screen.orientation.ToString();
            string screenResolution = string.Format("{0} x {1} @ {2}Hz", Screen.currentResolution.width.ToString(),
                Screen.currentResolution.height.ToString(), Screen.currentResolution.refreshRate.ToString());
            string useWifi = (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                .ToString();

            WWWForm wwwForm = new WWWForm();
            wwwForm.AddField("DeviceId", WebUtility.EscapeString(deviceId));
            wwwForm.AddField("DeviceName", WebUtility.EscapeString(deviceName));
            wwwForm.AddField("DeviceModel", WebUtility.EscapeString(deviceModel));
            wwwForm.AddField("ProcessorType", WebUtility.EscapeString(processorType));
            wwwForm.AddField("ProcessorCount", WebUtility.EscapeString(processorCount));
            wwwForm.AddField("MemorySize", WebUtility.EscapeString(memorySize));
            wwwForm.AddField("OperatingSystem", WebUtility.EscapeString(operatingSystem));
            wwwForm.AddField("IOSGeneration", WebUtility.EscapeString(iOSGeneration));
            wwwForm.AddField("IOSSystemVersion", WebUtility.EscapeString(iOSSystemVersion));
            wwwForm.AddField("IOSVendorIdentifier", WebUtility.EscapeString(iOSVendorIdentifier));
            wwwForm.AddField("GameVersion", WebUtility.EscapeString(gameVersion));
            wwwForm.AddField("Platform", WebUtility.EscapeString(platform));
            wwwForm.AddField("Language", WebUtility.EscapeString(language));
            wwwForm.AddField("UnityVersion", WebUtility.EscapeString(unityVersion));
            wwwForm.AddField("InstallMode", WebUtility.EscapeString(installMode));
            wwwForm.AddField("SandboxType", WebUtility.EscapeString(sandboxType));
            wwwForm.AddField("ScreenWidth", WebUtility.EscapeString(screenWidth));
            wwwForm.AddField("ScreenHeight", WebUtility.EscapeString(screenHeight));
            wwwForm.AddField("ScreenDPI", WebUtility.EscapeString(screenDpi));
            wwwForm.AddField("ScreenOrientation", WebUtility.EscapeString(screenOrientation));
            wwwForm.AddField("ScreenResolution", WebUtility.EscapeString(screenResolution));
            wwwForm.AddField("UseWifi", WebUtility.EscapeString(useWifi));

            GameEntry.WebRequest.AddWebRequest(GameEntry.BuiltinData.BuildInfo.CheckVersionUrl, wwwForm, this);
        }

        private void OnWebRequestSuccess(object sender, GameEventArgs e)
        {
            WebRequestSuccessEventArgs ne = (WebRequestSuccessEventArgs) e;
            if (ne.UserData != this)
            {
                return;
            }

            string responseJson = Utility.Converter.GetString(ne.GetWebResponseBytes());
            VersionInfo versionInfo = Utility.Json.ToObject<VersionInfo>(responseJson);
            if (versionInfo == null)
            {
                Log.Error("Parse VersionInfo failure.");
                return;
            }

            Log.Info("Latest game version is '{0}', local game version is '{1}'.", versionInfo.LatestGameVersion,
                Version.GameVersion);

            if (versionInfo.ForceGameUpdate)
            {
                /*****
                GameEntry.UI.OpenDialog(new DialogParams
                {
                    Mode = 2,
                    Title = GameEntry.Localization.GetString("ForceUpdate.Title"),
                    Message = GameEntry.Localization.GetString("ForceUpdate.Message"),
                    ConfirmText = GameEntry.Localization.GetString("ForceUpdate.UpdateButton"),
                    OnClickConfirm = delegate (object userData) { Application.OpenURL(versionInfo.GameUpdateUrl); },
                    CancelText = GameEntry.Localization.GetString("ForceUpdate.QuitButton"),
                    OnClickCancel = delegate (object userData) { UnityGameFramework.Runtime.GameEntry.Shutdown(ShutdownType.Quit); },
                }); 
                ***/


                GameEntry.Resource.UpdatePrefixUri = versionInfo.GameUpdateUrl;
                GameEntry.Resource.UpdateVersionList(versionInfo.VersionListLength, versionInfo.VersionListHashCode,
                    versionInfo.VersionListZipLength, versionInfo.VersionListZipHashCode,
                    new UpdateVersionListCallbacks((path, uri) =>
                    {
                        Log.Info("更新资源列表成功 path is '{0}',uri is '{1}'", path, uri);

                        GameEntry.Resource.CheckResources((resources, count, updateCount, length, zipLength) =>
                        {
                            Log.Info(
                                "检查资源成功的回调 resources is '{0}',count is '{1}',updateCount is '{2}',length is '{3}',zipLength is '{4}'");
                            OnInitResourcesComplete();
                        });
                    }, (uri, message) => { Log.Error("更新资源成功 uri is '{0}',message is '{1}'", uri, message); }));

                return;
            }

            GameEntry.Resource.InitResources(OnInitResourcesComplete);
        }

        private void OnWebRequestFailure(object sender, GameEventArgs e)
        {
            WebRequestFailureEventArgs ne = (WebRequestFailureEventArgs) e;
            if (ne.UserData != this)
            {
                return;
            }

            Log.Warning("Check version failure.");

            GameEntry.Resource.InitResources(OnInitResourcesComplete);
        }

        private void OnInitResourcesComplete()
        {
            m_InitResourcesComplete = true;

            Log.Info("Init resources complete.");
        }
    }
}