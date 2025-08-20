// YouTubeMusicApiClient.cs
using Newtonsoft.Json;
using SocketIOClient.Transport;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace YouTubeMusicWidget
{
    public class YouTubeMusicApiClient : IDisposable
    {
        private const string ApiUrl = "http://localhost:9863/api/v1";
        private readonly HttpClient _httpClient;
        private SocketIOClient.SocketIO _socket;
        public event Action<State> StateChanged;
        public event Action Authenticated;
        public event Action<string> LogMessage; // New event for logging
        private string _token;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_token);
        public void SetToken(string token) => _token = token;
        public string GetToken() => _token;

        public YouTubeMusicApiClient()
        {
            _httpClient = new HttpClient();
        }

        public async Task AuthenticateAsync(string appId, string appName, string appVersion)
        {
            try
            {
                var requestCodeBody = new { appId, appName, appVersion };
                var requestCodeJson = JsonConvert.SerializeObject(requestCodeBody);
                var requestCodeContent = new StringContent(requestCodeJson, Encoding.UTF8, "application/json");
                var requestCodeResponse = await _httpClient.PostAsync($"{ApiUrl}/auth/requestcode", requestCodeContent);
                requestCodeResponse.EnsureSuccessStatusCode();
                var requestCodeResponseBody = await requestCodeResponse.Content.ReadAsStringAsync();
                dynamic codeResponse = JsonConvert.DeserializeObject(requestCodeResponseBody);
                var code = codeResponse.code;

                var requestTokenBody = new { appId, code };
                var requestTokenJson = JsonConvert.SerializeObject(requestTokenBody);
                var requestTokenContent = new StringContent(requestTokenJson, Encoding.UTF8, "application/json");
                var requestTokenResponse = await _httpClient.PostAsync($"{ApiUrl}/auth/request", requestTokenContent);
                requestTokenResponse.EnsureSuccessStatusCode();
                var requestTokenResponseBody = await requestTokenResponse.Content.ReadAsStringAsync();
                dynamic tokenResponse = JsonConvert.DeserializeObject(requestTokenResponseBody);
                _token = tokenResponse.token;

                Authenticated?.Invoke();
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"Authentication failed: {ex.Message}");
            }
        }

        public async Task ConnectAsync()
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("Client must be authenticated before connecting.");

            _socket = new SocketIOClient.SocketIO(ApiUrl, new SocketIOClient.SocketIOOptions
            {
                Transport = TransportProtocol.WebSocket,
                Auth = new { token = _token }
            });

            _socket.On("state-update", response =>
            {
                try
                {
                    LogMessage?.Invoke($"Received state-update: {response}"); // Log raw data
                    var state = response.GetValue<State>();
                    if (state == null)
                    {
                        LogMessage?.Invoke("Parsed state is null.");
                    }
                    StateChanged?.Invoke(state);
                }
                catch (Exception ex)
                {
                    LogMessage?.Invoke($"Error parsing state-update: {ex.Message}");
                }
            });

            _socket.OnConnected += async (sender, e) =>
            {
                LogMessage?.Invoke("Socket connected. Requesting initial state.");
                await SendCommandAsync("player-get-state");
            };

            _socket.OnError += (sender, e) =>
            {
                LogMessage?.Invoke($"Socket Error: {e}");
            };

            await _socket.ConnectAsync();
        }


        public async Task SendCommandAsync(string command, object data = null)
        {
            if (!IsAuthenticated || _socket == null || !_socket.Connected)
            {
                LogMessage?.Invoke("Cannot send command: client not connected.");
                return;
            }

            if (data != null)
                await _socket.EmitAsync("command", new { command, data });
            else
                await _socket.EmitAsync("command", new { command });
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            if (_socket != null)
            {
                try { _socket.DisconnectAsync().Wait(); } catch { }
                _socket.Dispose();
            }
        }
    }
}
