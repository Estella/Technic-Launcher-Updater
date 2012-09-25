using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections;

namespace TechnicLauncher
{
    static class Program
    {
        public const string LauncherFile = "technic-launcher.jar";
        public static System.IO.StreamWriter log;
        public static string AppPath;
        public static TextWriter logger;
        static Form1 appForm;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            AppPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            AppPath = Path.Combine(AppPath, ".techniclauncher");
            if (!Directory.Exists(AppPath))
                Directory.CreateDirectory(AppPath);

            string LogPath = Path.Combine(AppPath, "logs");
            if (!Directory.Exists(LogPath))
                Directory.CreateDirectory(LogPath);
            string LogFile = Path.Combine(LogPath, "launcher_init.log");
            if (File.Exists(LogFile))
                File.Delete(LogFile);
            logger = File.AppendText(LogFile);
            LogBasicSystemInfo(logger);

            appForm = new Form1();
            Application.Run(appForm);
        }
        private static void LogBasicSystemInfo(TextWriter log)
        {
            log.WriteLine("Technic Windows Launcher Starting up.");
            log.WriteLine(getOSInfo());
            log.WriteLine(@"Contents of C:\Program Files\Java");
            foreach (string d in Directory.GetFileSystemEntries(@"C:\Program Files\Java"))
            {
                log.WriteLine("\t" + d);
            }
            log.WriteLine(@"Contents of C:\Program Files\Java (x86)");
            foreach (string d in Directory.GetFileSystemEntries(@"C:\Program Files (x86)\Java"))
            {
                log.WriteLine("\t" + d);
            }

            log.WriteLine(@"Registry(32-bit only) points to "+GetJavaInstallationPath());
            String path=LocateJavaFromPath();
            if (path==null)
                log.WriteLine("Java not found in user's PATH. (This is normal)");
            else
                log.WriteLine("Java found in PATH at: "+path);

            path=LocateJavaPath();
            if (path==null)
                log.WriteLine("JAVA_HOME is not set. (This is normal)");
            else
                log.WriteLine("JAVA_HOME points to "+path);

            path = GetJavaFileAssociationPath();
            if (path == null)
                log.WriteLine(@"Jarfiles do not open with Java. User error.");
            else
                log.WriteLine(@"Jarfiles open with " + path);

            path = LocateJavaFromProgramFiles();
            if (path==null)
                log.WriteLine(@"No Java found in C:\Program Files. User error.");
            else
                log.WriteLine(@"Found a Java by fast scan at: "+path);

        }

        private static int getOSArchitecture()
        {
            string pa = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
            return ((String.IsNullOrEmpty(pa) || String.Compare(pa, 0, "x86", 0, 3, true) == 0) ? 32 : 64);
        }
        private static string getOSInfo()
        {
            //Get Operating system information.
            OperatingSystem os = Environment.OSVersion;
            //Get version information about the os.
            Version vs = os.Version;

            //Variable to hold our return value
            string operatingSystem = "";

            if (os.Platform == PlatformID.Win32Windows)
            {
                //This is a pre-NT version of Windows
                switch (vs.Minor)
                {
                    case 0:
                        operatingSystem = "95";
                        break;
                    case 10:
                        if (vs.Revision.ToString() == "2222A")
                            operatingSystem = "98SE";
                        else
                            operatingSystem = "98";
                        break;
                    case 90:
                        operatingSystem = "Me";
                        break;
                    default:
                        break;
                }
            }
            else if (os.Platform == PlatformID.Win32NT)
            {
                switch (vs.Major)
                {
                    case 3:
                        operatingSystem = "NT 3.51";
                        break;
                    case 4:
                        operatingSystem = "NT 4.0";
                        break;
                    case 5:
                        if (vs.Minor == 0)
                            operatingSystem = "2000";
                        else
                            operatingSystem = "XP";
                        break;
                    case 6:
                        if (vs.Minor == 0)
                            operatingSystem = "Vista";
                        else
                            operatingSystem = "7";
                        break;
                    default:
                        break;
                }
            }
            //Make sure we actually got something in our OS check
            //We don't want to just return " Service Pack 2" or " 32-bit"
            //That information is useless without the OS version.
            if (operatingSystem != "")
            {
                //Got something.  Let's prepend "Windows" and get more info.
                operatingSystem = "Windows " + operatingSystem;
                //See if there's a service pack installed.
                if (os.ServicePack != "")
                {
                    //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                    operatingSystem += " " + os.ServicePack;
                }
                //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
                operatingSystem += " " + getOSArchitecture().ToString() + "-bit";
            }
            //Return the information we've gathered.
            return operatingSystem;
        }

