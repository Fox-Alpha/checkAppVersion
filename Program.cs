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


//--------------------- TODO ---------------------
//
//  Ausgabe einer Hilfe, wenn falsche oder fehlende 
//  Parameter angegeben wurden
//
//  Prüfen ob mindesten Parameter 'process' vorhanden ist
//  Wenn nicht, Hilfe und Benutzungshinweise ausgeben
//
//  Prüfen ob Parameter 'version' angegeben wurde.
//  - Wenn keine Version, dann Ausgabe der Version des Prozesses. 
//      Rückgabe für Nagios: OK
//
//  Prüfen ob der 'compare' Paremeter richtig verwender wurde
//  - Wenn nicht nummerische Zeichen in Version enthalten sind, 
//      dann kann nur TEXT als Vergleich genutzt werden.
//  - Wenn 'compare' verwendet wird. Muss !!! auch eine Version angegeben sein.
//
//--------------------- TODO ---------------------
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
		
		static Dictionary<string, string> dicApplications;
		static Dictionary<string, string> dicCmdArgs;
		static int status = (int) nagiosStatus.Ok;
		
        /// <summary>
        /// Optionen für den Vergleich der angegebenen Version und der aus
        /// dem Prozess ermittelten Versions Angabe
        /// </summary>
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
		
        /// <summary>
        /// Main Funktion der Anwendung
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
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

			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
			
			return status;
		}
    	
        /// <summary>
        /// Prüfen und ermitteln welche Parameter der Anwendung übergeben wurden
        /// Parameter werden in einem Dictionary gespeichert
        /// </summary>
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

                if (!string.IsNullOrWhiteSpace(cmdNeedVer))
                {
                    dicCmdArgs.Add("version", cmdNeedVer);
                }
                else
                    dicCmdArgs.Add("version", "1.0");

                if (!string.IsNullOrWhiteSpace(cmdProcess))
                {
                    dicCmdArgs.Add("process", cmdProcess);
                }
                else
                    dicCmdArgs.Add("process", "JM4");

                if (!string.IsNullOrWhiteSpace(cmdCompareType))
                {
                    dicCmdArgs.Add("compare", cmdCompareType);
                }
                else
                    dicCmdArgs.Add("compare", string.IsNullOrWhiteSpace(cmdNeedVer) ? string.Empty : "TEXT");
            }
            else
                //  Ausgabe der Hinweise zum Aufruf
                //  Und Nutzung der Parameter
                printUsage();
	        //	####
    	}
    	
    	/// <summary>
        /// Prüfen auf gültige compare Parameter
        /// Vergleich mit enum
        /// </summary>
        /// <param name="value"></param>
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
    	
        /// <summary>
        /// Prüfen ob der angegebene Prozess aktiv ist
        /// Es wird der erste gefundene Prozess genutzt
        /// Ermitteln der Version
        /// Vergleichen der Version
        /// </summary>
        /// <param name="strProcess">Name des zu prüfenden Prozess</param>
        /// <returns></returns>
    	static bool check_ProcessIsRunning(string strProcess)
    	{
    		//prz = new System.Diagnostics.Process();
    		Process [] appProzess = Process.GetProcessesByName(strProcess);
    		FileVersionInfo fvi;
    		
    		string strVersion = "";
    		string strVerNeed = "";
            bool equal = false;
    		
    		if (appProzess.Length > 0)
            {
    			Debug.WriteLine(appProzess[0].MainModule.FileName, "check_ProcessIsRunning() -> FileName");
    			Console.WriteLine("{0}", appProzess[0].MainModule.FileName);
    			Debug.WriteLine(appProzess[0].MainModule.FileVersionInfo.ToString(), "check_ProcessIsRunning() -> FileVersionInfo");
    			Console.WriteLine("{0}", appProzess[0].MainModule.FileVersionInfo); //, "check_ProcessIsRunning() -> FileVersionInfo");
    			
    			fvi = appProzess[0].MainModule.FileVersionInfo;
    			
    			strVersion = string.Format("{0}.{1}.{2}.{3}", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);
    			dicCmdArgs.TryGetValue("version", out strVerNeed);

                if ((compareType & cmdActionArgsCompareType.TEXT) != 0)
                {
                    equal = check_VersionNumbers(strVersion, strVerNeed);
                }
                else
                    equal = check_VersionNumbers(new int[] { fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart }, strVersion2IntArray(strVerNeed));

                if (equal)
    				Debug.WriteLine(string.Format("Version ist OK (Erf. {0}/ App {1})", strVerNeed, strVersion));
    			else
    				Debug.WriteLine(string.Format("Version ist NOK (Erf. {0}/ App {1})", strVerNeed, strVersion));
    			
    			return true;
    		}

    		return false;
    	}
    	
        /// <summary>
        /// Vergleich der Versionsnummer als Text vergleich
        /// </summary>
        /// <param name="strVerNum"></param>
        /// <param name="strVerNeed"></param>
        /// <returns>True, wenn Identisch. Sonst False</returns>
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
    	
        /// <summary>
        /// Vergleich der Versionsteile als nummericher Wert
        /// </summary>
        /// <param name="iVerNum">Array für ermittelte Version</param>
        /// <param name="iVerNeed">Array der zu prüfenden Version</param>
        /// <returns>True, wenn Identisch. Sonst False</returns>
    	static bool check_VersionNumbers(int[] iVerNum, int[] iVerNeed)
    	{
    		//  CompareType mit einbeziehen
            //  Nur Vergleichen wenn beide Versionen gleiche Anzahl Teile haben
    		if (iVerNum.Length == iVerNeed.Length)
            {
                //  Alle Teile vergleichen
                if ((compareType & cmdActionArgsCompareType.ALL) != 0)
                {
                    for (int i = 0; i < iVerNeed.Length; i++)
                    {
                        if (iVerNeed[i] != iVerNum[i])
                        {
                            //  Wenn ein Teil != dann Abruch
                            return false;
                        }
                    }
                    return true;
                }
                //  4-Teilig. Alle Teile einzelnd vergleichen
                //  Abruch wenn ein Teil != ist
                else if (iVerNum.Length == 4 && iVerNeed.Length == 4)
                {
                    if ((compareType & cmdActionArgsCompareType.Major) != 0)
                    {
                        if (iVerNeed[0] != iVerNum[0])
                        {
                            return false;
                        }
                    }
                    if ((compareType & cmdActionArgsCompareType.Minor) != 0)
                    {
                        if (iVerNeed[1] != iVerNum[1])
                        {
                            return false;
                        }
                    }
                    if ((compareType & cmdActionArgsCompareType.Build) != 0)
                    {
                        if (iVerNeed[2] != iVerNum[2])
                        {
                            return false;
                        }
                    }
                    if ((compareType & cmdActionArgsCompareType.Private) != 0)
                    {
                        if (iVerNeed[3] != iVerNum[3])
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
    		
    		return false;
    	}
    	
        /// <summary>
        /// Wandelt die übergebene Versionsnummer in ein nummerisches Array
        /// </summary>
        /// <param name="Version">String mit Versionsnummer </param>
        /// <returns>Nummerisches Array</returns>
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
        static public string ParseCmdLineParam(string key, string cmdline)
		{
			string res = "";
			try
			{
				int end = 0;
				int start = 0;
				
				//	Ersetzem von Anführungszeichen in der Parameterliste
				cmdline = Regex.Replace(cmdline, "\"", "");

                //  Start auf ersten Parameter beginnend mit ' -' setzen
                if (cmdline.IndexOf(" -", start) > 0)
                {
                    start = cmdline.IndexOf(" -", start);
                    cmdline = cmdline.Substring(start, cmdline.Length - start);
                }
                else
                    return string.Empty;

                //Wenn Key nicht gefunden wurde, dann beenden.
                if ((start = cmdline.ToLower().IndexOf(key)) <= 0)
					return string.Empty;
				
				if (cmdline.Length == start+key.Length)
					return cmdline.Substring(start, cmdline.Length-start);
				
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

        /// <summary>
        /// Ausgabe der Nutzungshinweise
        /// </summary>
        static void printUsage()
        {
            Console.WriteLine("Falsche/r oder fehlende/r Parameter angabe");
        }
	}
}