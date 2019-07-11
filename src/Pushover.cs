using System;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace JustinCredible.NetworkScanner
{
    public class Pushover
    {
        private const string PUSHOVER_API_URL = "https://api.pushover.net/1/messages.json";

        public static void Send(string token, string user, string message, bool verbose = false)
        {
            if (String.IsNullOrEmpty(token))
                throw new ArgumentException("To send a push notification via Pushover.net, an API token is required.");

            if (String.IsNullOrEmpty(user))
                throw new ArgumentException("To send a push notification via Pushover.net, an API user is required.");

            if (String.IsNullOrEmpty(message))
                throw new ArgumentException("To send a push notification via Pushover.net, an message is required.");

            var request = HttpWebRequest.Create(PUSHOVER_API_URL) as HttpWebRequest;
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            message = WebUtility.UrlEncode(message);

            var postData = $"token={token}&user={user}&message={message}";

            var byteArray = Encoding.UTF8.GetBytes(postData);
            request.ContentLength = byteArray.Length;

            var statusCode = -1;
            HttpWebResponse response = null;
            string responseBody = null;

            if (verbose)
            {
                Console.WriteLine("About to send Pushover.net API request...");
                Console.WriteLine($"URL: {request.RequestUri}");
                Console.WriteLine($"Content-Type: {request.ContentType}");
                Console.WriteLine($"Data: {postData}");
            }

            try
            {

                using (var stream = request.GetRequestStream())
                {
                    using (var streamWriter = new System.IO.StreamWriter(stream))
                    {
                        streamWriter.Write(postData);
                    }
                }

                response = request.GetResponse() as HttpWebResponse;
                statusCode = (int)response.StatusCode;

                using (var stream = response.GetResponseStream())
                {
                    using (var streamReader = new System.IO.StreamReader(stream))
                    {
                        responseBody = streamReader.ReadToEnd();
                    }
                }

                if (verbose)
                    Console.WriteLine($"Request Complete: Status Code: {statusCode} / Body: {responseBody}");

                Console.WriteLine("Push notification sent successfully.");
            }
            catch (WebException exception)
            {
                Console.Error.WriteLine($"Push notification send error: {statusCode} / Body: {responseBody}");

                if (verbose)
                    Console.WriteLine($"Push notification send error: WebException: {exception.Message}");
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Push notification send error: {statusCode} / Body: {responseBody}");

                if (verbose)
                    Console.WriteLine($"Push notification send error: Exception: {exception.Message}");
            }
        }
    }
}
