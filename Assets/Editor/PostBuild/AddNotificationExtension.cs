using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
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

      if (!File.Exists(swiftPath)) {
          UnityEngine.Debug.LogError("🔥 Swift-файл не создан! " + swiftPath);
      }

      // === Add Extension Target and files ===
      string swiftFileGUID = project.AddFile(relativeSwiftPath, relativeSwiftPath, PBXSourceTree.Source);
      string extensionTarget = project.AddAppExtension(mainTarget, extensionTargetName, extensionBundleId, relativePlistPath);
      project.AddFileToBuild(extensionTarget, swiftFileGUID);

      project.SetBuildProperty(extensionTarget, "SWIFT_VERSION", "5.0");
      project.SetBuildProperty(extensionTarget, "IPHONEOS_DEPLOYMENT_TARGET", "11.0");
      project.SetBuildProperty(extensionTarget, "CODE_SIGN_STYLE", "Automatic");

      RemoveEmbedAppExtensionsPhase(path, project, mainTarget);

      project.WriteToFile(projectPath); // сохранить после создания таргета

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
</dict> 
</plist>");

      var mainCapabilityManager = new ProjectCapabilityManager(
          projectPath,
          relativeMainEntitlementsPath,
          null,
          mainTarget
      );
      mainCapabilityManager.AddPushNotifications(false);
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

  private static void RemoveEmbedAppExtensionsPhase(string pathToBuiltProject, PBXProject project, string targetGuid)
  {
      string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
      string contents = File.ReadAllText(projectPath);

      const string marker = "/* Embed App Extensions */";
      int index = contents.IndexOf(marker);
      if (index == -1)
      {
          UnityEngine.Debug.Log("No Embed App Extensions phase found.");
          return;
      }

      int start = contents.LastIndexOf("/* Begin PBXCopyFilesBuildPhase section */", index);
      if (start == -1) start = contents.LastIndexOf("PBXCopyFilesBuildPhase", index);
      if (start == -1) return;

      int end = contents.IndexOf("};", index);
      if (end == -1) return;

      int blockStart = contents.LastIndexOf("\n", start) + 1;
      int blockEnd = contents.IndexOf("\n", end) + 1;

      string toRemove = contents.Substring(blockStart, blockEnd - blockStart);
      contents = contents.Replace(toRemove, "");

      File.WriteAllText(projectPath, contents);
      project.ReadFromFile(projectPath);
  }
}
