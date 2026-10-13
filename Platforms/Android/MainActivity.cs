using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace UniversalSearch;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode |
                          ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    private const int FolderPickerRequestCode = 7421;
    private TaskCompletionSource<string?>? _folderPickerCompletion;

    public static async Task<string?> PickFolderAsync()
    {
        if (Platform.CurrentActivity is not MainActivity activity)
            throw new InvalidOperationException("The Android activity is not available.");
        return await activity.PickFolderInternalAsync();
    }

    private Task<string?> PickFolderInternalAsync()
    {
        if (_folderPickerCompletion is not null)
            throw new InvalidOperationException("A folder picker is already open.");
        _folderPickerCompletion = new TaskCompletionSource<string?>();
        var intent = new Intent(Intent.ActionOpenDocumentTree);
        intent.AddFlags(ActivityFlags.GrantReadUriPermission |
                        ActivityFlags.GrantWriteUriPermission |
                        ActivityFlags.GrantPersistableUriPermission);
        StartActivityForResult(intent, FolderPickerRequestCode);
        return _folderPickerCompletion.Task;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode != FolderPickerRequestCode || _folderPickerCompletion is null) return;
        var completion = _folderPickerCompletion;
        _folderPickerCompletion = null;
        try
        {
            if (resultCode != Result.Ok || data?.Data is null)
            {
                completion.TrySetResult(null);
                return;
            }
            var uri = data.Data;
            var grantedFlags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
            ContentResolver?.TakePersistableUriPermission(uri, grantedFlags);
            completion.TrySetResult(uri.ToString());
        }
        catch (Exception ex) { completion.TrySetException(ex); }
    }
}