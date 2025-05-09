﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CroomsBellScheduleCS.Utils
{
    public class ApiClient
    {
        private readonly HttpClient _client = new();
        private readonly HttpClient _glitchClient = new();

        public ApiClient()
        {
            _glitchClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/134.0.0.0 Safari/537.36");
        }
        public static Result<T?> DecodeResponse<T>(string responseText)
        {
            Result<T?> result = new();
            try
            {
                ApiSimpleResponse? resp = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.ApiSimpleResponse);

                if (resp == null)
                {
                    result.OK = false;
                    result.Exception = new Exception("failed to decode json response");
                    return result;
                }

                if (resp.status == "OK")
                {
                    var typeInfo = SourceGenerationContext.Default.GetTypeInfo(typeof(ApiResponse<T>)) ?? throw new Exception("typeinfo not present: " + typeof(ApiResponse<T>).Name);
                    var apiResp = (ApiResponse<T>?)JsonSerializer.Deserialize(responseText, typeInfo);

                    if (apiResp != null)
                    {
                        result.Value = apiResp.data;
                        result.OK = true;
                    }
                    else
                    {
                        result.Exception = new("data decoded was null");
                        result.OK = false;
                    }
                }
                else
                {
                    var typeInfo = SourceGenerationContext.Default.GetTypeInfo(typeof(ApiResponse<ErrorResponse>)) ?? throw new Exception("typeinfo not present: ApiResponse<ErrorResponse>");
                    var apiResp = (ApiResponse<ErrorResponse>?)JsonSerializer.Deserialize(responseText, typeInfo);

                    result.OK = false;
                    if (apiResp != null)
                    {
                        result.ErrorValue = apiResp.data;
                    }
                    else
                    {
                        result.Exception = new("Failed to read error response message");
                    }
                }
            }
            catch (Exception ex)
            {
                result.OK = false;
                result.Exception = ex;
            }

            return result;
        }

        public async Task<Result> LoginAsync(string user, string pass)
        {
            var req = await RunLoginAsync(new LoginRequest() { username = user, password = pass });
            if (req.OK && req.Value != null)
            {
                SettingsManager.Settings.UserID = req.Value.uid;
                SettingsManager.Settings.SessionID = req.Value.sid;
                await SettingsManager.SaveSettings();
                return Result.Ok;
            }
            else
            {
                return new Result() { OK = false, Exception = req.Exception, Message = req.ErrorValue == null ? "" : req.ErrorValue.error };
            }
        }

        internal static string FormatResult<T>(Result<T> result)
        {
            if (result.OK)
                return "Server returned OK";
            if (result.ErrorValue != null)
                return result.ErrorValue.error.Contains("permissions") ? "Your login information has expired or is incorrect. Please login again." : result.ErrorValue.error;
            if (result.Exception != null)
            {
                if (result.Exception is SocketException)
                {
                    return "Network error, check your connection";
                }
                else
                {
                    return result.Exception.Message;
                }
            }
            return "Unspecified error";
        }

        internal static string FormatResult(Result result)
        {
            if (result.OK)
                return "Server returned OK";
            if (result.Message != null)
                return result.Message;
            if (result.Exception != null)
            {
                if (result.Exception is SocketException)
                {
                    return "Network error, check your connection";
                }
                else
                {
                    return result.Exception.Message;
                }
            }
            return "Unspecified error";
        }

        private static string SHA512(string input)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(input);
            var hashedInputBytes = System.Security.Cryptography.SHA512.HashData(bytes);

            // Convert to text
            // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte 
            var hashedInputStringBuilder = new System.Text.StringBuilder(128);
            foreach (var b in hashedInputBytes)
                hashedInputStringBuilder.Append(b.ToString("X2"));
            return hashedInputStringBuilder.ToString();
        }

        private async Task<Result<LoginResponse?>> RunLoginAsync(LoginRequest req)
        {
            req.password = SHA512(req.password).ToLower();

            StringContent content = new(JsonSerializer.Serialize(req, SourceGenerationContext.Default.LoginRequest));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await _client.PostAsync("https://api.croomssched.tech/users/login/", content);

            var responseText = await response.Content.ReadAsStringAsync();

            return DecodeResponse<LoginResponse>(responseText);
        }

        public async Task<Result<BellScheduleProperties?>> GetProperties()
        {
            try
            {
                var response = await _glitchClient.GetAsync("https://g-chrome-dino.glitch.me/cbsh.json");

                var responseText = await response.Content.ReadAsStringAsync();

                return new Result<BellScheduleProperties?>() { OK = true, Value = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.BellScheduleProperties) };
            }
            catch (Exception ex)
            {
                return new Result<BellScheduleProperties?>() { OK = false, Exception = ex };
            }
        }
        public async Task<Result<LunchData?>> GetLunchData()
        {
            try
            {
                var response = await _glitchClient.GetAsync("https://croomssched.glitch.me/infoFetch.json");

                var responseText = await response.Content.ReadAsStringAsync();

                return new Result<LunchData?>() { OK = true, Value = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.LunchData) };
            }
            catch (Exception ex)
            {
                return new Result<LunchData?>() { OK = false, Exception = ex };
            }
        }

        private void AddAuthorization()
        {
            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");


            _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"\"{SettingsManager.Settings.SessionID}\"");
        }
        public async Task<Result<CommandResponse?>> ValidateSessionAsync()
        {
            StringContent content = new("");
            // TODO: may cause exception or wrong header to be sent!

            AddAuthorization();
            var response = await _client.PostAsync("https://api.croomssched.tech/users/validateSID/" + SettingsManager.Settings.UserID, content);

            var responseText = await response.Content.ReadAsStringAsync();

            return DecodeResponse<CommandResponse>(responseText);
        }

        public async Task<Result> LogoutAsync()
        {
            AddAuthorization();
            var response = await _client.DeleteAsync("https://api.croomssched.tech/users/logout/" + SettingsManager.Settings.UserID);

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            {
                SettingsManager.Settings.UserID = null;
                SettingsManager.Settings.SessionID = null;
                await SettingsManager.SaveSettings();
                return Result.Ok;
            }
            return new Result() { OK = false };
        }

        public async Task<Result<FeedEntry[]?>> GetFeed()
        {
            var response = await _client.GetAsync("https://api.croomssched.tech/feed");

            var responseText = await response.Content.ReadAsStringAsync();

            return DecodeResponse<FeedEntry[]?>(responseText);
        }

        public async Task<Result<FeedEntry?>> PostFeed(string postContent, string postLink)
        {
            var properContent = WebUtility.HtmlEncode(postContent);

            // allow HTML content (if there is any)
            // todo improve detection
            if (properContent.Contains('/'))
            {
                properContent = properContent.Replace("&lt;", "<").Replace("&gt;", ">");
            }

            var req = new SubmitFeedRequest();

            if (string.IsNullOrEmpty(postLink))
            {
                req.data = properContent;
            }
            else
            {
                req.data = "<a target=\"CBSHfeed\" href=\"" + postLink + "\">" + properContent + "</a>";
            }

            StringContent content = new(JsonSerializer.Serialize(req, SourceGenerationContext.Default.SubmitFeedRequest));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            AddAuthorization();
            var response = await _client.PostAsync("https://api.croomssched.tech/feed", content);

            var responseText = await response.Content.ReadAsStringAsync();

            return DecodeResponse<FeedEntry>(responseText);
        }

        public async Task<Result<UsernameChangeRequest?>> ChangeUsernameAsync(string username)
        {
            var req = new UsernameChangeRequest() { username = username };
            StringContent content = new(JsonSerializer.Serialize(req, SourceGenerationContext.Default.UsernameChangeRequest));
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            AddAuthorization();

            var response = await _client.PatchAsync("https://api.croomssched.tech/users/changeUsername", content);

            var responseText = await response.Content.ReadAsStringAsync();

            return DecodeResponse<UsernameChangeRequest>(responseText);
        }

        internal async Task<Result<UserDetailsResponse?>> GetUserDetails()
        {
            AddAuthorization();

            var response = await _client.PostAsync("https://api.croomssched.tech/users/userDetails", null);

            var responseText = await response.Content.ReadAsStringAsync();

            return DecodeResponse<UserDetailsResponse>(responseText);
        }

        public async Task<Result<GetUserResult?>> GetUserByName(string username)
        {
            AddAuthorization();

            var response = await _client.GetAsync("https://api.croomssched.tech/users/" + username);

            var responseText = await response.Content.ReadAsStringAsync();

            return DecodeResponse<GetUserResult>(responseText);
        }

        public async Task<Result<SetProfilePictureResult?>> SetProfilePicture(byte[] c)
        {
            try
            {
                var formContent = new MultipartFormDataContent();

                var b = new ByteArrayContent(c);
                b.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                formContent.Add(b, "image", "image");

                using var requestMessage =
            new HttpRequestMessage(HttpMethod.Put, "https://api.croomssched.tech/users/setProfilePicture");

                requestMessage.Headers.TryAddWithoutValidation("Authorization", $"\"{SettingsManager.Settings.SessionID}\"");
                requestMessage.Content = formContent;

                var response = await _client.SendAsync(requestMessage);
                var responseText = await response.Content.ReadAsStringAsync();

                if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrEmpty(responseText))
                {
                    return new() { OK = false, Exception = new Exception($"Server error: {response.StatusCode}") };
                }
                ApiSimpleResponse? simple = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.ApiSimpleResponse) ?? throw new Exception("failed to decode json");

                if (simple.status == "OK")
                {
                    return new() { OK = true };

                }
                else
                {
                    SetProfilePictureResult? resp = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.SetProfilePictureResult) ?? throw new Exception("failed to decode erorr message");
                    if (resp.data == null) throw new("error data is null");
                    return new() { OK = false, ErrorValue = new ErrorResponse() { error = resp.data.error }, Value = resp };
                }
            }
            catch (Exception ex)
            {
                return new() { Exception = ex, OK = false };
            }
        }

        internal async Task<Result<AnnouncementData?>> GetAnnouncements()
        {
            try
            {
                var response = await _glitchClient.GetAsync("https://mikhail.croomssched.tech/annc.json");

                var responseText = await response.Content.ReadAsStringAsync();

                return new Result<AnnouncementData?>() { OK = true, Value = JsonSerializer.Deserialize(responseText, SourceGenerationContext.Default.AnnouncementData) };
            }
            catch (Exception ex)
            {
                return new Result<AnnouncementData?>() { OK = false, Exception = ex };
            }
        }
    }

    public class AnnouncementData
    {
        public List<Announcement> Announcements { get; set; } = [];
    }
    public class Announcement
    {
        public int ID { get; set; }
        public string Date { get; set; } = "";
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
    }
    public class LoginRequest
    {
        public string username { get; set; } = "";
        public string password { get; set; } = "";
    }
    public class CommandResponse
    {
        public bool result { get; set; } = false;
    }
    public class LoginResponse
    {
        public string sid { get; set; } = "";
        public string uid { get; set; } = "";
    }
    public class BellScheduleProperties
    {
        public string senseless { get; set; } = "";
        public string dailypoll { get; set; } = "";
    }
    public class LunchEntries
    {
        [JsonPropertyName("1")]
        public LunchEntry? Monday { get; set; }
        [JsonPropertyName("2")]
        public LunchEntry? Tuesday { get; set; }
        [JsonPropertyName("3")]
        public LunchEntry? Wednesday { get; set; }
        [JsonPropertyName("4")]
        public LunchEntry? Thursday { get; set; }
        [JsonPropertyName("5")]
        public LunchEntry? Friday { get; set; }
        [JsonPropertyName("6")]
        public string All { get; set; } = "";
    }
    public class LunchData
    {
        public LunchEntries lunch { get; set; } = new();
        public List<string> quickBits { get; set; } = [];
    }
    public class LunchEntry
    {
        public string name { get; set; } = "";
        public string image { get; set; } = "";
    }


    public class TeacherQuote
    {
        public string quote { get; set; } = "";
        public string teacher { get; set; } = "";
    }
    public class FeedEntry
    {
        public string data { get; set; } = "";
        public string store { get; set; } = "";
        public string id { get; set; } = "";
        public string uid { get; set; } = "";
        public DateTime create { get; set; }
        public DateTime delete { get; set; }
        public string createdBy { get; set; } = "";
        public bool verified { get; set; } = false;
    }
    public class SubmitFeedRequest
    {
        public string data { get; set; } = "";
    }
    public class UsernameChangeRequest
    {
        public string username { get; set; } = "";
    }
    public class ApiSimpleResponse
    {
        public string status { get; set; } = "";
    }
    public class ApiResponse<T>
    {
        public string status { get; set; } = "";
        public T? data { get; set; }
    }
    public class ErrorResponse
    {
        public string error { get; set; } = "No response from server";
    }
    public class Result
    {
        public bool OK { get; set; }
        public string Message { get; set; } = "Command OK";
        public Exception? Exception { get; set; }

        public static readonly Result Ok = new() { OK = true };
    }
    public class Result<T>
    {
        public bool OK { get; set; }
        public T? Value { get; set; }
        public ErrorResponse? ErrorValue { get; set; }
        public Exception? Exception { get; set; }
    }

    public class UserDetailsResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";
    }

    public class SetProfilePictureResultError
    {
        public string error { get; set; } = "";
    }

    public class SetProfilePictureResult
    {
        public string status { get; set; } = "";
        public SetProfilePictureResultError? data { get; set; }
    }

    public class GetUserResult
    {
        public string id { get; set; } = "";
        public string username { get; set; } = "";
    }
}
