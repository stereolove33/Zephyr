using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using SkinFusion;

internal static class UpdateCheckerTests
{
    static Dictionary<string, object> Release(string version, bool draft = false, bool prerelease = false)
    {
        string tag = "v" + version;
        return new Dictionary<string, object> {
            { "tag_name", tag }, { "draft", draft }, { "prerelease", prerelease },
            { "assets", new object[] { new Dictionary<string, object> {
                { "name", "Zephyr-v" + version + ".zip" }, { "state", "uploaded" }, { "size", 1024 },
                { "browser_download_url", "https://github.com/stereolove33/Zephyr/releases/download/" + tag + "/Zephyr-v" + version + ".zip" }
            } } }
        };
    }
    static void Expect(Dictionary<string, object> data, string state)
    {
        var actual = UpdateChecker.Evaluate(data);
        if (Convert.ToString(actual["state"]) != state) throw new Exception("Expected " + state);
    }
    static Dictionary<string, object> Asset(Dictionary<string, object> data)
    {
        return (Dictionary<string, object>)((object[])data["assets"])[0];
    }
    static int Main(string[] args)
    {
        Expect(Release("1.0.1"), "available");
        Expect(Release("1.0.0"), "current");
        Expect(Release("1.0.2"), "available");
        Expect(Release("1.0.10"), "available");
        Expect(Release("1.10.0"), "available");
        Expect(Release("2.0.0"), "available");
        Expect(Release("2.0.0", true), "unavailable");
        Expect(Release("2.0.0", false, true), "unavailable");
        Expect(Release("2.0.0-beta.1"), "unavailable");
        Expect(Release("garbage"), "unavailable");
        Expect(Release("01.0.0"), "unavailable");
        Expect(Release("999999999999.0.0"), "unavailable");
        Expect(null, "unavailable");
        var missing = Release("2.0.0"); missing["assets"] = new object[0]; Expect(missing, "unavailable");
        var unsafeUrl = Release("2.0.0"); Asset(unsafeUrl)["browser_download_url"] = "https://example.com/Zephyr.zip"; Expect(unsafeUrl, "unavailable");
        var pending = Release("2.0.0"); Asset(pending)["state"] = "starter"; Expect(pending, "unavailable");
        var empty = Release("2.0.0"); Asset(empty)["size"] = 0; Expect(empty, "unavailable");
        var malformed = Release("2.0.0"); Asset(malformed)["size"] = "broken"; Expect(malformed, "unavailable");
        var spoof = Release("2.0.0"); spoof["html_url"] = "file:///C:/untrusted.exe";
        var verified = UpdateChecker.Evaluate(spoof);
        if (Convert.ToString(verified["url"]) != "https://github.com/stereolove33/Zephyr/releases/tag/v2.0.0") throw new Exception("Untrusted release URL");
        if (args.Length > 0) Expect(new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(File.ReadAllText(args[0])), "current");
        Console.WriteLine("Update checks passed: version ordering, stable releases, valid assets and trusted URLs.");
        return 0;
    }
}
