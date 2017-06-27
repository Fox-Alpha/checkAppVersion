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
using CommandLine;
using CommandLine.Text;

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
    class Options
    {
        [Option('p', "process", Required = true,
        HelpText = "Der Prozess bei dem die Version geprüft werden soll")]
        public string Prozess { get; set; }

        [Option('v', "appversion", Required = false,
        HelpText = "Die Version auf die geprüft werden soll.")]
        public string Version { get; set; }

        private string _Compares;

        [Option('c', "compare", Required = false,
        HelpText = "Option auf was geprüft werden soll [Text|Major|Minor|Build|Private|All] default=TEXT")]
        //public cmdActionArgsCompareType compares { get; set; }
        public string Compares
        {
            get { return _Compares; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _Compares = string.Empty;
                }
                else
                    _Compares = value;
            }
        }

        [Option('e', "equals", Required = false,
        HelpText = "Wie geprüft werden soll [EQ|GT|LT|NEQ|GTE|LTE] default=EQ")]
        //public cmdActionArgsEqualType equals { get; set; }
        public string EqualsType { get; set; }
    }

    class Program
    {
        #region Eigenschaften
        //	Rückgabewerte für Nagios
        enum nagiosStatus
        {
            Ok = 0,
            Warning = 1,
            Critical = 2,
            Unknown = 3
        }

        static Dictionary<string, string> dicApplications;
        static Dictionary<string, string> dicCmdArgs;
        static int status = (int)nagiosStatus.Ok;

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
            LTE
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
        /// Parameter Zum Debuggen: -process=AM5 -version=1.8.0.70 -compare=Major,Build,private 
        public static int Main(string[] args)
        {
            Console.Title = "Nagios Client - NSClient++ App checkApplicationVersion";

            string prz, ver, cmp = "";

            string strVersion;
            //string strVerNeed;
            bool equal = false; ;
#if DEBUG
            int[] iVerPrz;
            int[] iVerNeed;
#endif
            compareType = cmdActionArgsCompareType.NONE;
            equalType = cmdActionArgsEqualType.NONE;

            //	Kommandozeilenparameter Auflistung
            //	Zwischenspeichern als Dictionary zum leichteren Zugriff
            dicCmdArgs = new Dictionary<string, string>();

            int exitCode = (int)nagiosStatus.Unknown;

            dicApplications = new Dictionary<string, string>()
            {
                {"JM4","JobManager 4"},
                {"AM5","ApplicationManager"},
                {"AMMT","AMMT"}
            };

            try
            {
                var result = CommandLine.Parser.Default.ParseArguments<Options>(args);

                if ((
                    exitCode = result
                    .MapResult(
                        options =>
                        {
                            if (!check_cmdLineArgs(new string[] { options.Prozess, options.Version, options.Compares, options.EqualsType }))
                            {
                                return (int)nagiosStatus.Warning;
                            }
                            return (int)nagiosStatus.Ok;

                        },
                        errors =>
                        {
                            Debug.WriteLine(errors);
                            Console.WriteLine(errors);
                            return (int)nagiosStatus.Critical;
                        }
                        )) == 2)
                {
                    return (int)nagiosStatus.Warning;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                return (int)nagiosStatus.Critical;
            }

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
                                if (!check_compareParameters(str))
                                {
                                    Console.WriteLine("Der angegebene vergleichs Parameter {0} ist ungültig", str);
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

#if DEBUG                
                string strMessageOut = string.Format ("Main() - Parameter: -process: '{1}' | -version: '{0}' | compare: '{2}' | equals: '{3}'",
                                !string.IsNullOrWhiteSpace (ver) ? ver : "LEER",
                                !string.IsNullOrWhiteSpace (prz) ? prz : "LEER",
                                !string.IsNullOrWhiteSpace (cmp) ? cmp : "LEER",
                                "N/A");
                Console.WriteLine(strMessageOut);
                Debug.WriteLine(strMessageOut);
#endif
                if (!check_ProcessIsRunning(prz, out strVersion))
                {
                    Console.WriteLine("'Der angegebene Prozess ist nicht gestartet oder der Prozessname ist falsch geschrieben' (" + prz + ")");
#if DEBUG
//                    Console.Write("Press any key to continue . . . ");
//                    Console.ReadKey(true);
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
#if DEBUG
                            iVerPrz = strVersion2IntArray(strVersion);
                            iVerNeed = strVersion2IntArray(ver);
                            equal = check_VersionNumbers(iVerPrz, iVerNeed);
#else
                            equal = check_VersionNumbers(strVersion2IntArray(strVersion), strVersion2IntArray(ver));
#endif
                        }
                        status = equal ? (int)nagiosStatus.Ok : (int)nagiosStatus.Critical;

#if DEBUG
                        Debug.WriteLine(string.Format("Version ist {3} (Erf. {0} ({2}) / App {1})", ver, strVersion, compareType, equal ? "OK" : "NOK"));
                        Console.WriteLine(string.Format("Version ist {3} (Erf. {0} ({2}) / App {1})", ver, strVersion, compareType, equal ? "OK" : "NOK"));

//                        Console.WriteLine("Press any key to continue . . . ");
//                        Console.ReadKey(true);
#endif

                        Console.WriteLine(string.Format("Version ist {3} (Erf. {0} ({2}) / App {1})|'(Erforderlich {0} ({2}) / Anwendung {1})'", ver, strVersion, compareType, equal ? "OK" : "NOK", status));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("'Version von {1} lautet {0}'", strVersion, prz));
                        status = (int)nagiosStatus.Ok;
#if DEBUG
//                        Console.WriteLine("Press any key to continue . . . ");
//                        Console.ReadKey(true);
#endif
                    }
                }
            }
            else
            {
                Console.WriteLine("Es muss mindestens der Name eines Prozesses angegeben werden oder der angegebene Prozess ist nicht gestartet (" + prz + ")");
            }
