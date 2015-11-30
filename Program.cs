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
//  - Wenn 'nicht nummerische' Zeichen in Version enthalten sind, 
//      dann kann nur TEXT als Vergleich genutzt werden.
//  - Wenn 'compare' verwendet wird. Muss !!! auch eine Version angegeben sein.
//
//  Equals als Parameter implementieren
//  - EQ, LT, GT, GTE, LTE, NEQ
//--------------------- TODO ---------------------
namespace checkAppVersion
{
	class Program
	{
        #region Eigenschaften
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

        public enum cmdActionArgsEqualType : int
        {
            NONE = 0,
            EQ = 1,
            GT,
            LT,
            NEQ,
            GTE,
            GTL
        }
		
		static cmdActionArgsCompareType _compareType;
		
		static public cmdActionArgsCompareType compareType
        {
			get { return _compareType; }
			set { _compareType = value; }
		}

        static cmdActionArgsEqualType _equalType;

        static public cmdActionArgsEqualType equalType
        {
            get { return _equalType; }
            set { _equalType = value; }
        }

        #endregion


        /// <summary>
        /// Main Funktion der Anwendung
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static int Main(string[] args)
		{
			string prz, ver, cmp = "";

//			Console.WriteLine("Nagios Client - NSClient++ App");
			Console.Title = "Nagios Client - NSClient++ App";
			
			compareType = cmdActionArgsCompareType.NONE;
            equalType = cmdActionArgsEqualType.NONE;

            check_cmdLineArgs();

            //  Es muss mindestens ein Prozessname angegeben sein
            if (dicCmdArgs.TryGetValue("process", out prz))
            {
                //  Wenn eine Version angegeben ist, dann Vergleich durchführem
                if (dicCmdArgs.TryGetValue("version", out ver))
                {
                    //  Für den Vergleich muss die Version mit angegeben sein
                    if (dicCmdArgs.TryGetValue("compare", out cmp))
                    {
                        if (!string.IsNullOrWhiteSpace(cmp))
                        {
                            foreach (string str in cmp.Split(','))
                            {
                                if(!check_compareParameters(str))
                                {
                                    Console.WriteLine("Der angegebene vergleichs Parameter {0} ist ungültig", str);
                                    printUsage();
                                    return (int)nagiosStatus.Unknown;
                                }
                            }
                        }
                    }
                    else
                    {
                        //  Kein compare Parameter angegeben
                    }
                }

                //  Wenn keine Version vorhanden ist, 
                //  dann nur ausgabe der Version vom Prozes
                if (!string.IsNullOrWhiteSpace(prz))
                {
                    string strVersion;
                    //string strVerNeed;
                    bool equal = false; ;
                    int[] iVer;

                    if (!check_ProcessIsRunning(prz, out strVersion))
                    {
                        Console.WriteLine("'Es muss mindestens der Name eines Prozesses angegeben werden oder der angegebene Prozess ist nicht gestartet'");
                        printUsage();
#if DEBUG
                        Console.Write("Press any key to continue . . . ");
                        Console.ReadKey(true);
#endif
                        status = (int)nagiosStatus.Unknown;
                    }
                    else 
                    {
                    	if (!string.IsNullOrWhiteSpace(ver))
	                    {
	                        if ((compareType & cmdActionArgsCompareType.TEXT) != 0)
	                        {
	                            equal = check_VersionNumbers(strVersion, ver);
	                        }
	                        else
	                        {
//	                            iVer = strVersion2IntArray(strVersion);
	                            equal = check_VersionNumbers(strVersion2IntArray(strVersion), strVersion2IntArray(ver));
	                        }
	                        status = equal ? (int)nagiosStatus.Ok : (int)nagiosStatus.Critical;
	
	                        if (equal)
	                            Debug.WriteLine(string.Format("Version ist OK (Erf. {0} ({2}) / App {1})", ver, strVersion, compareType));
	                        else
	                            Debug.WriteLine(string.Format("Version ist NOK (Erf. {0} ({2}) / App {1})", ver, strVersion, compareType));
	
	                        Console.WriteLine(string.Format("Version ist {3}|'(Erforderlich {0} ({2}) / Anwendung {1})'", ver, strVersion, compareType, equal ? "OK" : "NOK", status));
	                    }
                    	else
                    	{
                    		Console.WriteLine(string.Format("'Version von {1} lautet {0}'", strVersion, prz));
                    		status = (int)nagiosStatus.Ok;
                    	}
                    }

#if DEBUG
                    Console.Write("Press any key to continue . . . ");
                    Console.ReadKey(true);
#endif
                    return status;
                }
            }
            else
            {
                Console.WriteLine("Es muss mindestens der Name eines Prozesses angegeben werden oder der angegebene Prozess ist nicht gestartet");
                printUsage();
            }

#if DEBUG 
            Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
#endif

            return (int)nagiosStatus.Ok;
        }

        #region check Funktions
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
            string cmdEquals;