        private static String GetJavaInstallationPath()
        {
            const string javaKey = "SOFTWARE\\JavaSoft\\Java Runtime Environment";
            using (var baseKey = Registry.LocalMachine.OpenSubKey(javaKey))
            {
                if (baseKey != null)
                {
                    String currentVersion = baseKey.GetValue("CurrentVersion").ToString();
                    using (var homeKey = baseKey.OpenSubKey(currentVersion))
                    {
                        if (homeKey != null)
                        {
                            String home = homeKey.GetValue("JavaHome").ToString();
                            if (!home.Equals(""))   {       // Paranoia: JavaHome might exist and be empty.
                                String javaPath=Path.Combine(home,@"bin\java.exe");
                                if (File.Exists(javaPath))  // Paranoia: JavaHome might be set and set wrongly.
                                    return javaPath;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private static String LocateJavaFromPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (path == null)
                return null;
            var folders = path.Split(';');
            foreach (var folder in folders)
            {
                if (folder.ToLowerInvariant().Contains("system32"))
                    continue;
                var javaPath = Path.Combine(folder, "java.exe");
                if (File.Exists(javaPath))
                {
                    return javaPath;
                }
            }
            return null;
        }

        private static String LocateJavaPath()
        {
            var path = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (path == null)
                return null;
            var folders = path.Split(';');
            foreach (var folder in folders)
            {
                var javaPath = Path.Combine(Path.Combine(folder, "bin"), "java.exe");
                if (File.Exists(javaPath))
                {
                    return javaPath;
                }
            }
            return null;
        }

        private static String LocateJavaFromProgramFiles()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            var path = Path.Combine(programFiles, "Java");
            var pathNoX86 = path.Replace(" (x86)", "");
            if (Directory.Exists(path)||Directory.Exists(pathNoX86))
            {
                var folders = new List<string>(Directory.GetDirectories(pathNoX86));
                if (folders.Count <= 0)
                    folders.AddRange(Directory.GetDirectories(path));
                folders.Reverse();
                foreach (var folder in folders)
                {
                    var javaPath = Path.Combine(Path.Combine(folder, "bin"), "java.exe");
                    if (File.Exists(javaPath))
                    {
                        return javaPath;
                    }
                }
            }
            return null;
        }

        // Note that this will only ever be a 32-bit invocation as TechnicLauncherUpdater is itself
        // 32-bit and reflection comes into play.
        private static String GetJavaFileAssociationPath()
        {
            const string javaKey = @"jarfile\shell\open\command";
            using (var baseKey = Registry.ClassesRoot.OpenSubKey(javaKey))
            {
                if (baseKey != null)
                {
                    String commandLine = baseKey.GetValue("").ToString(); String javaPath = "";
                    javaPath = commandLine.Remove(commandLine.IndexOf(@".exe") + 4).Replace("\"", "");
                    if (!javaPath.Equals(""))
                    {
                        if (File.Exists(javaPath))
                            return javaPath;
                    }

                }
            }
            return null;
        }

        private static String LocateJavaByExhaustiveSearch(String initialPath)
        {
            appForm.NotifyStatus("Searching for Java in " + initialPath);
            Application.DoEvents();
            try
            {
                string[] paths = Directory.GetDirectories(initialPath);
                foreach (string d in paths)
                {
                    Console.WriteLine(d);
                    try
                    {
                        string[] files = Directory.GetFiles(d, "java.exe");
                        foreach (string f in files)
                        {
                            // Retrieve the first java.exe available.
                            return f;
                        }
                    }
                    // folder 'd' is not accessible. So continue to prevent trying to recurse.
                    catch (Exception e)
                    {
                        continue;
                    }

                    // Recurse. Did we find anything?
                    String ret = LocateJavaByExhaustiveSearch(d);
                    if (ret != null)
                        return ret;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return null;    // If we get here, there's no Java on drive C:
        }
        public static void RunLauncher(String launcherPath)
        {
            // First let JAVA_HOME override.
            // Then go with whatever the registry says
            // Then look for the most up-to-date one in program files
            // Then look in user's PATH
            // Then see if jarfiles are associated properly (32-bit only)
            // Then scan the whole harddrive for Java (no guarantee of version or bitness)
            var java = LocateJavaPath() ??
                GetJavaInstallationPath() ??
                LocateJavaFromProgramFiles() ??
                LocateJavaFromPath() ??
                GetJavaFileAssociationPath() ??
                LocateJavaByExhaustiveSearch(@"C:\");
            if (java == null || java.Equals(""))
            {
                // May reduce badly-written forum posts.
                String msg = "Could not find a Java interpreter. Please follow these steps to install one.\n\n" +
                    "Go to http://java.com/download\n" +
                    "Click on 'All Java Downloads' on the left side\n";
                if (getOSArchitecture() == 64)
                        msg+="Click on 'Windows Offline (64-bit)'\n";
                else
                        msg+="Click on 'Windows Offline (32-bit)'\n";
                msg+="When that file finishes downloading, run it and allow it to install. Then restart this program.";

                MessageBox.Show(msg,"Technic Launcher",MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                var info = new ProcessStartInfo
                               {
                                   CreateNoWindow = true,
                                   WorkingDirectory = Application.StartupPath,
                                   FileName = java,
                                   Arguments = String.Format("-jar \"{0}\"", launcherPath),
                                   UseShellExecute = false
                               };
                try
                {
                    logger.WriteLine(String.Format("Starting the Java launcher as \"{0}\" -jar \"{1}\"",java,launcherPath));
                    Process.Start(info);
                }
                catch (Exception e)
                {
                    logger.WriteLine(e.ToString());
                    // A rare exception and should never happen based on changes in the Paranoia commit.
                    MessageBox.Show("Found Java, but couldn't start the launcher. Your Java installation is probably corrupt. Go to http://java.com and download then reinstall Java.\n\nAlso, the following exception was received:\n\n" + e.ToString());
                }
            }
            logger.Flush();
            logger.Close();
            Application.Exit();
        }

    }
}
