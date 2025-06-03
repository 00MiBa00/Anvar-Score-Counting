using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using System.Diagnostics;

using System.IO;

public class AddNotificationExtension
{
    [PostProcessBuild(9999)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        string projectPath = PBXProject.GetPBXProjectPath(path);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
        string mainTarget = project.GetUnityMainTargetGuid();
#else
        string mainTarget = project.TargetGuidByName("Unity-iPhone");
#endif

        // === Extension Setup ===
        string extensionTargetName = "notifications";
        string extensionBundleId = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.iOS) + ".notifications";
        string extensionFolder = Path.Combine(path, "NotificationService");
        Directory.CreateDirectory(extensionFolder);

        string plistPath = Path.Combine(extensionFolder, "Info.plist");
        string swiftPath = Path.Combine(extensionFolder, "NotificationService.swift");
        string entitlementsExtensionPath = Path.Combine(extensionFolder, "notifications.entitlements");

        string relativePlistPath = "NotificationService/Info.plist";
        string relativeSwiftPath = "NotificationService/NotificationService.swift";
        string relativeEntitlementsExtensionPath = "NotificationService/notifications.entitlements";

        File.WriteAllText(plistPath, @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>NSExtension</key>
    <dict>
        <key>NSExtensionPointIdentifier</key>
        <string>com.apple.usernotifications.service</string>
        <key>NSExtensionPrincipalClass</key>
        <string>$(PRODUCT_MODULE_NAME).NotificationService</string>
    </dict>
</dict>
</plist>");

        File.WriteAllText(swiftPath, @"
import UserNotifications
import FirebaseMessaging

class NotificationService: UNNotificationServiceExtension {
    var contentHandler: ((UNNotificationContent) -> Void)?
    var bestAttemptContent: UNMutableNotificationContent?

    override func didReceive(_ request: UNNotificationRequest, withContentHandler contentHandler: @escaping (UNNotificationContent) -> Void) {
        self.contentHandler = contentHandler
        bestAttemptContent = request.content.mutableCopy() as? UNMutableNotificationContent

        guard let bestAttemptContent = bestAttemptContent else { return }

        FIRMessagingExtensionHelper().populateNotificationContent(
            bestAttemptContent,
            withContentHandler: contentHandler)
    }

    override func serviceExtensionTimeWillExpire() {
        if let contentHandler = contentHandler, let bestAttemptContent = bestAttemptContent {
            contentHandler(bestAttemptContent)
        }
    }
}
");

        // === Add Extension Target and files ===
        string swiftFileGUID = project.AddFile(relativeSwiftPath, relativeSwiftPath);
        string plistFileGUID = project.AddFile(relativePlistPath, relativePlistPath);

        string extensionTarget = project.AddAppExtension(mainTarget, extensionTargetName, extensionBundleId, relativePlistPath);
        project.AddFileToBuild(extensionTarget, swiftFileGUID);
        project.AddFileToBuild(extensionTarget, plistFileGUID);

        project.SetBuildProperty(extensionTarget, "SWIFT_VERSION", "5.0");
        project.SetBuildProperty(extensionTarget, "IPHONEOS_DEPLOYMENT_TARGET", "11.0");
        project.SetBuildProperty(extensionTarget, "CODE_SIGN_STYLE", "Automatic");

        project.WriteToFile(projectPath); // важно: сохранить после создания таргета

        // === Add capabilities to extension ===
        File.WriteAllText(entitlementsExtensionPath, @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>aps-environment</key>
    <string>development</string>
</dict>
</plist>");

        var extensionCapabilityManager = new ProjectCapabilityManager(
            projectPath,
            relativeEntitlementsExtensionPath,
            null,
            extensionTarget
        );
        extensionCapabilityManager.AddPushNotifications(false);
        extensionCapabilityManager.WriteToFile();

        // Добавим entitlements в build settings
        project.ReadFromFile(projectPath);
        project.AddBuildProperty(extensionTarget, "CODE_SIGN_ENTITLEMENTS", relativeEntitlementsExtensionPath);

        // === Add capabilities to main target ===
        string mainEntitlementsPath = Path.Combine(path, "main.entitlements");
        string relativeMainEntitlementsPath = "main.entitlements";
        File.WriteAllText(mainEntitlementsPath, @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>
    <key>aps-environment</key>
    <string>development</string>
    <key>UIBackgroundModes</key>
    <array>
        <string>remote-notification</string>
    </array>
</dict>
</plist>");

        var mainCapabilityManager = new ProjectCapabilityManager(
            projectPath,
            relativeMainEntitlementsPath,
            null,
            mainTarget
        );
        mainCapabilityManager.AddPushNotifications(false);
        mainCapabilityManager.AddBackgroundModes(BackgroundModesOptions.RemoteNotifications);
        mainCapabilityManager.WriteToFile();
        
        project.AddFrameworkToProject(mainTarget, "UserNotifications.framework", false);
        project.AddBuildProperty(mainTarget, "CODE_SIGN_ENTITLEMENTS", relativeMainEntitlementsPath);

        project.WriteToFile(projectPath);
        
        string newPlistPath = Path.Combine(path, "Info.plist");
        PlistDocument plist = new PlistDocument();
        plist.ReadFromFile(newPlistPath);

        plist.root.SetString("NSUserTrackingUsageDescription", "Your data will be used to provide you a better and personalized ad experience.");
        plist.root.SetString("NSPhotoLibraryUsageDescription", "Allow to access photo library.");
        plist.root.SetString("NSCameraUsageDescription", "Allow to access camera.");
        plist.root.SetString("NSMicrophoneUsageDescription", "Allow to access microphone.");
        plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);

        var customDict = plist.root.CreateDict("NSAppTransportSecurity");
        customDict.SetBoolean("NSAllowsArbitraryLoads", true);
        customDict.SetBoolean("NSAllowsArbitraryLoadsInWebContent", true);
        customDict.SetBoolean("NSAllowsLocalNetworking", true);
        customDict.SetBoolean("NSAllowsArbitraryLoadsForMedia", true);

        File.WriteAllText(newPlistPath, plist.WriteToString());
        
        
        InitPodFile(path);
        
        System.Threading.Thread.Sleep(3000);
        
        //RunPodInstall(path);
    }

    private static void InitPodFile(string pathToBuiltProject)
    {
        string podfilePath = Path.Combine(pathToBuiltProject, "Podfile");

        string podfileContent = @"
source 'https://cdn.cocoapods.org/'

platform :ios, '13.0'

use_frameworks! :linkage => :static

target 'UnityFramework' do
  pod 'AppsFlyerFramework', '6.16.2'
  pod 'FirebaseAnalytics', '11.10.0'
  pod 'Firebase/Messaging', '11.10.0'
  pod 'Firebase/RemoteConfig', '11.10.0'
  pod 'UnityAds', '~> 4.12.0'
end

target 'Unity-iPhone' do
end

target 'notifications' do
  pod 'FirebaseAnalytics', '11.10.0'
  pod 'Firebase/Messaging', '11.10.0'
end
";

        File.WriteAllText(podfilePath, podfileContent);
    }

    private static void RunPodInstall(string iosBuildPath)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = "pod";
        startInfo.Arguments = "install";
        startInfo.WorkingDirectory = iosBuildPath;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;

        // Добавляем нужные переменные окружения для UTF-8
        startInfo.EnvironmentVariables["LANG"] = "en_US.UTF-8";
        startInfo.EnvironmentVariables["LC_ALL"] = "en_US.UTF-8";

        Process process = Process.Start(startInfo);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        UnityEngine.Debug.Log("pod install output: " + output);
        if (!string.IsNullOrEmpty(error))
            UnityEngine.Debug.LogError("pod install error: " + error);
    }

}
