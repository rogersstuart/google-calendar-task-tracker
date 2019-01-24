using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleCalendarTaskTracker
{
    public static class GoogleCalendarManager
    {
        public static readonly string DefaultCalendarName = "primary";

        private static string calendar_name = DefaultCalendarName;

        static string[] Scopes = { CalendarService.Scope.Calendar };
        static string ApplicationName = "Google Calendar API .NET Quickstart";

        public static Task CompleteTaskAsync(SuspendedTask susptsk)
        {
            return CompleteTaskAsync((ActiveTask)susptsk, susptsk.EndTimeStamp);
        }

        public static Task CompleteTaskAsync(ActiveTask actitask)
        {
            return CompleteTaskAsync(actitask, DateTime.Now);
        }

        public static Task CompleteTaskAsync(ActiveTask actitask, DateTime end_time)
        {
            UserCredential credential;

            if (!File.Exists(ConfigurationManager.CredentialFilePath))
                throw new Exception("Credential File Not Found");

            using (var stream =
                new FileStream(ConfigurationManager.CredentialFilePath, FileMode.Open, FileAccess.Read))
            {
                string credPath = System.Environment.GetFolderPath(
                    System.Environment.SpecialFolder.Personal);
                credPath = Path.Combine(credPath, ".credentials/calendar-dotnet-quickstart");

                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            var service = new CalendarService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Event g_ev = new Event();

            g_ev.Summary = "Task: " + actitask.Task + " Type: " + actitask.Type;

            string combined_string = "";
            foreach (string line in actitask.Notes)
                combined_string += line + Environment.NewLine;

            g_ev.Description = combined_string;

            g_ev.Start = new Func<EventDateTime>(() =>
            {
                EventDateTime ev_dt = new EventDateTime();
                ev_dt.DateTime = actitask.StartTimeStamp;
                return ev_dt;
            }).Invoke();

            g_ev.End = new Func<EventDateTime>(() =>
            {
                EventDateTime ev_dt = new EventDateTime();
                ev_dt.DateTime = end_time;
                return ev_dt;
            }).Invoke();

            EventsResource.InsertRequest request = new EventsResource.InsertRequest(service, g_ev, calendar_name);

            return request.ExecuteAsync();
        }

        public static string CalendarName
        {
            get
            {
                return calendar_name;
            }
            set
            {
                calendar_name = value;
                ConfigurationManager.SaveToFile();
            }
        }

        public static string[] CalendarNames
        {
            get
            {
                UserCredential credential;

                if (!File.Exists(ConfigurationManager.CredentialFilePath))
                    throw new Exception("Credential File Not Found");

                using (var stream =
                    new FileStream(ConfigurationManager.CredentialFilePath, FileMode.Open, FileAccess.Read))
                {
                    string credPath = System.Environment.GetFolderPath(
                        System.Environment.SpecialFolder.Personal);
                    credPath = Path.Combine(credPath, ".credentials/calendar-dotnet-quickstart");

                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                    Console.WriteLine("Credential file saved to: " + credPath);
                }

                var service = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                CalendarList cdrlst = service.CalendarList.List().Execute();

                string[] ids = cdrlst.Items.Select(x => x.Summary).ToArray();

                return ids;


                //EventsResource.InsertRequest request = new EventsResource.GetRequest(service, g_ev, calendar_name);

                //request.ExecuteAsync();
            }
        }
    }
}
