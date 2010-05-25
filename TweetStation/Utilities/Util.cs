
using System;
using System.IO;
using System.Text;
using System.Threading;
using MonoTouch.UIKit;
using MonoTouch.Foundation;
using MonoTouch.Dialog;

namespace TweetStation
{
	public static class Util
	{
		/// <summary>
		///   A shortcut to the main application
		/// </summary>
		public static UIApplication MainApp = UIApplication.SharedApplication;
		public static AppDelegate MainAppDelegate = UIApplication.SharedApplication.Delegate as AppDelegate;
		
		public readonly static string BaseDir = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "..");

		//
		// Since we are a multithreaded application and we could have many
		// different outgoing network connections (api.twitter, images,
		// searches) we need a centralized API to keep the network visibility
		// indicator state
		//
		static object networkLock = new object ();
		static int active;
		
		public static void PushNetworkActive ()
		{
			lock (networkLock){
				active++;
				MainApp.NetworkActivityIndicatorVisible = true;
			}
		}
		
		public static void PopNetworkActive ()
		{
			lock (networkLock){
				active--;
				if (active == 0)
					MainApp.NetworkActivityIndicatorVisible = false;
			}
		}
		
		public static DateTime LastUpdate (string key)
		{
			var s = Defaults.StringForKey (key);
			if (s == null)
				return DateTime.MinValue;
			long ticks;
			if (Int64.TryParse (s, out ticks))
				return new DateTime (ticks, DateTimeKind.Utc);
			else
				return DateTime.MinValue;
		}
		
		public static bool NeedsUpdate (string key, TimeSpan timeout)
		{
			return DateTime.UtcNow - LastUpdate (key) > timeout;
		}
		
		public static void RecordUpdate (string key)
		{
			Defaults.SetString (key, DateTime.UtcNow.Ticks.ToString ());
		}
			
		
		public static NSUserDefaults Defaults = NSUserDefaults.StandardUserDefaults;
				
		const long TicksOneDay = 864000000000;
		const long TicksOneHour = 36000000000;
		const long TicksMinute = 600000000;
		
		static string s1 = Locale.GetText ("1 sec");
		static string sn = Locale.GetText (" secs");
		static string m1 = Locale.GetText ("1 min");
		static string mn = Locale.GetText (" mins");
		static string h1 = Locale.GetText ("1 hour");
		static string hn = Locale.GetText (" hours");
		static string d1 = Locale.GetText ("1 day");
		static string dn = Locale.GetText (" days");
		
		public static string FormatTime (TimeSpan ts)
		{
			int v;
			
			if (ts.Ticks < TicksMinute){
				v = ts.Seconds;
				if (v == 1)
					return s1;
				else
					return v + sn;
			} else if (ts.Ticks < TicksOneHour){
				v = ts.Minutes;
				if (v == 1)
					return m1;
				else
					return v + mn;
			} else if (ts.Ticks < TicksOneDay){
				v = ts.Hours;
				if (v == 1)
					return h1;
				else
					return v + hn;
			} else {
				v = ts.Days;
				if (v == 1)
					return d1;
				else
					return v + dn;
			}
		}
		
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
		
		public static RootElement MakeProgressRoot (string caption)
		{
			return new RootElement (caption){
				new Section (){
					new ActivityElement ()
				}
			};
		}
		
		public static RootElement MakeError (string diagMsg)
		{
			return new RootElement ("Error"){
				new Section ("Error"){
					new MultilineElement ("Unable to retrieve the information for " + diagMsg)
				}
			};
		}
		
		static long lastTime;
		public static void ReportTime (string s)
		{
			long now = DateTime.UtcNow.Ticks;
			
			Console.WriteLine ("[{0}] ticks since last invoke: {1}", s, now-lastTime);
			lastTime = now;
		}
	}
}
