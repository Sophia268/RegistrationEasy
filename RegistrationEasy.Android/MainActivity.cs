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
        MachineIdProvider.PlatformGetMachineId = () =>
        {
            try
            {
                return global::Android.Provider.Settings.Secure.GetString(ContentResolver, global::Android.Provider.Settings.Secure.AndroidId) ?? "ANDROID_UNKNOWN";
            }
            catch
            {
                return "ANDROID_ERROR";
            }
        };

        base.OnCreate(savedInstanceState);
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont();
    }
}
