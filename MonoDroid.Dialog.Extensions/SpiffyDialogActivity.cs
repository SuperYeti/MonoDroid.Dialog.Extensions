using System;
using MonoDroid.Dialog;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;

namespace MonoDroid.Dialog.Extensions
{
	public class SpiffyDialogActivity : DialogActivity 
	{
        Bitmap bgImage;

        public Bitmap BgImage
		{
			get
			{
				return bgImage;
			}
			set
			{
				bgImage = value;
				LoadView();
			}
		}

        public SpiffyDialogActivity(RootElement root, Bitmap bgImage) 
	    	: base (root)
	    {
			this.bgImage = bgImage;
	    }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LoadView();

        }

        public void LoadView()
        {
            if (bgImage != null)
                this.SetWallpaper(bgImage);

        }
		
		public event EventHandler ViewAppearing;

        protected override void OnResume()
        {
            base.OnResume();

            if (ViewAppearing != null)
                ViewAppearing(this, EventArgs.Empty);

        }
		
	}
	
}
