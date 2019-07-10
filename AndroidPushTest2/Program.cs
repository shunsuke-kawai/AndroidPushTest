using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text;
using System.IO;

namespace AndroidPushTest2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var _hub = Notifications.Instance.Hub;

            switch (args.Length)
            {
                case 4:
                case 3:
                    if (args[0] != "REGIST" && args[0] != "SEND" && args[0] != "DIRECTSEND")
                    {
                        Console.WriteLine("param error.");
                        return;
                    }
                    break;
                case 2:
                    if (args[0] != "DELETE" && args[0] != "GET" && args[0] != "GET2" && args[0] != "ALLSEND")
                    {
                        Console.WriteLine("param error.");
                        return;
                    }
                    break;
                case 1:
                    if (args[0] != "GET")
                    {
                        Console.WriteLine("param error.");
                        return;
                    }
                    break;
                default:
                    Console.WriteLine($"param error.length={args.Length}");
                    return;
            }

            // arg[0] : REGIST/SEND/DELETE
            if (args[0] == "REGIST")
            {
                var deviceToken = args[1];
                string[] tags = { args[2] };
                var regId = args[3];
                var ret = await RegistDeviceAsync(_hub, deviceToken, tags, regId);
                Console.WriteLine("=== REGIST RESULT START ===");
                Console.WriteLine($"DeviceToken: {deviceToken} ");
                Console.WriteLine($"TagName: {tags[0]}");
                Console.WriteLine($"RegistrationId: {ret.RegistrationId}");
                Console.WriteLine($"ExpirationTime: {ret.ExpirationTime}");
                Console.WriteLine("=== REGIST RESULT END ===");

            }
            else if (args[0] == "SEND")
            {
                var message = args[1];
                string[] tags = { args[2] };
                var alert = "{ \"data\" : {\"message\":\"" + message + "\"}}";
                var ret = await SendNotificationAsync(_hub, alert, tags);
                Console.WriteLine("=== SEND RESULT START ===");
                Console.WriteLine($"NotificationId: {ret.NotificationId}");
                Console.WriteLine($"Success: {ret.Success}");
                Console.WriteLine($"Failure: {ret.Failure}");
                Console.WriteLine($"State: {ret.State}");
                Console.WriteLine($"TrackingId: {ret.TrackingId}");
                if (ret.Results != null)
                {
                    foreach (var res in ret.Results)
                    {
                        Console.WriteLine($"RegistrationId {res.RegistrationId} ,ApplicationPlatform {res.ApplicationPlatform} ,Outcome {res.Outcome} ,PnsHandle {res.PnsHandle}");
                    }
                }
                Console.WriteLine("=== SEND RESULT END ===");
            }
            else if (args[0] == "DIRECTSEND")
            {
                var message = args[1];
                string token = args[2];
                var alert = "{ \"data\" : {\"message\":\"" + message + "\"}}";
                var ret = await SendDirectAsync(_hub, alert, token);
                Console.WriteLine("=== DIRECTSEND RESULT START ===");
                Console.WriteLine($"NotificationId: {ret.NotificationId}");
                Console.WriteLine($"Success: {ret.Success}");
                Console.WriteLine($"Failure: {ret.Failure}");
                Console.WriteLine($"State: {ret.State}");
                Console.WriteLine($"TrackingId: {ret.TrackingId}");
                if (ret.Results != null)
                {
                    foreach (var res in ret.Results)
                    {
                        Console.WriteLine($"RegistrationId {res.RegistrationId} ,ApplicationPlatform {res.ApplicationPlatform} ,Outcome {res.Outcome} ,PnsHandle {res.PnsHandle}");
                    }
                }
                Console.WriteLine("=== DIRECTSEND RESULT END ===");
            }
            else if (args[0] == "ALLSEND")
            {
                var message = args[1];
                var alert = "{ \"data\" : {\"message\":\"" + message + "\"}}";
                var ret = await SendAllAsync(_hub, alert);
                Console.WriteLine("=== ALLSEND RESULT START ===");
                Console.WriteLine($"NotificationId: {ret.NotificationId}");
                Console.WriteLine($"Success: {ret.Success}");
                Console.WriteLine($"Failure: {ret.Failure}");
                Console.WriteLine($"State: {ret.State}");
                Console.WriteLine($"TrackingId: {ret.TrackingId}");
                if (ret.Results != null)
                {
                    foreach (var res in ret.Results)
                    {
                        Console.WriteLine($"RegistrationId {res.RegistrationId} ,ApplicationPlatform {res.ApplicationPlatform} ,Outcome {res.Outcome} ,PnsHandle {res.PnsHandle}");
                    }
                }
                Console.WriteLine("=== ALLSEND RESULT END ===");
            }
            else if (args[0] == "DELETE")
            {
                Console.WriteLine($"registid: {args[1]}");
                _hub.DeleteRegistrationAsync(args[1]).Wait();
            }
            else if (args[0] == "GET")
            {
                var ret = await _hub.GetRegistrationsByTagAsync(args[1], 0);

                Console.WriteLine($"ret = {ret.Count()}");

                foreach (var res in ret)
                {
                    if (res is FcmRegistrationDescription)
                    {
                        var ap = (FcmRegistrationDescription)res;
                        Console.WriteLine($"FcmRegistrationId: {ap.FcmRegistrationId}");
                    }

                    Console.WriteLine($"Type: {res.GetType().ToString()}");
                    Console.WriteLine($"ExpirationTime: {res.ExpirationTime}");
                    Console.WriteLine($"RegistrationId: {res.RegistrationId}");
                    Console.WriteLine($"PnsHandle: {res.PnsHandle}");
                    Console.WriteLine($"ETag: {res.ETag}");

                }
            }
            else if (args[0] == "GET2")
            {
                var ret = await _hub.GetRegistrationAsync<RegistrationDescription>(args[1]);

                if (ret is FcmRegistrationDescription)
                {
                    var ap = (FcmRegistrationDescription)ret;
                    Console.WriteLine($"FcmRegistrationId: {ap.FcmRegistrationId}");
                    Console.WriteLine($"Type: {ap.GetType().ToString()}");
                    Console.WriteLine($"ExpirationTime: {ap.ExpirationTime}");
                    Console.WriteLine($"RegistrationId: {ap.RegistrationId}");
                    Console.WriteLine($"PnsHandle: {ap.PnsHandle}");
                    Console.WriteLine($"ETag: {ap.ETag}");
                }

            }
            return;
        }

        private static async void SendTemplateNotificationAsync()
        {
            // Define the notification hub.
            NotificationHubClient hub = NotificationHubClient.CreateClientFromConnectionString("<connection string with full access>", "<hub name>");

            // Create an array of breaking news categories.
            var categories = new string[] { "World", "Politics", "Business", "Technology", "Science", "Sports" };

            // Send the notification as a template notification. All template registrations that contain
            // "messageParam" and the proper tags will receive the notifications.
            // This includes APNS, GCM, WNS, and MPNS template registrations.

            Dictionary<string, string> templateParams = new Dictionary<string, string>();

            foreach (var category in categories)
            {
                templateParams["messageParam"] = "Breaking " + category + " News!";
                await hub.SendTemplateNotificationAsync(templateParams, category);
            }
        }

        public static async Task<FcmRegistrationDescription> RegistDeviceAsync(NotificationHubClient _hub, string deviceToken, string[] tags, string regId)
        {
            var registration = new FcmRegistrationDescription(regId);
            registration.RegistrationId = deviceToken;
            registration.Tags = new HashSet<string>(tags);
            var ret = await _hub.CreateOrUpdateRegistrationAsync(registration);
            return ret;
        }

        public static async Task<string> GetRegistrationIdAsync(NotificationHubClient _hub, string deviceToken = null)
        {
            string newRegistrationId = null;

            if (deviceToken != null)
            {
                var registrations = await _hub.GetRegistrationsByChannelAsync(deviceToken, 50);
                foreach (RegistrationDescription registration in registrations)
                {
                    if (newRegistrationId == null)
                    {
                        newRegistrationId = registration.RegistrationId;
                    }
                    else
                    {
                        await _hub.DeleteRegistrationAsync(registration);
                    }
                }
            }
            if (newRegistrationId == null)
            {
                newRegistrationId = await _hub.CreateRegistrationIdAsync();
            }
            return newRegistrationId;
        }

        public static async Task<NotificationOutcome> SendAllAsync(NotificationHubClient _hub, string notice)
        {
            var ret = await _hub.SendFcmNativeNotificationAsync(notice);

            await LogFeedback(_hub, ret.NotificationId);

            return ret;
        }
        public static async Task<NotificationOutcome> SendDirectAsync(NotificationHubClient _hub, string notice, string token)
        {
            var notification = new FcmNotification(notice);
            var ret = await _hub.SendDirectNotificationAsync(notification, token);

            await LogFeedback(_hub, ret.NotificationId);

            return ret;
        }

        public static async Task<NotificationOutcome> SendNotificationAsync(NotificationHubClient _hub, string notice, string[] tags)
        {
            var ret = await _hub.SendFcmNativeNotificationAsync(notice, tags);

            await LogFeedback(_hub, ret.NotificationId);

            return ret;
        }

        private static async Task LogFeedback(NotificationHubClient _hub, string notificationId)
        {
            if (notificationId == null)
            {
                return;
            }

            var retryCount = 0;
            while (retryCount++ < 6)
            {
                try
                {
                    var result = await _hub.GetNotificationOutcomeDetailsAsync(notificationId);

                    if (result.State != NotificationOutcomeState.Completed)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                    else
                    {
                        var feedbackUri = result.PnsErrorDetailsUri;
                        if (!string.IsNullOrEmpty(feedbackUri))
                        {
                            Console.WriteLine("feedbackBlobUri: {0}", feedbackUri);
                            var feedbackFromBlob = ReadFeedbackFromBlob(new Uri(feedbackUri));
                            Console.WriteLine("Feedback from blob: {0}", feedbackFromBlob);
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error StackTrace: {0}", ex.StackTrace); 
                }
            }
        }

        private static string ReadFeedbackFromBlob(Uri uri)
        {
            var currentBlock = new CloudAppendBlob(uri);
            var stringbuilder = new StringBuilder();
            using (var streamReader = new StreamReader(currentBlock.OpenRead()))
            {
                while (!streamReader.EndOfStream)
                {
                    string currentFeedbackString = streamReader.ReadLine();
                    if (currentFeedbackString != null)
                    {
                        stringbuilder.AppendLine(currentFeedbackString);
                    }
                }
            }
            return stringbuilder.ToString();
        }
    }


    class Notifications
    {
        public static Notifications Instance = new Notifications();

        public NotificationHubClient Hub { get; set; }

        private Notifications()
        {

            Hub = NotificationHubClient.CreateClientFromConnectionString(
                "[your connection string]", "[your hub name]", false);
        }
    }
}