#if DEBUG
            Console.WriteLine("Press any key to continue . . . ");
            Console.ReadKey(true);
#endif
            return status;
        }

        #region check Funktions

        /// <summary>
        /// Prüfen und ermitteln welche Parameter der Anwendung übergeben wurden
        /// Parameter werden in einem Dictionary gespeichert
        /// </summary>
        static bool check_cmdLineArgs(params string[] options)
        {
            if (options.Length != 4)
            {
                return false;
            }

            //	Kommandozeilenparameter Auflistung
            //	Zwischenspeichern als Dictionary zum leichteren Zugriff
            string cmdProcess = string.IsNullOrWhiteSpace(options[0]) ? string.Empty : options[0];
            string cmdNeedVer = string.IsNullOrWhiteSpace(options[1]) ? string.Empty : options[1];
            string cmdCompareType = string.IsNullOrWhiteSpace(options[2]) ? string.Empty : options[2];
            string cmdEquals = string.IsNullOrWhiteSpace(options[3]) ? string.Empty : options[3];
#if DEBUG
            Console.WriteLine("Arg Count: " + Environment.GetCommandLineArgs().LongLength);
            Debug.WriteLine("Arg Count: " + Environment.GetCommandLineArgs().LongLength);
#endif
            if (!string.IsNullOrWhiteSpace(cmdProcess))
            {
                dicCmdArgs.Add("process", cmdProcess);
            }
            else
                dicCmdArgs.Add("process", string.Empty);

            if (!string.IsNullOrWhiteSpace(cmdNeedVer))
            {
                dicCmdArgs.Add("version", cmdNeedVer);
            }
            else
                dicCmdArgs.Add("version", string.Empty);

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

            return true;
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
                    Debug.WriteLine("{0} = {1}", enumarg, value);
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
            Process[] appProzess = Process.GetProcessesByName(strProcess);
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
            strVersion = "Prozess wurde nicht gefunden oder ist nicht gestartet (" + strProcess + ")"; //string.Empty;
            Console.WriteLine(strVersion);

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
            if (!string.IsNullOrWhiteSpace(strVerNum) && !string.IsNullOrWhiteSpace(strVerNeed))
            {
                if (strVerNum != strVerNeed)
                {
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
                    //                    else
                    //                        break;
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

            if (verarr.Length > 0)
            {
                for (int i = 0; i < verarr.Length; i++)
                {
                    if (int.TryParse(verarr[i], out iTmp))
                        iver.SetValue(iTmp, i);
                    else
                        return null;
                }
            }
            else
                return new int[] { 1, 0, 0, 0 };

            return iver;
        }

        #endregion
    }
}