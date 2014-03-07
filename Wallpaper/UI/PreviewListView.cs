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
using Android.Views.Animations;
using Android.Animation;
using System.Threading;
using Tsanie.Common;

namespace Wallpaper
{
    enum PreviewState
    {
        Done,
        Refreshable,
        Start,
        Refresh,
        Refreshing
    }

    class PreviewListView : ListView
    {
        ViewGroup refresher;
        int refresherHeight = -1;
        PreviewListAdapter adapter;

        float yTouch;
        PreviewState state = PreviewState.Done;

        public PreviewListView(Context context) : base(context) { }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            if (refresher != null)
            {
                MeasureChild(refresher, widthMeasureSpec, heightMeasureSpec);

                if (refresherHeight < 0)
                {
                    refresherHeight = refresher.MeasuredHeight;
                    refresher.SetPadding(0, -refresherHeight, 0, 0);
                }
            }
        }

        public override IListAdapter Adapter
        {
            get { return base.Adapter; }
            set
            {
                this.adapter = (PreviewListAdapter)value;
                LayoutInflater inflater = LayoutInflater.From(Context);
                refresher = (ViewGroup)inflater.Inflate(Resource.Layout.preview_header, this, false);
                AddHeaderView(refresher);

                base.Adapter = value;
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            float y = e.GetY();
            int offset;
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    yTouch = y;
                    if (state == PreviewState.Refreshing)
                    {
                        break;
                    }
                    if (adapter.FirstItemIndex == 0)
                    {
                        state = PreviewState.Refreshable;
                    }
                    break;

                case MotionEventActions.Move:
                    if ((state == PreviewState.Refreshing) ||
                        (state != PreviewState.Refreshable && state != PreviewState.Refresh && state != PreviewState.Start))
                    {
                        break;
                    }
                    offset = (int)((y - yTouch) / 3);
                    if (offset >= 0)
                    {
                        if (offset > refresherHeight)
                        {
                            setMessage(Resources.GetString(Resource.String.torefresh));
                            state = PreviewState.Refresh;
                        }
                        else
                        {
                            setMessage(Resources.GetString(Resource.String.refresh));
                            state = PreviewState.Start;
                        }
                        refresher.SetPadding(0, -refresherHeight + offset, 0, 0);
                    }
                    break;

                case MotionEventActions.Up:
                    if (state == PreviewState.Refreshable || state == PreviewState.Done)
                    {
                        state = PreviewState.Done;
                        break;
                    }
                    offset = (int)((y - yTouch) / 3);
                    if (state == PreviewState.Start)
                    {
                        animateToY(-offset, -refresherHeight, PreviewState.Done);
                        break;
                    }
                    if (state == PreviewState.Refresh)
                    {
                        setMessage(Resources.GetString(Resource.String.refreshing));
                        animateToY(refresherHeight - offset, 0, PreviewState.Refreshing);

                        // TODO:
                        new Thread(delegate()
                        {
                            byte[] result = NetUtility.GetData("https://yande.re/post/index.json?page=1&limit=20");
                            adapter.ReadResult(result);
                            result = null;
                            this.Post(delegate
                            {
                                this.Invalidate();
                                refreshOver();
                            });
                        }).Start();
                    }
                    break;
            }
            return base.OnTouchEvent(e);
        }

        void setMessage(string message)
        {
            refresher.FindViewById<TextView>(Resource.Id.textView_refresher).Text = message;
        }

        void refreshOver()
        {
            setMessage(Resources.GetString(Resource.String.refresh));
            animateToY(-refresherHeight, -refresherHeight, PreviewState.Done);
            SetSelection(0);
            state = PreviewState.Done;
        }

        void animateToY(int offset, int y, PreviewState state)
        {
            int shortAnimTime = Resources.GetInteger(Android.Resource.Integer.ConfigShortAnimTime);

            this.Animate().SetDuration(shortAnimTime).Y(offset).SetListener(new PreviewAnimationListener(this, y, state));
            //TranslateAnimation trans = new TranslateAnimation(GetX(), GetX(), GetY(), y);
            //trans.Duration = shortAnimTime;
            //trans.AnimationEnd += delegate
            //{
            //    this.SetY(0);
            //    this.refresher.SetPadding(0, y, 0, 0);
            //    this.state = state;
            //};
        }

        class PreviewAnimationListener : AnimatorListenerAdapter
        {
            PreviewListView listview;
            int y;
            PreviewState state;

            public PreviewAnimationListener(PreviewListView listview, int y, PreviewState state)
                : base()
            {
                this.listview = listview;
                this.y = y;
                this.state = state;
            }

            public override void OnAnimationEnd(Animator animation)
            {
                listview.SetY(0);
                listview.refresher.SetPadding(0, y, 0, 0);
                listview.state = state;
            }
        }
    }
}