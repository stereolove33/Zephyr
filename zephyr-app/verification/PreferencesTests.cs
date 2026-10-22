using System;
using System.Reflection;
using System.Web.Script.Serialization;

internal static class PreferencesTests
{
    static void Main(string[] args)
    {
        Type type = Assembly.LoadFrom(args[0]).GetType("SkinFusion.UiPreferences");
        var json = new JavaScriptSerializer();
        object defaults = json.Deserialize("{}", type);
        if (!(bool)type.GetProperty("minimizeToTray").GetValue(defaults, null))
            throw new Exception("Tray default changed");
        object saved = json.Deserialize("{\"autoRun\":true,\"minimizeToTray\":false}", type);
        if (!(bool)type.GetProperty("autoRun").GetValue(saved, null) ||
            (bool)type.GetProperty("minimizeToTray").GetValue(saved, null))
            throw new Exception("Saved settings changed");
        if (json.Deserialize(json.Serialize(saved), type) == null)
            throw new Exception("Preferences round trip failed");
        Console.WriteLine("Preferences defaults, saved settings and round trip passed.");
    }
}
