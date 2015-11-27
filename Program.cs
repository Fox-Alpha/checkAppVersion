/*
 * Erstellt mit SharpDevelop.
 * Benutzer: buck
 * Datum: 26.11.2015
 * Zeit: 14:09
 * 
 */
using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;

namespace checkAppVersion
{
	class Program
	{
		//	Rückgabewerte für Nagios
		enum nagiosStatus
		{
			Ok=0,
			Warning=1,
			Critical=2,
			Unknown=3
		}
		
//		static string[] cmdArgs;
		static Dictionary<string, string> dicApplications;
		static Dictionary<string, string> dicCmdArgs;
		
//		static bool bProcess;
//		static bool b;
		
//		Process prz;
		
		static int status =(int) nagiosStatus.Ok;
		
//		static string[] ver;
		
		[Flags()]
		public enum cmdActionArgsCompareType : int
		{
			NONE = 0,
			TEXT = 1,
			Major = 2,
			Minor = 4,
			Build = 8,
			Private = 16,
			ALL = Major | Minor | Build | Private,
			
		}
		
		static cmdActionArgsCompareType _compareType;
		
		static public cmdActionArgsCompareType compareType {
			get { return _compareType; }
			set { _compareType = value; }
		}
		

    	public static int Main(string[] args)
		{
			string tmp = "";

//			Console.WriteLine("Nagios Client - NSClient++ App");
			Console.Title = "Nagios Client - NSClient++ App";
			
			compareType = cmdActionArgsCompareType.NONE;
			
			check_cmdLineArgs();
			
			if (dicCmdArgs.TryGetValue("compare", out tmp)) {
				if (!string.IsNullOrWhiteSpace(tmp)) {
					foreach (string str in tmp.Split(',')) {
	    				check_compareParameters(str);
	    			}
				}
			}
			
			if (dicCmdArgs.TryGetValue("process", out tmp)) {
				if (!string.IsNullOrWhiteSpace(tmp)) {
					check_ProcessIsRunning(tmp);
				}
			}
			

//			if (args.Length > 0) {
//				foreach(string s in args)
//					Debug.WriteLine(s, "CommandLIne");
//				
//				cmdArgs = Environment.GetCommandLineArgs();
//
//				if (Environment.GetCommandLineArgs().Length > 0) {
//					strProcName = ParseCmdLineParam("process", Environment.CommandLine);
//					
//					strVerNeed = ParseCmdLineParam("version", Environment.CommandLine);
//					strCompareType = ParseCmdLineParam("compare", Environment.CommandLine);
//					
//					
//					if (!string.IsNullOrEmpty(strProcName) && !string.IsNullOrEmpty(strVerNeed)) {
//						ver = strVerNeed.Split('.');
//						if(check_ProcessIsRunning(strProcName))
//							Debug.WriteLine("Process gefunden");
//						else
//							Debug.WriteLine("Process {0} nicht gefunden", strProcName);
//					}
//				}
//			}
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
			
			return status;
		}
    	
    	static void check_cmdLineArgs()
    	{
	        //	Kommandozeilenparameter Auflistung
	        //	Zwischenspeichern als Dictionary zum leichteren Zugriff
	        dicCmdArgs = new Dictionary<string, string>();
	        
	        dicApplications = new Dictionary<string, string>() 
	        {
	        	{"JM4","JobManager 4"}, 
	        	{"AM5","ApplicationManager"}, 
	        	{"AMMT","AMMT"}
	        };
	        
			string cmdNeedVer;
			string cmdProcess;
			string cmdCompareType;

			if (Environment.GetCommandLineArgs().Length > 0) 
			{
	        	cmdNeedVer = ParseCmdLineParam("version", Environment.CommandLine);
	        	cmdProcess = ParseCmdLineParam("process", Environment.CommandLine);
	        	cmdCompareType = ParseCmdLineParam("compare", Environment.CommandLine);
	        	
	        	if (!string.IsNullOrWhiteSpace(cmdNeedVer)) {
	        		dicCmdArgs.Add("version", cmdNeedVer);
	        	}
	        	else
	        		dicCmdArgs.Add("version", "1.0");
	        	
	        	if (!string.IsNullOrWhiteSpace(cmdProcess)) {
	        		dicCmdArgs.Add("process", cmdProcess);
	        	}
	        	else
	        		dicCmdArgs.Add("process", "JM4");

	        	if (!string.IsNullOrWhiteSpace(cmdCompareType)) {
	        		dicCmdArgs.Add("compare", cmdCompareType);
	        	}
	        	else
	        		dicCmdArgs.Add("compare", string.IsNullOrWhiteSpace(cmdNeedVer) ? string.Empty : "TEXT");
	        }
	        //	####
    	}
    	
    	
    	static void check_compareParameters(string value)
    	{
        //	Übergebenen Action Parameter prüfen ob dieser im enum enthalten ist
	        cmdActionArgsCompareType result;	
	        
        	foreach (string enumarg in Enum.GetNames(typeof(cmdActionArgsCompareType)))
	        {
        		if (!string.IsNullOrWhiteSpace(value) && value.ToLower() == enumarg.ToLower()) {
        			Debug.WriteLine("{0} = {1}",enumarg, value);
        			Enum.TryParse<cmdActionArgsCompareType>(value, out result);
        			if (compareType == cmdActionArgsCompareType.NONE) {
        				compareType = result;
        			}
        			else
        				compareType = compareType | result;
        		} 
	        }
        	Debug.WriteLine("Action = {0}", compareType);
        //	####
    	}
    	
