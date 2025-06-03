using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class PostprocessBuildPlayer
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget == BuildTarget.iOS)
        {
            // Настройка Info.plist
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
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
            File.WriteAllText(plistPath, plist.WriteToString());

            // Настройка проекта Xcode
            string projectPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            PBXProject pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(projectPath));
            string mainTargetGuid = pbxProject.GetUnityMainTargetGuid();
            string unityFrameworkTargetGuid = pbxProject.GetUnityFrameworkTargetGuid();
            pbxProject.SetBuildProperty(mainTargetGuid, "ENABLE_BITCODE", "NO");
            pbxProject.SetBuildProperty(unityFrameworkTargetGuid, "ENABLE_BITCODE", "NO");
            File.WriteAllText(projectPath, pbxProject.WriteToString());
        }
    }
}