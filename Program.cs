using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;

namespace luaWatcher
{
    class Program
    {
        public static DateTime lastRead = DateTime.MinValue;
        public static String uri = System.Environment.CurrentDirectory;
        static void Main(string[] args)
        {

            using (FileSystemWatcher watcher = new FileSystemWatcher())
            {
                watcher.Path = uri;
                watcher.IncludeSubdirectories  = true;
                watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                watcher.Filter = "*.lua";

                watcher.Changed += OnChanged;

                watcher.EnableRaisingEvents = true;

                WriteColor("Welcome to [luaWatcher https://github.com/enesbayrktar/luaWatcher]", ConsoleColor.Yellow);
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
                WriteColor($"luaWatcher : '[{e.Name}' {e.ChangeType} {lastRead} {lastWriteTime}]", ConsoleColor.Yellow);

                Process luac = new Process();

                luac.StartInfo.FileName   = "luac_mta.exe";
                luac.StartInfo.Arguments = $"-e3 -o {e.FullPath}c {e.FullPath}";

                luac.Start();

                foreach(string f in Directory.EnumerateFiles(uri, "meta.xml", SearchOption.AllDirectories))
                {
                    XmlDocument document = new XmlDocument();
                    document.Load(f);

                    string FormattedName = e.Name.Substring(e.Name.IndexOf($"\\") + 1);
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
    }
}
