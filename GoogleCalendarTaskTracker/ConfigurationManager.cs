using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace GoogleCalendarTaskTracker
{
    public static class ConfigurationManager
    {
        private static string tasks_and_types_file_path = "tasks.txt";
        private static string configuration_file_path = "gctt_config.bin";
        private static string credential_file_path = "client_secret.json";

        public static void SaveToFile()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();

                bf.Serialize(ms, tasks_and_types_file_path);
                bf.Serialize(ms, configuration_file_path);
                bf.Serialize(ms, GoogleCalendarManager.CalendarName);
                bf.Serialize(ms, credential_file_path);
                bf.Serialize(ms, ActiveTaskManager.WholeTasks);
                bf.Serialize(ms, ActiveTaskManager.SuspendedTasks);

                ms.Position = 0;

                File.WriteAllBytes(configuration_file_path, ms.ToArray());
            }
        }

        public static void ReadFromFile()
        {
            if (File.Exists(configuration_file_path))
            {
                try
                {
                    byte[] sdatb = File.ReadAllBytes(configuration_file_path);
                    using (MemoryStream ms = new MemoryStream(sdatb))
                    {
                        BinaryFormatter bf = new BinaryFormatter();

                        ms.Position = 0;

                        tasks_and_types_file_path = (string)bf.Deserialize(ms);

                        configuration_file_path = (string)bf.Deserialize(ms);

                        GoogleCalendarManager.CalendarName = (string)bf.Deserialize(ms);

                        credential_file_path = (string)bf.Deserialize(ms);

                        ActiveTaskManager.WholeTasks.Clear();
                        ActiveTaskManager.SuspendedTasks.Clear();

                        ActiveTaskManager.WholeTasks.AddRange((List<ActiveTask>)new BinaryFormatter().Deserialize(ms));
                        ActiveTaskManager.SuspendedTasks.AddRange((List<SuspendedTask>)new BinaryFormatter().Deserialize(ms));
                    }
                }
                catch (Exception ex)
                {
                    File.Delete(configuration_file_path);
                }
            }
        }

        public static string CredentialFilePath
        {
            get
            {
                return credential_file_path;
            }

            set
            {
                credential_file_path = value;
            }
        }

        public static string ConfigurationFilePath
        {
            get
            {
                return configuration_file_path;
            }

            set
            {
                ReadFromFile();
                File.Delete(configuration_file_path);
                configuration_file_path = value;
                SaveToFile();
            }
        }

        public static string TasksAndTypesFilePath
        {
            get
            {
                return tasks_and_types_file_path;
            }

            set
            {
                tasks_and_types_file_path = value;
            }
        }
    }
}
