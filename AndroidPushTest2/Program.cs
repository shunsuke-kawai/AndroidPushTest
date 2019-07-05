using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;

namespace AndroidPushTest2
{
    class Program
    {
        static void Main(string[] args)
        {
            var _hub = Notifications.Instance.Hub;
            switch (args.Length)
            {
                case 4:
                case 3:
                    if (args[0] != "REGIST" && args[0] != "SEND")
                    {
                        Console.WriteLine("param error.");
                        return;
                    }
                    break;
                case 2:
                    if (args[0] != "DELETE" && args[0] != "GET")
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
                var ret = RegistDevice(_hub, deviceToken, tags, regId).Result;
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
                var ret = SendNotification(_hub, alert, tags).Result;
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
            else if (args[0] == "DELETE")
            {
                Console.WriteLine($"registid: {args[1]}");
                _hub.DeleteRegistrationAsync(args[1]).Wait();
            }
            else if (args[0] == "GET")
            {
                var ret = _hub.GetRegistrationsByTagAsync(args[1], 0);
                
                Console.WriteLine($"ret = {ret.Result.Count()}");

                foreach (var res in ret.Result)
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
            return;
        }
        public static async Task<FcmRegistrationDescription> RegistDevice(NotificationHubClient _hub, string deviceToken, string[] tags, string regId)
        {
            var registration = new FcmRegistrationDescription(regId);
            registration.RegistrationId = deviceToken;
            registration.Tags = new HashSet<string>(tags);
            var ret = await _hub.CreateOrUpdateRegistrationAsync(registration);
            return ret;
        }

        public static async Task<string> GetRegistrationId(NotificationHubClient _hub, string deviceToken = null)
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
        public static async Task<NotificationOutcome> SendNotification(NotificationHubClient _hub, string notice, string[] tags)
        {
            var ret = await _hub.SendFcmNativeNotificationAsync(notice, tags);
            return ret;
        }
        public static char[] StringToBytes(string str)
        {
            var bs = new List<char>();
            for (int i = 0; i < str.Length / 2; i++)
            {
                bs.Add((char)Convert.ToByte(str.Substring(i * 2, 2), 16));
            }
            return bs.ToArray();
        }

    }
    class Notifications
    {
        public static Notifications Instance = new Notifications();

        public NotificationHubClient Hub { get; set; }

        private Notifications()
        {
            Hub = NotificationHubClient.CreateClientFromConnectionString(
                "[your connection string]", "[your hub name]", true);
        }
    }
}
