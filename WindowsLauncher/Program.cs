using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TechnicLauncher
{
    static class Program
    {
        public const string LauncherFile = "technic-launcher.jar";
        public static string AppPath;
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

            appForm = new Form1();
            Application.Run(appForm);
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
            if (!Directory.Exists(path))
            {
                if (path.Contains("(x86)"))
                {
                    path = path.Replace(" (x86)", "");
                    if (!Directory.Exists(path))
                        return null;
                }
                else
                {
                    return null;
                }
            }
            var folders = new List<string>(Directory.GetDirectories(path));
            if (folders.Count <= 0)
            {
                path = path.Replace("Program Files", "Program Files (x86)");
                if (!Directory.Exists(path))
                    return null;
                folders.AddRange(Directory.GetDirectories(path));
            }
            folders.Reverse();
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
            var java = GetJavaInstallationPath() ?? 
                LocateJavaFromPath() ??
                LocateJavaPath() ??
                LocateJavaFromProgramFiles() ??
                GetJavaFileAssociationPath() ??
                LocateJavaByExhaustiveSearch(@"C:\");
            if (java == null || java.Equals(""))
            {
                // May reduce badly-written forum posts.
                MessageBox.Show("Can't find java directory. Go to http://java.com and download then reinstall Java.");
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
                    Process.Start(info);
                }
                catch (Exception e)
                {
                    // A rare exception and should never happen based on changes in the Paranoia commit.
                    MessageBox.Show("Found Java, but couldn't start the launcher. Your Java installation is probably corrupt. Go to http://java.com and download then reinstall Java.\n\nAlso, the following exception was received:\n\n" + e.ToString());
                }
            }
            Application.Exit();
        }

    }
}
