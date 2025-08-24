using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SurfVpnClientTest1.Services
{
    public class PkceAuthService
    {
        private const string DeviceAuthorizationUrl = "https://localhost:7295/connect/authorize";
        private const string TokenUrl = "https://localhost:7295/connect/token";
        private const string ClientId = "pkce-client";
        private const string Scope = "openid profile email";

        public string CodeVerifier { get; private set; }
        public string CodeChallenge { get; private set; }

        public PkceAuthService()
        {
            GeneratePkceCodes();
        }

        private void GeneratePkceCodes()
        {
            // Generate a random code verifier
            var rng = new RNGCryptoServiceProvider();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            CodeVerifier = Base64UrlEncode(bytes);

            // Generate code challenge
            using var sha256 = SHA256.Create();
            var challengeBytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(CodeVerifier));
            CodeChallenge = Base64UrlEncode(challengeBytes);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public string GetAuthorizationUrl(string redirectUri)
        {
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["client_id"] = ClientId;
            query["scope"] = Scope;
            query["response_type"] = "code";
            query["redirect_uri"] = redirectUri;
            query["code_challenge"] = CodeChallenge;
            query["code_challenge_method"] = "S256";
            return $"{DeviceAuthorizationUrl}?{query}";
        }

        public async Task<string> ExchangeCodeForTokenAsync(string code, string redirectUri)
        {
            using var client = new HttpClient();
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", ClientId),
                new KeyValuePair<string, string>("code_verifier", CodeVerifier)
            });
            var response = await client.PostAsync(TokenUrl, content);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> ListenForRedirectAsync(string redirectUri, int timeoutSeconds = 120)
        {
            var uri = new Uri(redirectUri);
            string prefix = $"{uri.Scheme}://{uri.Host}:{uri.Port}/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(prefix);
            listener.Start();

            var task = listener.GetContextAsync();
            if (await Task.WhenAny(task, Task.Delay(timeoutSeconds * 1000)) == task)
            {
                var context = task.Result;
                var request = context.Request;
                var response = context.Response;

                // Extract code from query string
                string code = request.QueryString["code"];

                // Respond to browser
                string responseString = "<html><body>Login complete. You may close this window.</body></html>";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                listener.Stop();
                return code;
            }
            else
            {
                listener.Stop();
                throw new TimeoutException("Timeout waiting for authorization response.");
            }
        }
    }
}
