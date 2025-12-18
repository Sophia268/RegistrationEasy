using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia;
using Avalonia.Android;
using RegistrationEasy.Services;

namespace RegistrationEasy.Android;

[Activity(
    Label = "RegistrationEasy",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/icon",
    MainLauncher = true,
    Name = "com.registrationeasy.app.MainActivity",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        try
        {
            CopyConfigFromAssets();

            App.OpenUrl = (url) =>
            {
                try
                {
                    var uri = global::Android.Net.Uri.Parse(url);
                    var intent = new global::Android.Content.Intent(global::Android.Content.Intent.ActionView, uri);
                    intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
                    global::Android.App.Application.Context.StartActivity(intent);
                }
                catch (System.Exception ex)
                {
                    global::Android.Util.Log.Error("RegistrationEasy", $"Cannot open url: {ex}");
                }
            };

            MachineIdProvider.PlatformGetMachineId = () =>
            {
                try
                {
                    return global::Android.Provider.Settings.Secure.GetString(ContentResolver, global::Android.Provider.Settings.Secure.AndroidId) ?? "ANDROID_UNKNOWN";
                }
                catch (System.Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error getting Android ID: {ex}");
                    return "ANDROID_ERROR";
                }
            };

            base.OnCreate(savedInstanceState);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FATAL ERROR in MainActivity.OnCreate: {ex}");
            // Log to Android logcat as well
            global::Android.Util.Log.Error("RegistrationEasy", $"FATAL ERROR in MainActivity.OnCreate: {ex}");
            throw; // Re-throw to let the app crash visibly if we can't recover
        }
    }

    private void CopyConfigFromAssets()
    {
        try
        {
            var destination = System.IO.Path.Combine(System.AppContext.BaseDirectory, "config.json");
            // Overwrite to ensure we have the latest config
            using var stream = Assets!.Open("config.json");
            using var dest = System.IO.File.Create(destination);
            stream.CopyTo(dest);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Config copy failed: {ex.Message}");
            global::Android.Util.Log.Error("RegistrationEasy", $"Config copy failed: {ex.Message}");
        }
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
