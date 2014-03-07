using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Tsanie.Common;
using System.Text;
using System.Json;
using System.Collections.Generic;
using Android.Graphics;
using System.Threading;
using Android.Graphics.Drawables;
using Android.Views.Animations;

namespace Wallpaper
{
    [Activity(Label = "@string/ApplicationName", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        public const int ANIMATION_DURATION = 400;
        public const int NOTIFICATION_DOWNLOAD = 0;

        ProgressBar loading;
        PreviewListView listPreview;
        PreviewListAdapter adapter;

        Notification notification;
        NotificationManager manager;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.main);

            // Get elements from the layout resource,
            loading = FindViewById<ProgressBar>(Resource.Id.progressBar1);
            GridLayout root = FindViewById<GridLayout>(Resource.Id.main_root);
            listPreview = new PreviewListView(this);
            root.AddView(listPreview, GridLayout.LayoutParams.MatchParent, GridLayout.LayoutParams.MatchParent);

            listPreview.ItemLongClick += listPreview_ItemLongClick;

            new Thread(loadPreviews).Start();
        }

        void listPreview_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            if (notification != null)
            {
                Toast.MakeText(this, "不要着急，一个一个下。", ToastLength.Short).Show();
                return;
            }

            notification = new Notification(Resource.Drawable.icon, "开始下载", Java.Lang.JavaSystem.CurrentTimeMillis());
            notification.Flags = NotificationFlags.OngoingEvent | NotificationFlags.NoClear;
            var picture = adapter.Pictures[e.Position - 1];
            string filename = picture["file_url"];
            filename = filename.Substring(filename.LastIndexOf('/') + 1);
            notification.ContentView = getDownloadView(filename, 0, 0);

            manager = ((NotificationManager)GetSystemService(Context.NotificationService));
            manager.Notify(NOTIFICATION_DOWNLOAD, notification);
            new Thread(downloadPicture).Start(picture);
        }

        RemoteViews getDownloadView(string file, int total, int position)
        {
            var view = new RemoteViews(PackageName, Resource.Layout.notification);
            view.SetTextViewText(Resource.Id.textNotificationFile, file);
            view.SetProgressBar(Resource.Id.progressDownloader, total, position, total == 0);
            return view;
        }

        void loadPreviews()
        {
            byte[] result = NetUtility.GetData("https://yande.re/post/index.json?page=1&limit=20");
            adapter = new PreviewListAdapter(result, this);
            listPreview.Post(delegate
            {
                listPreview.Adapter = adapter;
                listPreview.SetOnScrollListener(adapter);
                AlphaAnimation alpha = new AlphaAnimation(1, 0);
                alpha.Duration = ANIMATION_DURATION;
                alpha.AnimationEnd += delegate { loading.Visibility = ViewStates.Gone; };
                loading.StartAnimation(alpha);
            });
            result = null;
        }

        void downloadPicture(object o)
        {
            JsonValue picture = (JsonValue)o;
            int length = (int)picture["file_size"];
            if (Android.OS.Environment.MediaMounted == Android.OS.Environment.ExternalStorageState)
            {
                string filename = picture["file_url"];
                filename = filename.Substring(filename.LastIndexOf('/') + 1);
                byte[] data = NetUtility.GetData(picture["file_url"], pos =>
                {
                    notification.ContentView = getDownloadView(filename, length, (int)pos);
                    manager.Notify(NOTIFICATION_DOWNLOAD, notification);
                });

                Java.IO.File sdcardDir = Android.OS.Environment.ExternalStorageDirectory;
                string path = sdcardDir.Path + "/Android/data/org.tsanie.Wallpaper_data/images";
                Java.IO.File path1 = new Java.IO.File(path);
                if (!path1.Exists())
                {
                    path1.Mkdirs();
                }

                // save
                using (var writer = new System.IO.FileStream(path + "/" + filename, System.IO.FileMode.Create))
                {
                    writer.Write(data, 0, data.Length);
                    writer.Flush();
                }

                listPreview.Post(delegate
                {
                    Toast.MakeText(this, "下载完成", ToastLength.Long).Show();
                });
            }
            else
            {
                listPreview.Post(delegate
                {
                    Toast.MakeText(this, "无法写入sd卡", ToastLength.Long).Show();
                });
            }

            manager.Cancel(NOTIFICATION_DOWNLOAD);
            notification = null;
        }
    }
}

