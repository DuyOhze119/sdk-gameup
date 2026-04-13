using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

#if UNITY_IOS
using System.IO;
using UnityEditor.iOS.Xcode;
#endif

namespace GameUpSDK.Editor
{
    public class GameUpPostProcess : IPostprocessBuildWithReport
    {
        private const string TrackingUsageDescription = "Dữ liệu này giúp hiển thị quảng cáo phù hợp hơn với bạn.";

        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
#if UNITY_IOS
            if (report.summary.platform != BuildTarget.iOS)
                return;

            var plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString("NSUserTrackingUsageDescription", TrackingUsageDescription);
            File.WriteAllText(plistPath, plist.WriteToString());
#endif
        }
    }
}
