using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SkinFusion
{
    internal sealed class UpdateChecker
    {
        internal const string CurrentVersion = "1.0.1";
        private const string Releases = "https://github.com/stereolove33/Zephyr/releases/";
        private Task<object> pending;
        private DateTime checkedAt = DateTime.MinValue;
        private string releaseUrl;

        // Called on the WebView thread. Repeated requests share one check and
        // are cached briefly to avoid GitHub's unauthenticated rate limit.
        internal Task<object> Check()
        {
            if (pending == null || (pending.IsCompleted && DateTime.UtcNow - checkedAt > TimeSpan.FromMinutes(1)))
            {
                checkedAt = DateTime.UtcNow;
                pending = Fetch();
            }
            return pending;
        }

        private async Task<object> Fetch()
        {
            releaseUrl = null;
            try
            {
                using (var handler = new HttpClientHandler { AllowAutoRedirect = false, SslProtocols = SslProtocols.Tls12 })
                using (var client = new HttpClient(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.MaxResponseContentBufferSize = 1024 * 1024;
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Zephyr/" + CurrentVersion);
                    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
                    string body = await client.GetStringAsync("https://api.github.com/repos/stereolove33/Zephyr/releases/latest");
                    var serializer = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
                    var data = serializer.Deserialize<Dictionary<string, object>>(body);
                    var result = Evaluate(data);
                    if (Convert.ToString(result["state"]) == "available") releaseUrl = Convert.ToString(result["url"]);
                    return result;
                }
            }
            catch (Exception)
            {
                // Network failures must never stop skin loading or show a modal.
                return new { state = "unavailable", currentVersion = CurrentVersion };
            }
        }

        internal static Version ParseVersion(string text)
        {
            var match = Regex.Match(text ?? "", @"\Av?(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\z");
            int major, minor, patch;
            if (!match.Success || !Int32.TryParse(match.Groups[1].Value, out major)
                || !Int32.TryParse(match.Groups[2].Value, out minor)
                || !Int32.TryParse(match.Groups[3].Value, out patch)) return null;
            return new Version(major, minor, patch);
        }

        internal static Dictionary<string, object> Evaluate(Dictionary<string, object> data)
        {
            var result = new Dictionary<string, object> { { "state", "unavailable" }, { "currentVersion", CurrentVersion } };
            object tag, draft, prerelease, assets;
            if (data == null || !data.TryGetValue("tag_name", out tag) || !(tag is string)
                || !data.TryGetValue("draft", out draft) || !(draft is bool) || (bool)draft
                || !data.TryGetValue("prerelease", out prerelease) || !(prerelease is bool) || (bool)prerelease) return result;
            Version version = ParseVersion((string)tag);
            if (version == null) return result;
            string displayVersion = version.ToString(3);
            string expectedAsset = "Zephyr-v" + displayVersion + ".zip";
            string expectedDownload = Releases + "download/" + Uri.EscapeDataString((string)tag) + "/" + expectedAsset;
            bool downloadable = false;
            if (data.TryGetValue("assets", out assets) && assets is IEnumerable)
            {
                foreach (object entry in (IEnumerable)assets)
                {
                    var asset = entry as Dictionary<string, object>;
                    object name, state, url, size;
                    if (asset != null && asset.TryGetValue("name", out name) && Convert.ToString(name) == expectedAsset
                        && asset.TryGetValue("state", out state) && Convert.ToString(state) == "uploaded"
                        && asset.TryGetValue("browser_download_url", out url) && Convert.ToString(url) == expectedDownload
                        && asset.TryGetValue("size", out size) && (size is int || size is long) && Convert.ToInt64(size) > 0)
                        downloadable = true;
                }
            }
            if (!downloadable) return result;
            result["state"] = version.CompareTo(ParseVersion(CurrentVersion)) > 0 ? "available" : "current";
            result["version"] = displayVersion;
            result["url"] = Releases + "tag/" + Uri.EscapeDataString((string)tag);
            return result;
        }

        internal object OpenRelease()
        {
            if (releaseUrl == null) throw new InvalidOperationException("No verified update is available.");
            Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true });
            return null;
        }
    }
}
