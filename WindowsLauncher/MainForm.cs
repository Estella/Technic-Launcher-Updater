using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace TechnicLauncher
{
    public partial class Form1 : Form
    {
        public string LauncherURL = "http://206.217.207.1/Technic/";
        private readonly string _launcherFile = Path.Combine(Program.AppPath, Program.LauncherFile);
        private readonly string _launcherBackupFile = Path.Combine(Program.AppPath, Program.LauncherFile + ".bak");
        private readonly string _launcherTempFile = Path.Combine(Program.AppPath, Program.LauncherFile + ".temp");
        private int _hashDownloadCount, _launcherDownloadCount;
        private Exception error;

        public delegate void IsAddressibleCallback(bool isAddressable, object userToken);

        public void IsAddressible(Uri uri, IsAddressibleCallback callback, object userToken)
        {
            using (var client = new MyClient())
            {
                client.HeadOnly = true;
                client.DownloadStringCompleted += new DownloadStringCompletedEventHandler(IsAddressibleHeadString);
                client.DownloadStringAsync(uri, new object[] { callback, userToken });
            }
        }

        void IsAddressibleHeadString(object sender, DownloadStringCompletedEventArgs e)
        {
            IsAddressibleCallback callback = (IsAddressibleCallback)((object[])e.UserState)[0];
            bool isAddressible = true;
            if (e.Error != null) isAddressible = false;

            callback.Invoke(isAddressible, ((object[])e.UserState)[1]);
        }



        public Form1()
        {
            InitializeComponent();
        }

        private void DownloadHash()
        {
            lblStatus.Text = @"Checking Launcher Version...";
            pbStatus.Style = ProgressBarStyle.Marquee;
            var uri = new Uri(String.Format("{0}CHECKSUM.md5", LauncherURL));

            IsAddressible(uri, new IsAddressibleCallback(IsAddressibleResult), uri);
        }

        private void IsAddressibleResult(bool isAddressible, object uriObj)
        {
            Uri uri = (Uri)uriObj;

            var versionCheck = new WebClient();
            versionCheck.DownloadStringCompleted += DownloadStringCompleted;

            if (_hashDownloadCount < 3 && isAddressible)
            {
                _hashDownloadCount++;
                versionCheck.DownloadStringAsync(uri, _launcherFile);
            }
            else
            {
                Program.RunLauncher(_launcherFile);
                Close();
            }
        }

        private void DownloadLauncher()
        {
            lblStatus.Text = String.Format(@"Downloading Launcher ({0}/{1})..", _launcherDownloadCount, 3);
            pbStatus.Style = ProgressBarStyle.Continuous;
            if (_launcherDownloadCount < 3)
            {
                _launcherDownloadCount++;
                var wc = new WebClient();
                wc.DownloadProgressChanged += DownloadProgressChanged;
                wc.DownloadFileCompleted += DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri(String.Format("{0}technic-launcher.jar", LauncherURL)), _launcherTempFile);
            }
            else
            {
                MessageBox.Show("Error", error.Message);
                Program.RunLauncher(_launcherFile);
                Close();
            }
        }

        void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                error = e.Error;
                DownloadLauncher();
                return;
            }
            lblStatus.Text = @"Running Launcher..";
            pbStatus.Value = 100;

            if (File.Exists(_launcherBackupFile))
                File.Delete(_launcherBackupFile);
            if (File.Exists(_launcherFile))
                File.Move(_launcherFile, _launcherBackupFile);
            File.Move(_launcherTempFile, _launcherFile);
            Program.RunLauncher(_launcherFile);
            Close();
        }

        void DownloadStringCompleted(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                DownloadHash();
                return;
            }
            MD5 hash = new MD5CryptoServiceProvider();
            String md5 = null, serverMD5 = null;
            var sb = new StringBuilder();

            try
            {

                using (var fs = File.Open(_launcherFile, FileMode.Open, FileAccess.Read))
                {
                    var md5Bytes = hash.ComputeHash(fs);
                    foreach (byte hex in md5Bytes)
                        sb.Append(hex.ToString("x2"));
                    md5 = sb.ToString().ToLowerInvariant();

                    fs.Seek(0, SeekOrigin.Begin);
                    Byte[] magic = new Byte[2];
                    fs.Read(magic, 0, 2);
                    if (magic[0] != 80 || magic[1] != 75)
                        throw new ApplicationException();
                }
            }
            catch (IOException ioException)
            {
                Console.WriteLine(ioException.Message);
                Console.WriteLine(ioException.StackTrace);

                MessageBox.Show("Cannot check launcher version, the launcher is currently open!", "Launcher Currently Open", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Application.Exit();
                return;
            }
            catch (ApplicationException appException)
            {
                MessageBox.Show("Unable to download launcher. Please check your internet connection by opening www.techniclauncher.net in your webbrowser.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
                return;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.WriteLine(exception.StackTrace);

                MessageBox.Show("Error checking launcher version", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            var lines = e.Result.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!line.Contains("technic-launcher.jar")) continue;
                serverMD5 = line.Split('|')[0].ToLowerInvariant();
                break;
            }

            if (serverMD5 != null && serverMD5.Equals(md5)) {
                Program.RunLauncher(_launcherFile);
                Close();
            }
            else
            {
                DownloadLauncher();
            }
        }

        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            lblStatus.Text = String.Format("Downloaded {0}% of launcher..", e.ProgressPercentage);
            pbStatus.Value = e.ProgressPercentage;
        }
        public void NotifyStatus(String status)
        {
            lblStatus.Text = status;
            lblStatus.Width = 8000;
            pbStatus.Value = 100;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists(_launcherFile))
            {
                DownloadHash();
            }
            else
            {
                DownloadLauncher();
            }
        }
    }
}
