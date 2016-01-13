using System;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace NbuExplorer
{
    [Serializable]
    public class Preferences
    {
        public static readonly Preferences Instance;

        public static string AppSettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "NbuExplorer");
        private static string storeFileName = Path.Combine(AppSettingsFolder, "Preferences.xml");

        static XmlSerializer xs = new XmlSerializer(typeof(Preferences));

        //settings values
        public Size WindowSize = new Size(600, 450);
        public bool RecalculateUtcToLocal = true;
        public bool ExportOnlySelected = false;
        public bool SaveDataOnExit = false;
        public bool AllowDragAndDrop = false;
        public bool ShowMessageSizeColumn = false;
        public MessageSourcesPreference MessageSources = new MessageSourcesPreference();
        public BruteForcePreference BruteForce = new BruteForcePreference();

        //subclases
        [Serializable]
        public class MessageSourcesPreference
        {
            public bool Vmg = true;
            public bool Symbian = true;
            public bool Predef = true;
            public bool Binary = true;
        }

        [Serializable]
        public class BruteForcePreference
        {
            public bool Jpg = true;
            public bool Mp4 = true;
            public bool Vcards = true;
            public bool Zip = true;
        }

        private Preferences()
        {
        }

        static Preferences()
        {
            try
            {
                if (File.Exists(storeFileName))
                {
                    var instance = Load();
                    if (instance != null)
                    {
                        Instance = instance;
                        return;
                    }
                }
                Directory.CreateDirectory(AppSettingsFolder);
            }
            catch
            {
                //when exception happens, default instance will be created on next line
            }
            Instance = new Preferences();
        }

        private static Preferences Load()
        {
            using (var sr = new StreamReader(storeFileName))
            {
                return (Preferences)xs.Deserialize(sr);
            }
        }

        public static void Save()
        {
            using (TextWriter tw = new StreamWriter(storeFileName))
            {
                xs.Serialize(tw, Instance);
            }
        }

    }
}
