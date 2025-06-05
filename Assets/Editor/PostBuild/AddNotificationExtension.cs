using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
using System.IO;
using System.Text;

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

      var plistContent = new StringBuilder();
      plistContent.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
      plistContent.AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
      plistContent.AppendLine("<plist version=\"1.0\">");
      plistContent.AppendLine("<dict>");
      plistContent.AppendLine("  <key>NSExtension</key>");
      plistContent.AppendLine("  <dict>");
      plistContent.AppendLine("    <key>NSExtensionPointIdentifier</key>");
      plistContent.AppendLine("    <string>com.apple.usernotifications.service</string>");
      plistContent.AppendLine("    <key>NSExtensionPrincipalClass</key>");
      plistContent.AppendLine("    <string>$(PRODUCT_MODULE_NAME).NotificationService</string>");
      plistContent.AppendLine("  </dict>");
      plistContent.AppendLine("</dict>");
      plistContent.AppendLine("</plist>");
      File.WriteAllText(plistPath, plistContent.ToString());

      var swiftContent = new StringBuilder();
      swiftContent.AppendLine("import UserNotifications");
      swiftContent.AppendLine("import FirebaseMessaging");
      swiftContent.AppendLine();
      swiftContent.AppendLine("class NotificationService: UNNotificationServiceExtension {");
      swiftContent.AppendLine("  var contentHandler: ((UNNotificationContent) -> Void)?");
      swiftContent.AppendLine("  var bestAttemptContent: UNMutableNotificationContent?");
      swiftContent.AppendLine();
      swiftContent.AppendLine("  override func didReceive(_ request: UNNotificationRequest, withContentHandler contentHandler: @escaping (UNNotificationContent) -> Void) {");
      swiftContent.AppendLine("    self.contentHandler = contentHandler");
      swiftContent.AppendLine("    bestAttemptContent = request.content.mutableCopy() as? UNMutableNotificationContent");
      swiftContent.AppendLine();
      swiftContent.AppendLine("    guard let bestAttemptContent = bestAttemptContent else { return }");
      swiftContent.AppendLine();
      swiftContent.AppendLine("    FIRMessagingExtensionHelper().populateNotificationContent(");
      swiftContent.AppendLine("      bestAttemptContent,");
      swiftContent.AppendLine("      withContentHandler: contentHandler)");
      swiftContent.AppendLine("  }");
      swiftContent.AppendLine();
      swiftContent.AppendLine("  override func serviceExtensionTimeWillExpire() {");
      swiftContent.AppendLine("    if let contentHandler = contentHandler, let bestAttemptContent = bestAttemptContent {");
      swiftContent.AppendLine("      contentHandler(bestAttemptContent)");
      swiftContent.AppendLine("    }");
      swiftContent.AppendLine("  }");
      swiftContent.AppendLine("}");
      File.WriteAllText(swiftPath, swiftContent.ToString());

      string swiftFileGUID = project.AddFile(relativeSwiftPath, relativeSwiftPath, PBXSourceTree.Source);
      string extensionTarget = project.AddAppExtension(mainTarget, extensionTargetName, extensionBundleId, relativePlistPath);

      string plistGuid = project.FindFileGuidByProjectPath(relativePlistPath);
      if (!string.IsNullOrEmpty(plistGuid))
      {
          string buildPhase = project.GetResourcesBuildPhaseByTarget(extensionTarget);
          project.RemoveFileFromBuild(buildPhase, plistGuid);
      }

      project.AddFileToBuild(extensionTarget, swiftFileGUID);

      project.SetBuildProperty(extensionTarget, "INFOPLIST_FILE", relativePlistPath);
      project.SetBuildProperty(extensionTarget, "SWIFT_VERSION", "5.0");
      project.SetBuildProperty(extensionTarget, "IPHONEOS_DEPLOYMENT_TARGET", "11.0");
      project.SetBuildProperty(extensionTarget, "CODE_SIGN_STYLE", "Automatic");

      RemoveEmbedAppExtensionsPhase(path, project, mainTarget);
      project.WriteToFile(projectPath);

      var entitlementsContent = new StringBuilder();
      entitlementsContent.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
      entitlementsContent.AppendLine("<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">");
      entitlementsContent.AppendLine("<plist version=\"1.0\">");
      entitlementsContent.AppendLine("<dict>");
      entitlementsContent.AppendLine("  <key>aps-environment</key>");
      entitlementsContent.AppendLine("  <string>development</string>");
      entitlementsContent.AppendLine("</dict>");
      entitlementsContent.AppendLine("</plist>");
      File.WriteAllText(entitlementsExtensionPath, entitlementsContent.ToString());

      var extensionCapabilityManager = new ProjectCapabilityManager(
          projectPath,
          relativeEntitlementsExtensionPath,
          null,
          extensionTarget
      );
      extensionCapabilityManager.AddPushNotifications(false);
      extensionCapabilityManager.WriteToFile();

      project.ReadFromFile(projectPath);
      project.AddBuildProperty(extensionTarget, "CODE_SIGN_ENTITLEMENTS", relativeEntitlementsExtensionPath);

      string mainEntitlementsPath = Path.Combine(path, "main.entitlements");
      string relativeMainEntitlementsPath = "main.entitlements";
      File.WriteAllText(mainEntitlementsPath, entitlementsContent.ToString());

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

      var podfileBuilder = new StringBuilder();
      podfileBuilder.AppendLine("source 'https://cdn.cocoapods.org/'");
      podfileBuilder.AppendLine();
      podfileBuilder.AppendLine("platform :ios, '13.0'");
      podfileBuilder.AppendLine();
      podfileBuilder.AppendLine("use_frameworks!");
      podfileBuilder.AppendLine();
      podfileBuilder.AppendLine("target 'UnityFramework' do");
      podfileBuilder.AppendLine("  pod 'AppsFlyerFramework', '6.16.2'");
      podfileBuilder.AppendLine("  pod 'FirebaseAnalytics', '11.10.0'");
      podfileBuilder.AppendLine("  pod 'Firebase/Messaging', '11.10.0'");
      podfileBuilder.AppendLine("end");
      podfileBuilder.AppendLine();
      podfileBuilder.AppendLine("target 'Unity-iPhone' do");
      podfileBuilder.AppendLine("end");
      podfileBuilder.AppendLine();
      podfileBuilder.AppendLine("target 'notifications' do");
      podfileBuilder.AppendLine("  pod 'FirebaseAnalytics', '11.10.0'");
      podfileBuilder.AppendLine("  pod 'Firebase/Messaging', '11.10.0'");
      podfileBuilder.AppendLine("end");

      File.WriteAllText(podfilePath, podfileBuilder.ToString());
      File.WriteAllText(Path.Combine(pathToBuiltProject, "podfile_ready"), "ok");
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