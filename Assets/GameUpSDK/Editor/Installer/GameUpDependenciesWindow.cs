using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace GameUpSDK.Installer
{
    /// <summary>
    /// Cửa sổ hướng dẫn cài đặt tất cả package phụ thuộc của GameUp SDK.
    /// Tự động xuất hiện khi SDK được cài lần đầu tiên qua Git URL Package.
    /// </summary>
    public class GameUpDependenciesWindow : EditorWindow
    {
        // ─── Định nghĩa các package phụ thuộc ────────────────────────────────────

        private enum InstallMethod { GitUrl, ScopedRegistry, OpenUrl }

        private class PackageDef
        {
            public string DisplayName;
            public string Description;
            public bool Required;

            // Tên assembly để detect xem package đã cài chưa
            public string AssemblyName;

            public InstallMethod Method;

            // Git URL (dùng khi Method == GitUrl)
            public string GitUrl;

            // Scoped registry (dùng khi Method == ScopedRegistry)
            public string RegistryName;
            public string RegistryUrl;
            public string[] RegistryScopes;
            public string PackageId;    // VD: "com.google.firebase.analytics"

            // URL tải thủ công (dùng khi Method == OpenUrl)
            public string DownloadUrl;
            public string DownloadLabel;

            // ── Runtime state ──
            public bool IsInstalled;
            public bool IsInstalling;
            public string InstallError;
        }

        private static readonly PackageDef[] s_packages =
        {
            new PackageDef
            {
                DisplayName   = "EDM4U — External Dependency Manager",
                Description   = "Bắt buộc. Giải quyết native dependency Android/iOS cho Firebase & AdMob.",
                Required      = true,
                AssemblyName  = "Google.VersionHandler",
                Method        = InstallMethod.GitUrl,
                GitUrl        = "https://github.com/googlesamples/unity-jar-resolver.git?path=upm",
            },
            new PackageDef
            {
                DisplayName   = "IronSource LevelPlay SDK",
                Description   = "Bắt buộc. Mediation chính: Banner, Interstitial, Rewarded.",
                Required      = true,
                AssemblyName  = "Unity.LevelPlay",
                Method        = InstallMethod.OpenUrl,
                DownloadUrl   = "https://developers.is.com/ironsource-mobile/unity/unity-plugin/",
                DownloadLabel = "Mở trang tải IronSource SDK →",
            },
            new PackageDef
            {
                DisplayName   = "Firebase SDK  (Analytics + Crashlytics + Remote Config)",
                Description   = "Bắt buộc. Analytics, crash reporting, remote configuration.",
                Required      = true,
                AssemblyName  = "Firebase.App",
                Method        = InstallMethod.OpenUrl,
                DownloadUrl   = "https://firebase.google.com/docs/unity/setup",
                DownloadLabel = "Mở trang tải Firebase Unity SDK →",
            },
            new PackageDef
            {
                DisplayName   = "Google Mobile Ads — AdMob",
                Description   = "Tùy chọn. Dùng cho App Open Ads (ngoài mediation IronSource).",
                Required      = false,
                AssemblyName  = "GoogleMobileAds",
                Method        = InstallMethod.OpenUrl,
                DownloadUrl   = "https://github.com/googlesamples/unity-admob-sdk/releases",
                DownloadLabel = "Mở trang tải AdMob Unity Plugin →",
            },
            new PackageDef
            {
                DisplayName   = "AppsFlyer Attribution SDK",
                Description   = "Tùy chọn. Mobile measurement & attribution.",
                Required      = false,
                AssemblyName  = "AppsFlyer",
                Method        = InstallMethod.GitUrl,
                GitUrl        = "https://github.com/AppsFlyerSDK/appsflyer-unity-plugin.git#upm",
            },
        };

        // ─── State ────────────────────────────────────────────────────────────────

        private Vector2 _scroll;
        private bool _isRefreshing;
        private bool _isBatchInstalling;

        // Queue để cài lần lượt từng package
        private readonly Queue<PackageDef> _installQueue = new Queue<PackageDef>();
        private AddRequest _currentAddRequest;
        private PackageDef _currentInstallingPackage;

        // ─── Static helpers ───────────────────────────────────────────────────────

        [MenuItem("GameUp SDK/Setup Dependencies")]
        public static void ShowWindow()
        {
            var win = GetWindow<GameUpDependenciesWindow>(true, "GameUp SDK — Setup Dependencies");
            win.minSize = new Vector2(560, 520);
            // Đặt kích thước ban đầu nếu window chưa được mở
            if (win.position.width < 560)
                win.position = new Rect(win.position.x, win.position.y, 620, 580);
            win.RefreshStatus();
        }

        /// <summary>
        /// Kiểm tra nhanh (đồng bộ) xem tất cả package bắt buộc đã cài chưa.
        /// Dùng bởi GameUpPackageInstaller để quyết định có mở window không.
        /// </summary>
        public static bool AreAllRequiredPackagesInstalled()
        {
            return s_packages
                .Where(p => p.Required)
                .All(p => IsAssemblyLoaded(p.AssemblyName));
        }

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            RefreshStatus();
        }

        private void OnDisable()
        {
            EditorApplication.update -= PollInstallQueue;
        }

        // ─── GUI ─────────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawHeader();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawPackageList();
            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField(
                "GameUp SDK — Setup Dependencies",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 });

            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Cài đặt các package phụ thuộc bên dưới để sử dụng GameUp SDK.\n" +
                "• Package \"tự động\": thêm trực tiếp qua Unity Package Manager.\n" +
                "• Package \"thủ công\": tải .unitypackage về rồi import vào project.",
                MessageType.Info);

            EditorGUILayout.Space(6);
        }

        private void DrawPackageList()
        {
            bool drewRequired = false, drewOptional = false;

            foreach (var pkg in s_packages)
            {
                // Section headers
                if (pkg.Required && !drewRequired)
                {
                    DrawSectionHeader("BẮT BUỘC");
                    drewRequired = true;
                }
                if (!pkg.Required && !drewOptional)
                {
                    EditorGUILayout.Space(8);
                    DrawSectionHeader("TÙY CHỌN");
                    drewOptional = true;
                }

                DrawPackageRow(pkg);
            }
        }

        private static void DrawSectionHeader(string title)
        {
            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                padding   = new RectOffset(4, 0, 6, 2),
            };
            EditorGUILayout.LabelField(title, style);
        }

        private void DrawPackageRow(PackageDef pkg)
        {
            bool isInstalling = pkg.IsInstalling || (_isBatchInstalling && _installQueue.Contains(pkg));
            Color boxColor    = pkg.IsInstalled ? new Color(0.18f, 0.45f, 0.18f, 0.3f)
                              : isInstalling    ? new Color(0.3f,  0.3f,  0.6f,  0.3f)
                              :                   new Color(0.45f, 0.18f, 0.18f, 0.3f);

            // Row background
            var rect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, rect.height + 2), boxColor);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);

            // Status icon
            string icon = pkg.IsInstalled ? "✓" : isInstalling ? "⟳" : "✗";
            var iconStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 14,
                normal    = { textColor = pkg.IsInstalled ? Color.green : isInstalling ? Color.yellow : Color.red },
                fixedWidth = 24,
            };
            GUILayout.Label(icon, iconStyle, GUILayout.Width(24));

            // Name + description
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(pkg.DisplayName, EditorStyles.boldLabel);
            var descStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            EditorGUILayout.LabelField(pkg.Description, descStyle);
            if (!string.IsNullOrEmpty(pkg.InstallError))
                EditorGUILayout.HelpBox(pkg.InstallError, MessageType.Error);
            EditorGUILayout.EndVertical();

            // Action button
            GUILayout.Space(4);
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !isInstalling && !_isBatchInstalling;

            if (pkg.IsInstalled)
            {
                var greenStyle = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = Color.green }, fontStyle = FontStyle.Bold };
                GUILayout.Label("Đã cài", greenStyle, GUILayout.Width(64));
            }
            else if (isInstalling)
            {
                EditorGUILayout.LabelField("Đang cài...", GUILayout.Width(84));
            }
            else
            {
                switch (pkg.Method)
                {
                    case InstallMethod.GitUrl:
                    case InstallMethod.ScopedRegistry:
                        if (GUILayout.Button("Cài tự động", GUILayout.Width(100)))
                            StartInstall(pkg);
                        break;

                    case InstallMethod.OpenUrl:
                        if (GUILayout.Button(pkg.DownloadLabel ?? "Tải thủ công →", GUILayout.Width(200)))
                            Application.OpenURL(pkg.DownloadUrl);
                        break;
                }
            }

            GUI.enabled = wasEnabled;
            GUILayout.Space(8);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(2);
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();

            // Refresh button
            bool wasEnabled = GUI.enabled;
            GUI.enabled = !_isBatchInstalling;

            if (GUILayout.Button("↻  Làm mới trạng thái", GUILayout.Height(30)))
                RefreshStatus();

            // Install All Auto button
            bool hasAutoUninstalled = s_packages.Any(
                p => !p.IsInstalled &&
                     (p.Method == InstallMethod.GitUrl || p.Method == InstallMethod.ScopedRegistry));

            if (hasAutoUninstalled)
            {
                if (GUILayout.Button("⬇  Cài tất cả (tự động)", GUILayout.Height(30)))
                    StartBatchInstall();
            }

            GUI.enabled = wasEnabled;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Manual install hint
            bool hasManualUninstalled = s_packages.Any(p => !p.IsInstalled && p.Method == InstallMethod.OpenUrl);
            if (hasManualUninstalled)
            {
                EditorGUILayout.HelpBox(
                    "Một số package cần cài thủ công:\n" +
                    "1. Nhấn nút \"Tải thủ công →\" để mở trang tải.\n" +
                    "2. Tải file .unitypackage về máy.\n" +
                    "3. Double-click file hoặc vào Assets > Import Package > Custom Package...\n" +
                    "4. Nhấn \"Làm mới trạng thái\" để kiểm tra lại.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(4);

            // Continue button
            bool allRequiredDone = AreAllRequiredPackagesInstalled();
            if (allRequiredDone)
            {
                EditorGUILayout.HelpBox(
                    "Tất cả package bắt buộc đã được cài đặt! " +
                    "Nhấn bên dưới để mở cửa sổ cấu hình SDK.",
                    MessageType.None);

                if (GUILayout.Button("→  Mở cấu hình SDK (GameUp SDK Setup)", GUILayout.Height(36)))
                {
                    GameUpPackageInstaller.MarkSetupComplete();
                    Close();
                    EditorApplication.ExecuteMenuItem("GameUp SDK/Setup");
                }
            }
        }

        // ─── Install logic ────────────────────────────────────────────────────────

        private void StartInstall(PackageDef pkg)
        {
            if (pkg.IsInstalling) return;
            pkg.IsInstalling = true;
            pkg.InstallError = null;
            Repaint();

            switch (pkg.Method)
            {
                case InstallMethod.GitUrl:
                    EnqueueGitInstall(pkg);
                    break;

                case InstallMethod.ScopedRegistry:
                    AddScopedRegistryAndPackage(pkg);
                    break;
            }
        }

        private void StartBatchInstall()
        {
            _isBatchInstalling = true;
            _installQueue.Clear();

            foreach (var pkg in s_packages)
            {
                if (pkg.IsInstalled) continue;
                if (pkg.Method != InstallMethod.GitUrl && pkg.Method != InstallMethod.ScopedRegistry) continue;

                pkg.InstallError = null;
                _installQueue.Enqueue(pkg);
            }

            ProcessNextInQueue();
        }

        private void EnqueueGitInstall(PackageDef pkg)
        {
            _installQueue.Clear();
            _installQueue.Enqueue(pkg);
            ProcessNextInQueue();
        }

        private void ProcessNextInQueue()
        {
            if (_installQueue.Count == 0)
            {
                _isBatchInstalling       = false;
                _currentInstallingPackage = null;
                _currentAddRequest        = null;
                EditorApplication.update -= PollInstallQueue;
                RefreshStatus();
                return;
            }

            var pkg = _installQueue.Peek();
            _currentInstallingPackage = pkg;
            pkg.IsInstalling = true;
            Repaint();

            _currentAddRequest = Client.Add(pkg.GitUrl);
            EditorApplication.update += PollInstallQueue;
        }

        private void PollInstallQueue()
        {
            if (_currentAddRequest == null || !_currentAddRequest.IsCompleted) return;

            EditorApplication.update -= PollInstallQueue;

            var pkg = _currentInstallingPackage;
            if (pkg != null)
            {
                pkg.IsInstalling = false;

                if (_currentAddRequest.Status == StatusCode.Success)
                {
                    pkg.IsInstalled  = true;
                    pkg.InstallError = null;
                }
                else
                {
                    pkg.InstallError = _currentAddRequest.Error?.message ?? "Cài thất bại.";
                }
            }

            _installQueue.Dequeue();
            _currentAddRequest        = null;
            _currentInstallingPackage = null;

            ProcessNextInQueue();
            Repaint();
        }

        private void AddScopedRegistryAndPackage(PackageDef pkg)
        {
            // Đọc manifest.json, thêm scoped registry + dependency, ghi lại
            string manifestPath = System.IO.Path.Combine(
                Application.dataPath, "..", "Packages", "manifest.json");

            try
            {
                string json    = System.IO.File.ReadAllText(manifestPath);
                var    manifest = SimpleJsonHelper.ParseObject(json);

                // Thêm scoped registry nếu chưa có
                if (!string.IsNullOrEmpty(pkg.RegistryUrl))
                {
                    if (!manifest.ContainsKey("scopedRegistries"))
                        manifest["scopedRegistries"] = new List<object>();

                    var registries = (List<object>)manifest["scopedRegistries"];
                    bool found = registries.OfType<Dictionary<string, object>>()
                        .Any(r => r.TryGetValue("url", out var u) && u?.ToString() == pkg.RegistryUrl);

                    if (!found)
                    {
                        registries.Add(new Dictionary<string, object>
                        {
                            ["name"]   = pkg.RegistryName,
                            ["url"]    = pkg.RegistryUrl,
                            ["scopes"] = pkg.RegistryScopes?.ToList<object>() ?? new List<object>(),
                        });
                    }
                }

                // Thêm dependency
                if (!manifest.ContainsKey("dependencies"))
                    manifest["dependencies"] = new Dictionary<string, object>();

                var deps = (Dictionary<string, object>)manifest["dependencies"];
                if (!deps.ContainsKey(pkg.PackageId))
                    deps[pkg.PackageId] = "latest";

                System.IO.File.WriteAllText(manifestPath, SimpleJsonHelper.Serialize(manifest));
                AssetDatabase.Refresh();

                pkg.IsInstalling = false;
                pkg.IsInstalled  = true;
            }
            catch (Exception ex)
            {
                pkg.IsInstalling = false;
                pkg.InstallError = "Lỗi khi sửa manifest.json: " + ex.Message;
            }

            Repaint();
        }

        // ─── Status refresh ───────────────────────────────────────────────────────

        private void RefreshStatus()
        {
            foreach (var pkg in s_packages)
            {
                pkg.IsInstalled  = IsAssemblyLoaded(pkg.AssemblyName);
                pkg.IsInstalling = false;
                pkg.InstallError = null;
            }
            Repaint();
        }

        private static bool IsAssemblyLoaded(string assemblyName)
        {
            if (string.IsNullOrEmpty(assemblyName)) return false;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (string.Equals(asm.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    // ─── Minimal JSON helper (không dùng Newtonsoft/JsonUtility để giữ assembly sạch) ───

    internal static class SimpleJsonHelper
    {
        public static Dictionary<string, object> ParseObject(string json)
        {
            // Dùng Unity built-in JsonUtility không hỗ trợ Dictionary,
            // nên parse thủ công phần dependencies/scopedRegistries cần thiết.
            // Thực tế: dùng regex-free approach với index tracking.
            json = json.Trim();
            int idx = 0;
            return (Dictionary<string, object>)ParseValue(json, ref idx);
        }

        private static object ParseValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) return null;

            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't') { i += 4; return true; }
            if (c == 'f') { i += 5; return false; }
            if (c == 'n') { i += 4; return null; }
            return ParseNumber(s, ref i);
        }

        private static Dictionary<string, object> ParseObject(string s, ref int i)
        {
            var dict = new Dictionary<string, object>();
            i++; // skip '{'
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return dict; }

            while (i < s.Length)
            {
                SkipWhitespace(s, ref i);
                string key = ParseString(s, ref i);
                SkipWhitespace(s, ref i);
                i++; // skip ':'
                object val = ParseValue(s, ref i);
                dict[key] = val;
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; break; }
            }
            return dict;
        }

        private static List<object> ParseArray(string s, ref int i)
        {
            var list = new List<object>();
            i++; // skip '['
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }

            while (i < s.Length)
            {
                list.Add(ParseValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; break; }
            }
            return list;
        }

        private static string ParseString(string s, ref int i)
        {
            i++; // skip opening '"'
            var sb = new System.Text.StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c == '\\' && i < s.Length)
                {
                    char esc = s[i++];
                    switch (esc)
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case '/':  sb.Append('/');  break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        default:   sb.Append(esc);  break;
                    }
                }
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static object ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '.' || s[i] == 'e' || s[i] == 'E' || s[i] == '+'))
                i++;
            string num = s.Substring(start, i - start);
            if (int.TryParse(num, out int iv)) return iv;
            if (double.TryParse(num, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double dv)) return dv;
            return num;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        public static string Serialize(object obj, int indent = 0)
        {
            string pad  = new string(' ', indent * 2);
            string pad1 = new string(' ', (indent + 1) * 2);

            switch (obj)
            {
                case null:    return "null";
                case bool b:  return b ? "true" : "false";
                case int iv:  return iv.ToString();
                case long lv: return lv.ToString();
                case double dv:
                    return dv.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case string sv:
                    return "\"" + sv.Replace("\\", "\\\\").Replace("\"", "\\\"")
                                    .Replace("\n", "\\n").Replace("\r", "\\r")
                                    .Replace("\t", "\\t") + "\"";

                case Dictionary<string, object> dict:
                {
                    if (dict.Count == 0) return "{}";
                    var lines = dict.Select(
                        kv => pad1 + "\"" + kv.Key + "\": " + Serialize(kv.Value, indent + 1));
                    return "{\n" + string.Join(",\n", lines) + "\n" + pad + "}";
                }

                case List<object> list:
                {
                    if (list.Count == 0) return "[]";
                    var lines = list.Select(item => pad1 + Serialize(item, indent + 1));
                    return "[\n" + string.Join(",\n", lines) + "\n" + pad + "]";
                }

                default:
                    return "\"" + obj.ToString() + "\"";
            }
        }
    }
}
