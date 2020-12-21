using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace luaWatcher
{
    class Program
    {
        public static string welcomeArt = @"
        .__                                   __         .__
        |  |  __ _______     __  _  _______ _/  |_  ____ |  |__   ___________
        |  | |  |  \__  \    \ \/ \/ /\__  \\   __\/ ___\|  |  \_/ __ \_  __ \
        |  |_|  |  // __ \_   \     /  / __ \|  | \  \___|   Y  \  ___/|  | \/
        |____/____/(____  /____\/\_/  (____  /__|  \___  >___|  /\___  >__|
                        \/_____/           \/          \/     \/     \/

        ";
        public static DateTime lastRead = DateTime.MinValue;
        public static String uri = System.Environment.CurrentDirectory;
        static void Main(string[] args)
        {
            Console.Title = "Lua Watcher (via https://mtasa.com/)";
            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = uri;
                watcher.IncludeSubdirectories  = true;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.lua";

                watcher.Changed += OnChanged;

                watcher.EnableRaisingEvents = true;
                Console.WriteLine(welcomeArt);
                WriteColor("Welcome to [luaWatcher https://github.com/enesbayrktar/lua_watcher]", ConsoleColor.Yellow);
                WriteColor($"Started watching ['{uri}'] with [subfolders.]", ConsoleColor.Yellow);
                WriteColor("Type ['q'] to stop [luaWatcher].", ConsoleColor.Yellow);
                Console.WriteLine(" ");
                while (Console.Read() != 'q') ;
            }
        }

        static void OnChanged(object source, FileSystemEventArgs e)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);
            if (lastWriteTime != lastRead)
            {
                WriteColor($"luaWatcher : File changed: '[{e.Name}]' [{e.ChangeType} {lastRead} {lastWriteTime}]", ConsoleColor.Yellow);

                Process luac = new Process();

                luac.StartInfo.FileName   = "lib/luac_mta.exe";
                luac.StartInfo.Arguments = $"-e3 -o {e.Name}c {e.Name}";

                luac.Start();

                string XmlFolder = GetRootFolder(e.Name);

                foreach(string f in Directory.EnumerateFiles(XmlFolder, "meta.xml", SearchOption.AllDirectories))
                {
                    XmlDocument document = new XmlDocument();
                    document.Load(f);

                    string FormattedName = e.Name.Substring(e.Name.IndexOf($"\\") + 1).Replace("\\", "/");
                    XmlNode node = document.SelectSingleNode($"//script[@src='{FormattedName}']");
                    if (node != null) {
                        node.Attributes["src"].Value = FormattedName + 'c';
                        WriteColor($"luaWatcher : ['{f}'] is updated successfully. New value is [{FormattedName}c]", ConsoleColor.Yellow);
                    }
                    document.Save(f);
                }

                lastRead = lastWriteTime;
            }
        }

        static void WriteColor(string message, ConsoleColor color)
        {

            var pieces = Regex.Split(message, @"(\[[^\]]*\])");

            for(int i=0;i<pieces.Length;i++)
            {
                string piece = pieces[i];

                Console.ForegroundColor = ConsoleColor.DarkGray;

                if (piece.StartsWith("[") && piece.EndsWith("]"))
                {
                    Console.ForegroundColor = color;
                    piece = piece.Substring(1,piece.Length-2);
                }

                Console.Write(piece);
                Console.ResetColor();

            }

            Console.WriteLine();

        }

        static string GetRootFolder(string path)
        {
            while (true)
            {
                string temp = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(temp))
                    break;
                path = temp;
            }
            return path;
        }
    }
}
