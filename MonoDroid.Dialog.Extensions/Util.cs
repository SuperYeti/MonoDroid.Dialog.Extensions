/*
Copyright (c) 2010 Novell Inc.

Author: Miguel de Icaza
Updates: Warren Moxley

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
using System;
using System.IO;
using System.Text;
using System.Threading;
using Android.Views;
using Android.Runtime;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Reflection;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using Android;
using System.Diagnostics;
using MonoDroid.Dialog;
using Android.Locations;
using System.Globalization;
using Android.Graphics.Drawables;
using Android.App;
using Android.Content;
using Android.Preferences;
using Android.Telephony;

namespace MonoDroid.Dialog.Extensions
{
	public static class Util
	{
		/// <summary>
		///   A shortcut to the main application
		/// </summary>
		public readonly static string BaseDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "..");

		const long TicksOneDay = 864000000000;
		const long TicksOneHour = 36000000000;
		const long TicksMinute = 600000000;
		
		public static string StripHtml (string str)
		{
			if (str.IndexOf ('<') == -1)
				return str;
			var sb = new StringBuilder ();
			for (int i = 0; i < str.Length; i++){
				char c = str [i];
				if (c != '<'){
					sb.Append (c);
					continue;
				}
				
				for (i++; i < str.Length; i++){
					c =  str [i];
					if (c == '"' || c == '\''){
						var last = c;
						for (i++; i < str.Length; i++){
							c = str [i];
							if (c == last)
								break;
							if (c == '\\')
								i++;
						}
					} else if (c == '>')
						break;
				}
			}
			return sb.ToString ();
		}
	
		public static string ObscureString (string value)
		{
			StringBuilder sb = new StringBuilder ();
			
			if (value.Length >= 4) {
				sb.Append (char.Parse ("*"), value.Length - 4);
				sb.Append (value.Substring (value.Length - 4));
			} else {
				sb.Append (value);
			}
			
			return sb.ToString ();
			
		}
		
		//static UIActionSheet sheet;
		//public static UIActionSheet GetSheet (string title)
		//{
		//	sheet = new UIActionSheet (title);
		//	return sheet;
		//}
		
		public static bool IsEmail(string inputEmail)
		{
			if(string.IsNullOrEmpty(inputEmail))
				return false;
			
			string strRegex = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
		         @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" + 
		         @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
		   
			Regex re = new Regex(strRegex);
		   
			if (re.IsMatch(inputEmail))
		    	return (true);
			else
		    	return (false);
			
		}
		
	}
	
	public static class Device
	{
		public static string IPAddress ()
		{
			IPHostEntry host;
			string localIP = "?";
			host = Dns.GetHostEntry (Dns.GetHostName ());
			
			foreach (IPAddress ip in host.AddressList) {
				if (ip.AddressFamily == AddressFamily.InterNetwork) {
					localIP = ip.ToString ();
				}
			}
			
			return localIP;
			
		}
		
		public static  string Documents = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
		
	}

	public static class Encryption
	{
		public static string EncryptString (string toEncrypt, string bkey)
		{
			byte[] keyArray;
			byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes (toEncrypt);
			
			MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider ();
			keyArray = hashmd5.ComputeHash (UTF8Encoding.UTF8.GetBytes (bkey));
			
			TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider ();
			tdes.Key = keyArray;
			tdes.Mode = CipherMode.ECB;
			tdes.Padding = PaddingMode.PKCS7;
			
			ICryptoTransform cTransform = tdes.CreateEncryptor ();
			byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
			
			return Convert.ToBase64String (resultArray, 0, resultArray.Length);
			
		}

		public static string EncryptString (int value, string bkey)
		{
			return EncryptString (value.ToString (), bkey);
			
		}

		public static string DecryptString (string toDecrypt, string bkey)
		{
			byte[] keyArray;
			byte[] toEncryptArray = Convert.FromBase64String (toDecrypt);
			
			MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider ();
			keyArray = hashmd5.ComputeHash (UTF8Encoding.UTF8.GetBytes (bkey));
			
			TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider ();
			tdes.Key = keyArray;
			tdes.Mode = CipherMode.ECB;
			tdes.Padding = PaddingMode.PKCS7;
			
			ICryptoTransform cTransform = tdes.CreateDecryptor ();
			byte[] resultArray = cTransform.TransformFinalBlock (toEncryptArray, 0, toEncryptArray.Length);
			
			return UTF8Encoding.UTF8.GetString (resultArray);
		}

	
	}

	public class Password
	{
		public static string GetNewPassword (int passwordLength)
		{
			return GetNewPassword (passwordLength, "");
			
		}

		public static string GetNewPassword (int passwordLength, string exclusions)
		{
			Password pw = new Password (passwordLength);
			pw.Exclusions = exclusions;
			
			return pw.Generate ();
			
		}
		
		public static string GetNewPassword (int passwordLength, bool excludeSpecialCharacters)
		{
			Password pw = new Password (passwordLength);
			pw.ExcludeSymbols = true;
			
			return pw.Generate ();
			
		}

		public static bool ValidatePassword (string password)
		{
			Regex reg = new Regex ("^(?=.*\\d)(?=.*[a-z])(?=.*[A-Z]).{10,16}$");
			
			return reg.IsMatch (password);
			
		}

		public Password (int passwordLength)
		{
			pwdCharArray = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%^&()-_=+[]{}\\,<.>/?".ToCharArray ();
			
			Minimum = passwordLength;
			
			if (int.MaxValue - 2 >= passwordLength)
				Maximum = passwordLength + 2;
			
			ConsecutiveCharacters = false;
			RepeatCharacters = true;
			ExcludeSymbols = false;
			Exclusions = null;
			
			rng = new RNGCryptoServiceProvider ();
		}

		protected int GetCryptographicRandomNumber (int lowerBound, int upperBound)
		{
			int urndnum;
			byte[] rndnum = new byte[] { 0, 0, 0, 0 };
			
			if (upperBound >= 1) {
				if (lowerBound == upperBound - 1) {
					return lowerBound;
					
				}
				
			}
			
			int xcludeRndBase = int.MaxValue - (int.MaxValue % System.Convert.ToInt32 (upperBound - lowerBound));
			
			do {
				rng.GetBytes (rndnum);
				urndnum = System.BitConverter.ToInt32 (rndnum, 0);
			} while (urndnum >= xcludeRndBase);
			
			return System.Convert.ToInt32 ((urndnum % (upperBound - lowerBound)) + lowerBound);
			
		}

		protected char GetRandomCharacter ()
		{
			int upperBound = pwdCharArray.GetUpperBound (0);
			
			if (this.ExcludeSymbols) {
				upperBound = Password.UBoundDigit;
			}
			
			int randomCharPosition = GetCryptographicRandomNumber (pwdCharArray.GetLowerBound (0), upperBound);
			
			if (randomCharPosition < 0) {
				randomCharPosition = randomCharPosition * -1;
			}
			
			char randomChar = pwdCharArray[randomCharPosition];
			
			return randomChar;
			
		}

		protected string Generate ()
		{
			
			//// Pick random length between minimum and maximum
			int pwdLength = GetCryptographicRandomNumber (this.Minimum, this.Maximum);
			
			StringBuilder pwdBuffer = new StringBuilder ();
			pwdBuffer.Capacity = this.Maximum;
			
			//// Generate random characters
			char lastCharacter;
			char nextCharacter;
			
			//// Initial dummy character flag
			lastCharacter = (char)10;
			nextCharacter = (char)10;
			
			int i = 0;
			
			for (i = 0; i <= pwdLength; i++) {
				
				nextCharacter = GetRandomCharacter ();
				
				if (!this.ConsecutiveCharacters) {
					while (lastCharacter == nextCharacter) {
						nextCharacter = GetRandomCharacter ();
					}
				}
				
				if (!this.RepeatCharacters) {
					
					string temp = pwdBuffer.ToString ();
					int duplicateIndex = temp.IndexOf (nextCharacter);
					while (-1 != duplicateIndex) {
						nextCharacter = GetRandomCharacter ();
						duplicateIndex = temp.IndexOf (nextCharacter);
					}
				}
				
				if (this.Exclusions != null) {
					while (-1 != this.Exclusions.IndexOf (nextCharacter)) {
						nextCharacter = GetRandomCharacter ();
					}
				}
				pwdBuffer.Append (nextCharacter);
				lastCharacter = nextCharacter;
			}
			
			if (null != pwdBuffer) {
				return pwdBuffer.ToString ();
				
			} else {
				
				return string.Empty;
			}
		}

		public string Exclusions {
			get { return this.exclusionSet; }
			set { this.exclusionSet = value; }
		}

		public int Minimum {
			get { return this.minSize; }
			set {
				this.minSize = value;
				if (Password.DefaultMinimum > this.minSize) {
					this.minSize = Password.DefaultMinimum;
				}
			}
		}

		public int Maximum {
			get { return this.maxSize; }
			set {
				this.maxSize = value;
				if (this.minSize >= this.maxSize) {
					this.maxSize = Password.DefaultMaximum;
				}
			}
		}

		public bool ExcludeSymbols {
			get { return this.hasSymbols; }
			set { this.hasSymbols = value; }
		}

		public bool RepeatCharacters {
			get { return this.hasRepeating; }
			set { this.hasRepeating = value; }
		}

		public bool ConsecutiveCharacters {
			get { return this.hasConsecutive; }
			set { this.hasConsecutive = value; }
		}

		private const int DefaultMinimum = 8;
		private const int DefaultMaximum = 10;
		private const int UBoundDigit = 61;

		private RNGCryptoServiceProvider rng;
		private int minSize;
		private int maxSize;
		private bool hasRepeating;
		private bool hasConsecutive;
		private bool hasSymbols;
		private string exclusionSet;
		private char[] pwdCharArray;
		
	}
	
	public static class Util1
	{
        public static string StripHtml (string str)
		{
			if (str.IndexOf ('<') == -1)
				return str;
			var sb = new StringBuilder ();
			for (int i = 0; i < str.Length; i++){
				char c = str [i];
				if (c != '<'){
					sb.Append (c);
					continue;
				}
				
				for (i++; i < str.Length; i++){
					c =  str [i];
					if (c == '"' || c == '\''){
						var last = c;
						for (i++; i < str.Length; i++){
							c = str [i];
							if (c == last)
								break;
							if (c == '\\')
								i++;
						}
					} else if (c == '>')
						break;
				}
			}
			return sb.ToString ();
		}
		
		public static string CleanName (string name)
		{
			if (name.Length == 0)
				return "";
			
			bool clean = true;
			foreach (char c in name){
				if (Char.IsLetterOrDigit (c) || c == '_')
					continue;
				clean = false;
				break;
			}
			if (clean)
				return name;
			
			var sb = new StringBuilder ();
			foreach (char c in name){
				if (!Char.IsLetterOrDigit (c))
					break;
				
				sb.Append (c);
			}
			return sb.ToString ();
		}
		
		static long lastTime;
		[Conditional ("TRACE")]
		public static void ReportTime (string s)
		{
			long now = DateTime.UtcNow.Ticks;
			
			Console.WriteLine ("[{0}] ticks since last invoke: {1}", s, now-lastTime);
			lastTime = now;
            
		}
		
		[Conditional ("TRACE")]
		public static void Log (string format, params object [] args)
		{
			Console.WriteLine (String.Format (format, args));
		}
		
		public static void LogException (string text, Exception e)
		{
			using (var s = System.IO.File.AppendText (Util.BaseDir + "/Documents/crash.log")){
				var msg = String.Format ("On {0}, message: {1}\nException:\n{2}", DateTime.Now, text, e.ToString());
				s.WriteLine (msg);
				Console.WriteLine (msg);
			}
		}
			
		
		static CultureInfo americanCulture;
		public static CultureInfo AmericanCulture {
			get {
				if (americanCulture == null)
					americanCulture = new CultureInfo ("en-US");
				return americanCulture;
			}
		}
		#region Location
		/*
		internal class MyCLLocationManagerDelegate : CLLocationManagerDelegate {
			Action<CLLocation> callback;
			
			public MyCLLocationManagerDelegate (Action<CLLocation> callback)
			{
				this.callback = callback;
			}
			
			public override void UpdatedLocation (CLLocationManager manager, CLLocation newLocation, CLLocation oldLocation)
			{
				manager.StopUpdatingLocation ();
				locationManager = null;
				callback (newLocation);
			}
			
			public override void Failed (CLLocationManager manager, NSError error)
			{
				callback (null);
			}
			
		}

		static CLLocationManager locationManager;
		static public void RequestLocation (Action<CLLocation> callback)
		{
			locationManager = new CLLocationManager () {
				DesiredAccuracy = CLLocation.AccuracyBest,
				Delegate = new MyCLLocationManagerDelegate (callback),
				DistanceFilter = 1000f
			};
			if (CLLocationManager.LocationServicesEnabled)
				locationManager.StartUpdatingLocation ();
		}	
        */
		#endregion
	}
	
	public static class StringUtil
	{
		public static string SeperateCamelCase(string value)
		{
			return Regex.Replace(value, "((?<=[a-z])[A-Z]|[A-Z](?=[a-z]))", " $1").Trim();
		}
	}

    public class Alerts
    {
        Action _action;

        public void SuccessfulMessage(Context context)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(context);

            builder.SetMessage("Saved Succcessfully")
                .SetCancelable(false)
                .SetPositiveButton("Ok", OnClick)
                .SetTitle("Success");
            
            AlertDialog alert = builder.Create();

            alert.Show();

        }

        public void SuccessfulMessage(Context context, string message)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(context);

            builder.SetMessage(message)
                .SetCancelable(false)
                .SetPositiveButton("Ok", OnClick)
                .SetTitle("Success");

            AlertDialog alert = builder.Create();

            alert.Show();

        }

        public void UnsuccessfulMessage(Context context)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(context);

            builder.SetMessage("Save UnSuccessful, Please try again")
                .SetCancelable(false)
                .SetPositiveButton("Ok", OnClick)
                .SetTitle("Error");

            AlertDialog alert = builder.Create();

            alert.Show();

        }

        public void UnsuccessfulMessage(Context context, string message)
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(context);

            builder.SetMessage(message)
                .SetCancelable(false)
                .SetPositiveButton("Ok", OnClick)
                .SetTitle("Error");

            AlertDialog alert = builder.Create();

            alert.Show();
            
        }

        public void MessageAndRedirect(Context context, string message, Action action)
        {
            _action = action;

            AlertDialog.Builder builder = new AlertDialog.Builder(context);

            builder.SetMessage(message)
                .SetCancelable(false)
                .SetPositiveButton("Ok", OnClick)
                .SetTitle("Info");

            AlertDialog alert = builder.Create();

            alert.Show();
            
        }

        private void OnClick(object sender, Android.Content.DialogClickEventArgs args)
        {
            switch (args.Which)
            {
                case DialogInterface.ButtonNegative:
                    break;

                case DialogInterface.ButtonPositive:
                    if (_action != null)
                        _action();
                    break;

                case DialogInterface.ButtonNeutral:
                    break;

            }
           
        }

    }

}