            if (Environment.GetCommandLineArgs().Length > 0)
            {
                cmdNeedVer = ParseCmdLineParam("version", Environment.CommandLine);
                cmdProcess = ParseCmdLineParam("process", Environment.CommandLine);
                cmdCompareType = ParseCmdLineParam("compare", Environment.CommandLine);
                cmdEquals = ParseCmdLineParam("equals", Environment.CommandLine);

                if (!string.IsNullOrWhiteSpace(cmdNeedVer))
                {
                    dicCmdArgs.Add("version", cmdNeedVer);
                }
                else
                    dicCmdArgs.Add("version", string.Empty);

                if (!string.IsNullOrWhiteSpace(cmdProcess))
                {
                    dicCmdArgs.Add("process", cmdProcess);
                }
                else
                    dicCmdArgs.Add("process", string.Empty);

                if (!string.IsNullOrWhiteSpace(cmdCompareType))
                {
                    dicCmdArgs.Add("compare", cmdCompareType);
                }
                else
                    dicCmdArgs.Add("compare", string.IsNullOrWhiteSpace(cmdNeedVer) ? string.Empty : "TEXT");

                if (!string.IsNullOrWhiteSpace(cmdCompareType))
                {
                    dicCmdArgs.Add("equals", cmdEquals);
                }
                else
                    dicCmdArgs.Add("equals", string.IsNullOrWhiteSpace(cmdEquals) ? string.Empty : "eq");
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
    	static bool check_compareParameters(string value)
    	{
        //	Übergebenen Action Parameter prüfen ob dieser im enum enthalten ist
	        cmdActionArgsCompareType result;	
	        
        	foreach (string enumarg in Enum.GetNames(typeof(cmdActionArgsCompareType)))
	        {
        		if (!string.IsNullOrWhiteSpace(value) && value.ToLower() == enumarg.ToLower())
                {
        			Debug.WriteLine("{0} = {1}",enumarg, value);
        			Enum.TryParse(enumarg, out result);
        			if ((compareType & cmdActionArgsCompareType.NONE) != 0)  
                    {
        				compareType = result;
        			}
        			else
        				compareType = compareType | result;
        		} 
	        }
        	Debug.WriteLine("Action = {0}", compareType);

            return (compareType & cmdActionArgsCompareType.NONE) != 0 ? false : true;
        //	####
    	}

        /// <summary>
        /// Prüft den Equals Parameter auf gültigkeit
        /// </summary>
        static void check_equalsParameters()
        {
            cmdActionArgsEqualType result;
            string value = "";
            dicCmdArgs.TryGetValue("equals", out value);

            foreach (string enumarg in Enum.GetNames(typeof(cmdActionArgsEqualType)))
            {
                if (!string.IsNullOrWhiteSpace(value) && value.ToLower() == enumarg.ToLower())
                {
                    Debug.WriteLine("{0} = {1}", enumarg, value);
                    Enum.TryParse(enumarg, out result);
                    if (equalType == cmdActionArgsEqualType.NONE)
                    {
                        equalType = result;
                    }
                    else
                        equalType = equalType | result;
                }
            }
            Debug.WriteLine("Equals = {0}", equalType);
        }

        /// <summary>
        /// Prüfen ob der angegebene Prozess aktiv ist
        /// Es wird der erste gefundene Prozess genutzt
        /// Ermitteln der Version
        /// Vergleichen der Version
        /// </summary>
        /// <param name="strProcess">Name des zu prüfenden Prozess</param>
        /// <returns></returns>
        static bool check_ProcessIsRunning(string strProcess, out string strVersion)
    	{
    		Process [] appProzess = Process.GetProcessesByName(strProcess);
    		FileVersionInfo fvi;
    		
    		if (appProzess.Length > 0)
            {
    			Debug.WriteLine(appProzess[0].MainModule.FileName, "check_ProcessIsRunning() -> FileName");
//    			Console.WriteLine("{0}", appProzess[0].MainModule.FileName);
    			Debug.WriteLine(appProzess[0].MainModule.FileVersionInfo.ToString(), "check_ProcessIsRunning() -> FileVersionInfo");
//    			Console.WriteLine("{0}", appProzess[0].MainModule.FileVersionInfo); //, "check_ProcessIsRunning() -> FileVersionInfo");
    			
    			fvi = appProzess[0].MainModule.FileVersionInfo;
                strVersion = string.Format("{0}.{1}.{2}.{3}", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);

                return true;
    		}
            strVersion = string.Empty;

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
            if (iVerNeed == null || iVerNum == null)
            {
                return false;
            }
            //  CompareType mit einbeziehen
            //  Nur Vergleichen wenn beide Versionen gleiche Anzahl Teile haben
            if (iVerNum.Length >= iVerNeed.Length)
            {
                //  Alle Teile vergleichen
                for (int i = 0; i < iVerNeed.Length; i++)
                {
                    if (((compareType & cmdActionArgsCompareType.Major) != 0) && i == 0)
                    {
                        if (iVerNeed[i] != iVerNum[i])
                        {
                            return false;
                        }
                        continue;
                    }
                    if (((compareType & cmdActionArgsCompareType.Minor) != 0) && i == 1)
                    {
                        if (iVerNeed[i] != iVerNum[i])
                        {
                            return false;
                        }
                        continue;
                    }
                    if (((compareType & cmdActionArgsCompareType.Build) != 0) && i == 2)
                    {
                        if (iVerNeed[i] != iVerNum[i])
                        {
                            return false;
                        }
                        continue;
                    }
                    if (((compareType & cmdActionArgsCompareType.Private) != 0) && i == 3)
                    {
                        if (iVerNeed[i] != iVerNum[i])
                        {
                            return false;
                        }
                        continue;
                    }
                    if (i > 3 && (compareType & cmdActionArgsCompareType.ALL) != 0)
                    {
                        if (iVerNeed[i] != iVerNum[i])
                        {
                            return false;
                        }
                    }
                    else
                        break;
                }
                return true;
            }
    		return false;
    	}

        #endregion

        #region helperFunktions
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
    			for(int i=0; i<verarr.Length;i++)
    			{
                    if (int.TryParse(verarr[i], out iTmp))
                        iver.SetValue(iTmp, i);
                    else
                        return null;
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
				int pos = 0;
				
				//	Ersetzem von Anführungszeichen in der Parameterliste
				cmdline = Regex.Replace(cmdline, "\"", "");

                //  Start auf ersten Parameter beginnend mit ' -' setzen
                if ((pos = cmdline.IndexOf(" -", start)) > -1)
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

        #endregion
    }
}