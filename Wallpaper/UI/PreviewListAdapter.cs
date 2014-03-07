using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.Json;
using Android.Graphics;
using Android.Views.Animations;
using System.Threading;
using Tsanie.Common;

namespace Wallpaper
{
    class PreviewListAdapter : BaseAdapter, Android.Widget.AbsListView.IOnScrollListener
    {
        static Random rand = new Random();

        JsonArray pictures;
        Bitmap[] bitmaps;
        MainActivity main;

        public JsonArray Pictures { get { return pictures; } }

        public PreviewListAdapter(byte[] result, MainActivity main)
        {
            this.main = main;
            ReadResult(result);
        }

        public void ReadResult(byte[] result)
        {
            string json = Encoding.UTF8.GetString(result);
            try
            {
                this.pictures = (JsonArray)JsonArray.Parse(json);
                this.bitmaps = new Bitmap[this.pictures.Count];
            }
            catch
            {
                //var err = JsonValue.Parse(json);
                //view.Post(delegate
                //{
                //    Toast.MakeText(main, (string)err["msg"], ToastLength.Long).Show();
                //});
                this.pictures = new JsonArray(new JsonValue[0]);
            }
        }

        public override int Count
        {
            get { return pictures.Count; }
        }

        public override Java.Lang.Object GetItem(int position)
        {
            return position;
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        void LoadBitmap(ImageView iv, Bitmap bmp, View progress)
        {
            iv.SetImageBitmap(bmp);
            iv.Alpha = 1;
            AlphaAnimation alpha = new AlphaAnimation(0, 1);
            alpha.Duration = MainActivity.ANIMATION_DURATION;
            iv.StartAnimation(alpha);

            if (progress != null)
            {
                alpha = new AlphaAnimation(1, 0);
                alpha.Duration = MainActivity.ANIMATION_DURATION;
                alpha.AnimationEnd += delegate { progress.Visibility = ViewStates.Gone; };
                progress.StartAnimation(alpha);
            }
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            Holder holder;
            if (convertView == null)
            {
                var flater = LayoutInflater.From(main);
                convertView = flater.Inflate(Resource.Layout.item, null);
                holder = new Holder
                {
                    textSize = convertView.FindViewById<TextView>(Resource.Id.textSize),
                    textTag = convertView.FindViewById<TextView>(Resource.Id.textTag),
                    imagePreview = convertView.FindViewById<ImageView>(Resource.Id.imagePreview),
                    progress = convertView.FindViewById(Resource.Id.progressPreview)
                };
                convertView.Tag = holder;
            }
            else
            {
                holder = (Holder)convertView.Tag;
            }

            JsonValue val = pictures[position];
            holder.textSize.Text = string.Format("{0}x{1} ({2})", val["width"], val["height"], position);
            holder.textTag.Text = "tags: " + val["tags"];
            holder.imagePreview.Alpha = 0;
            Bitmap bmp = bitmaps[position];
            if (bmp != null)
            {
                holder.progress.Alpha = 0;
                // cache
                LoadBitmap(holder.imagePreview, bmp, null);
            }
            else
            {
                holder.progress.Alpha = 1;
                holder.progress.Visibility = ViewStates.Visible;
                string url = val["preview_url"];
                new Thread(delegate()
                {
                    byte[] data = NetUtility.GetData(url);
                    holder.imagePreview.Post(delegate
                    {
                        bmp = BitmapFactory.DecodeByteArray(data, 0, data.Length);
                        bitmaps[position] = bmp;
                        LoadBitmap(holder.imagePreview, bmp, holder.progress);
                    });
                }).Start();
            }

            return convertView;
        }

        class Holder : Java.Lang.Object
        {
            public TextView textSize;
            public TextView textTag;
            public ImageView imagePreview;
            public View progress;
        }

        int firstItemIndex;
        ScrollState scrollState;

        public int FirstItemIndex { get { return firstItemIndex; } }
        public ScrollState ScrollState { get { return scrollState; } }

        public void OnScroll(AbsListView view, int firstVisibleItem, int visibleItemCount, int totalItemCount)
        {
            this.firstItemIndex = firstVisibleItem;
        }

        public void OnScrollStateChanged(AbsListView view, ScrollState scrollState)
        {
            this.scrollState = scrollState;
        }
    }
}