    	static bool check_ProcessIsRunning(string strProcess)
    	{
    		//prz = new System.Diagnostics.Process();
    		Process [] appProzess = Process.GetProcessesByName(strProcess);
    		FileVersionInfo fvi;
    		
    		string strVersion = "";
    		string strVerNeed = "";
//    		int[] iVerInf;
    		
    		if (appProzess.Length > 0) {
//    			iVerInf = new int[4];
    			Debug.WriteLine(appProzess[0].MainModule.FileName, "check_ProcessIsRunning() -> FileName");
    			Console.WriteLine("{0}", appProzess[0].MainModule.FileName);
    			Debug.WriteLine(appProzess[0].MainModule.FileVersionInfo.ToString(), "check_ProcessIsRunning() -> FileVersionInfo");
    			Console.WriteLine("{0}", appProzess[0].MainModule.FileVersionInfo); //, "check_ProcessIsRunning() -> FileVersionInfo");
    			
    			fvi = appProzess[0].MainModule.FileVersionInfo;
    			
    			strVersion = string.Format("{0}.{1}.{2}.{3}", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);
    			dicCmdArgs.TryGetValue("version", out strVerNeed);
    			
//    			iVerInf.SetValue(fvi.FileMajorPart, 0);
//    			iVerInf.SetValue(fvi.FileMinorPart, 1);
//    			iVerInf.SetValue(fvi.FileBuildPart, 2);
//    			iVerInf.SetValue(fvi.FilePrivatePart, 3);
    			
//    			if(check_VersionNumbers(iVerInf))
				if(check_VersionNumbers(new int[]{fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart}, strVersion2IntArray(strVerNeed)))
    				Debug.WriteLine(string.Format("Version ist OK (Erf. {0}/ App {1})", strVerNeed, strVersion));
    			else
    				Debug.WriteLine(string.Format("Version ist NOK (Erf. {0}/ App {1})", strVerNeed, strVersion));
//    				= string.Format("{0}.{1}.{2}.{3}", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FilePrivatePart, fvi.FileBuildPart);
    			
    			return true;
    		}

    		return false;
    	}
    	
    	static bool check_VersionNumbers(string strVerNum, string strVerNeed)
    	{
    		if (!string.IsNullOrWhiteSpace(strVerNum) && !string.IsNullOrWhiteSpace(strVerNeed)) {
    			if (strVerNum != strVerNeed) {
    				return false;
    			}
    		}
    		else 
    			return false;
    		
    		return true;
    	}
    	
    	static bool check_VersionNumbers(int[] iVerNum, int[] iVerNeed)
    	{
    		//TODO: CompareType mit einbeziehen
    		if (iVerNum.Length == iVerNeed.Length) {
    			for(int i=0;i<iVerNeed.Length;i++)
    			{
    				
    			}
    		}
    		
    		
    		return false;
    	}
    	
    	static int[] strVersion2IntArray(string Version)
    	{
    		string[] verarr = Version.Split('.');
    		int[] iver = new int[4];
    		int iTmp;
    		
    		if(verarr.Length > 0)
    		{
    			for(int i=0; i<verarr.Length-1;i++)
    			{
    				if(!int.TryParse(verarr[i], out iTmp))
    					iver.SetValue(iTmp, i);
    			}
    		}
    		else
    			return new int[]{1,0,0,0};
    		
    		return iver;
    	}
	
	
		/// <summary>
		/// Auslesen der Parameter aus dem Kommandozeilen Aufruf
		/// </summary>
		/// <param name="key"></param>
		/// <param name="cmdline"></param>
		/// <returns></returns>
		public static string ParseCmdLineParam(string key, string cmdline)
		{
			string res = "";
			try
			{
				int end = 0;
				int start = 0;
				
				//	Ersetzem von Anführungszeichen in der Parameterliste
				cmdline = Regex.Replace(cmdline, "\"", "");
				
				//Wenn Key nicht gefunden wurde, dann beenden.
				if((start = cmdline.ToLower().IndexOf(key)) <= 0)
					return "";
				
				if (cmdline.Length == start+key.Length)
					return cmdline.Substring(start, cmdline.Length-start);;
				
				//prüfen ob dem Parameter ein Wert mit '=' angehängt ist
				if (cmdline[start+key.Length] == '=') {
					//Start hinter das '=' setzten
					start += key.Length+1;
				}
				else				
					start += key.Length;
				
				//Position des nächsten Parameters ermitteln
				if(cmdline.Length > start)
					end = cmdline.IndexOf(" -", start);

				int length = 0;
				
				if (end > 0)
				{
					length = end-start;
				} 
				else 
				{
					length = cmdline.Length-start;
				}
				if(length > 0)
					res = cmdline.Substring(start, length);
				
			} 
			catch (System.Exception ex)
			{
				Debug.WriteLine(ex.Message);
			}
			return res;
		}
	}
}