using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ProtoBuf;
using System.Drawing.Imaging;

namespace Bot
{
    public class API
    {
        public static int reactionTimeMS = 1000; // ms
        public static string logFile = "Log_API_Errors.txt";
        public static string cacheFile = "cache";

        public static int contextLimit = 1000000;
        public static int maxTokens = 10000;
        public static int maxNonResponse = 99;

        // ChatGPT API settings
        public static string aiName = "ChatGPT";
        public static string aiModel = "gpt-4.1";
        public static string apiKey = "";
        public static string apiEndpoint = "https://api.openai.com/v1/chat/completions";
        public static void Load()
        {
            LoadSettings();
            if (File.Exists("apikey.txt"))
            {
                apiKey = File.ReadAllText("apikey.txt").Trim();
                if (apiKey.StartsWith("xai-"))
                {
                    // Grok API settings
                    aiName = "Grok";
                    aiModel = "grok-4";
                    apiEndpoint = "https://api.x.ai/v1/chat/completions";
                    contextLimit = 250000;
                }
                Program.WriteLine( aiName+" API key loaded.");
            }
            else
            {
                Program.WriteLine("API key needed in apikey.txt");
                Console.Read();
            }
        }

        private static void LoadSettings()
        {
            try
            {
                string settingsFile = "settings.ini";
                if (!File.Exists(settingsFile))
                {
                    GenerateSettings(settingsFile);
                }

                var settings = new Dictionary<string, string>();
                foreach (string line in File.ReadAllLines(settingsFile))
                {
                    if (line.Contains("="))
                    {
                        string[] parts = line.Split(new char[] { '=' }, 2);
                        settings[parts[0].Trim()] = parts[1].Trim();
                    }
                }

                if (settings.ContainsKey("reactionTimeMS")) reactionTimeMS = int.Parse(settings["reactionTimeMS"]);
                if (settings.ContainsKey("maxTokens")) maxTokens = int.Parse(settings["maxTokens"]);
                if (settings.ContainsKey("maxNonResponse")) maxNonResponse = int.Parse(settings["maxNonResponse"]);
                if (settings.ContainsKey("maxClickDelay")) Live.maxClickDelay = int.Parse(settings["maxClickDelay"]);
                if (settings.ContainsKey("enableAlerts")) Live.enableAlerts = bool.Parse(settings["enableAlerts"]);
                if (settings.ContainsKey("saveScreenshot")) Live.enableAlerts = bool.Parse(settings["saveScreenshot"]);
                GenerateSettings(settingsFile);
            }
            catch (Exception ex)
            {
                Program.WriteLine("Error loading settings.ini, using default settings instead: " + ex.ToString());
            }
        }

        private static void GenerateSettings(string settingsFile)
        {
            var defaultSettings = new List<string>
            {
                "reactionTimeMS="+reactionTimeMS,
                "maxTokens="+maxTokens,
                "maxNonResponse="+maxNonResponse,
                "maxClickDelay="+Live.maxClickDelay,
                "enableAlerts="+Live.enableAlerts,
                "saveScreenshot="+Live.saveScreenshot
            };
            try
            {
                File.WriteAllLines(settingsFile, defaultSettings);
            }
            catch (Exception ex) { Program.WriteLine("Error generating default settings file: " + ex.ToString()); }
        }
        public static string role = "assistant";
        public static string systemPrompt { get { return "You are an AI bot able to control the desktop computer you are running on through mouse and keyboard. You see the desktop through a stream of screenshots after every action you take. The screen resolution is "+Live.GetWidth()+" x "+Live.GetHeight()+" (Width x Height). You have not been given any specific goal and are free to explore and pursue your own objectives, as well as interact and communicate back with the user. The open console window visible on the desktop is your interface and represents our ongoing conversation (your responses as well as the our prompts). Do NOT click in the console window because it'll freeze this process and prevent you from continuing. You can communicate directly with the user by just writing in the text after the JSON. You can communicate directly with the user by just thinking out loud, which will print in the console automatically, so do NOT try to type in the console window text area. To request input or response from the user, make sure to set the request-response to true in the JSON, but leave it false if you just need the next screenshot to continue without user input."; } }
        public static string instruction { get { return "Control the desktop by instructing in this exact JSON format: \n{\"mouse-x\": 100, \"mouse-y\": 200, \"shift-down\": false, \"alt-down\": false, \"control-down\": false, \"left-click\": true, \"right-click\": false, \"hold-click\": false, \"text-input\": \"\", \"enter-key\": false, \"backspace-key\": false, \"escape-key\": false, \"request-response\": false} \n\nwhere mouse-x and mouse-y are screen coordinates (-1 for no move), shift-down/alt-down/control-down are booleans to hold those keys during the action, left-click/right-click are booleans to perform those clicks after moving, hold-click for keeping the click down and not releasing, text-input is a string to type at the end of mouse movement (empty if none, make sure to double-escape quotes or it'll break the JSON), enter-key/backspace-key/escape-key for pressing the enter/backspace/escape after all other input (mouse and text), and request-response is a boolean to request a response from the user (true to request a user response, otherwise leave false to keep going and get the next screenshot but the user can't respond until it's set to true later). You can send multiple separate JSON instructions, and they'll be processed sequentially. After the JSON, explain in a short message what you are doing and include anything you want to say to the user. \n\nDo NOT click in the console window (neither the title bar nor the text area) because it'll freeze this process and prevent you from continuing. You can communicate directly with the user by just thinking out loud, which will print in the console automatically. Mind the token limit of "+maxTokens+" and 60-sec timeout, don't overthink any one step. Be careful not to do too many instructions in one step in case there is a misclick or things change. Make sure to check and verify your actions with the next screenshot."
            //+"When moving and clicking the mouse, try to move the mouse first without clicking, so you can verify the location on the next screenshot and THEN provide the click instruction in the next JSON."
            ; } }

        private static List<MessagePair> promptHistory = new List<MessagePair>();
        private static int promptTokens = 0;

        public static int GetPromptTokens() { return promptTokens; }

        public static bool HasPromptHistory() { return promptHistory.Any(); }

        private static void Clear()
        {
            promptHistory.Clear();
            promptTokens = 0;
        }

        public static LiveActions GetResponse(string prompt)
        {
            prompt = "The user says this: \"" + prompt + "\"\n\n";
            using (var client = new CustomWebClient())
            {
                client.Headers.Add("Authorization", "Bearer " + apiKey);
                client.Headers.Add("Content-Type", "application/json");
                System.Net.ServicePointManager.SecurityProtocol = (System.Net.SecurityProtocolType)3072;

                StringBuilder fullPrompt = new StringBuilder();
                if (promptTokens > contextLimit)
                {
                    fullPrompt.Append("We are continuing from a previous session, where you ran out of tokens to maintain context. Below are your notes (oldest to newest) to hopefully recall some idea of what you were doing:\n");
                    foreach (MessagePair msg in promptHistory)
                    {
                        LiveActions actions = ParseResponse(msg.Assistant);
                        if (actions != null)
                        {
                            string explanation = actions.explanation.Replace("Explanation: ", "").Replace("\n", " ");
                            fullPrompt.Append(" - \"" + explanation + "\"\n");
                        }
                    }
                    fullPrompt.Append("\n");
                    fullPrompt.Append("Now to continue:\n");
                    fullPrompt.Append(prompt);
                    Clear();
                }
                else
                {
                    fullPrompt.Append(prompt);
                }

                if(promptTokens==0) fullPrompt.Append("\n\n" + instruction);

                var messages = new List<dynamic> { new { role = "system", content = systemPrompt + "\n\n" + instruction } };
                foreach (MessagePair msg in promptHistory)
                {
                    messages.Add(new { role = "user", content = msg.User });
                    messages.Add(new { role = role, content = msg.Assistant });
                }
                messages.Add(new { role = "user", content = fullPrompt.ToString() });

                var requestBody = new
                {
                    model = aiModel,
                    stream = false,
                    messages = messages.ToArray(),
                    temperature = 0.0,
                    max_tokens = maxTokens
                };
                string json = JsonConvert.SerializeObject(requestBody);

                using (MemoryStream ms = new MemoryStream())
                {
                    Live.GetScreen().Save(ms, ImageFormat.Png);
                    byte[] imageBytes = ms.ToArray();
                    string base64String = Convert.ToBase64String(imageBytes);
                    string formattedBytes = string.Format("data:image/png;base64,{0}", base64String);
                    fullPrompt.Insert(0, "Attached is the latest screenshot of the computer desktop.\n\n");

                    var messagesWithScreenshot = new List<dynamic> { new { role = "system", content = systemPrompt } };
                    foreach (MessagePair msg in promptHistory)
                    {
                        messagesWithScreenshot.Add(new { role = "user", content = msg.User });
                        messagesWithScreenshot.Add(new { role = "assistant", content = msg.Assistant });
                    }

                    var userMessageContent = new object[]
                    {
                        new { type = "text", text = fullPrompt.ToString() },
                        new { type = "image_url", image_url = new { url = formattedBytes } }
                    };
                    messagesWithScreenshot.Add(new { role = "user", content = userMessageContent });

                    requestBody = new
                    {
                        model = aiModel,
                        stream = false,
                        messages = messagesWithScreenshot.ToArray(),
                        temperature = 0.0,
                        max_tokens = maxTokens
                    };
                    json = JsonConvert.SerializeObject(requestBody);
                }

                //LogAPI(json);

                string responseContent = "";
                try
                {
                    string responseJson = client.UploadString(apiEndpoint, "POST", json);
                    //LogAPI(responseJson);

                    JObject deserialized = JObject.Parse(responseJson);
                    responseContent = (string)deserialized["choices"][0]["message"]["content"];

                    promptHistory.Add(new MessagePair { User = fullPrompt.ToString(), Assistant = responseContent });

                    SaveCache();

                    promptTokens = (int)deserialized["usage"]["prompt_tokens"];
                    //Program.WriteLine(aiModel + " context tokens used: " + promptTokens);

                    return ParseResponse(responseContent);
                }
                catch (Exception ex)
                {
                    Program.WriteLine(aiModel + " API error: " + ex.Message);
                    LogAPI("Error with API JSON: " + ex.Message + "\n\n" + responseContent);
                    return null;

                }
            }


        }

        private static void LogAPI(string s)
        {
            File.AppendAllLines(logFile, new string[] { DateTime.Now + ": " +s  });
        }

        public class CustomWebClient : WebClient
        {
            // Custom timeout in milliseconds (default to 40 seconds).
            public int Timeout = 300 * 1000;

            protected override WebRequest GetWebRequest(Uri uri)
            {
                var request = base.GetWebRequest(uri);
                request.Timeout = Timeout; // Set the timeout here.
                return request;
            }
        }



        public static LiveActions ParseResponse(string response)
        {
            if (response.Trim() == "") return null;

            LiveActions actions = new LiveActions();
            string remainingResponse = response;
            try
            {
                //response = response.Replace("null", "\"\"");
                Regex regex = new Regex(@"\{.*?""mouse-x""\s*:\s*[0-9\.\-]+\s*,\s*""mouse-y""\s*:\s*[0-9\.\-]+\s*,\s*""shift-down""\s*:\s*(true|false)\s*,\s*""alt-down""\s*:\s*(true|false)\s*,\s*""control-down""\s*:\s*(true|false)\s*,\s*""left-click""\s*:\s*(true|false)\s*,\s*""right-click""\s*:\s*(true|false)\s*,\s*""hold-click""\s*:\s*(true|false)\s*,\s*""text-input""\s*:\s*""[^""]*""\s*\,\s*""enter-key""\s*:\s*(true|false)\s*,\s*""backspace-key""\s*:\s*(true|false)\s*,\s*""escape-key""\s*:\s*(true|false)\s*,\s*""request-response""\s*:\s*(true|false)\s*}", RegexOptions.Singleline | RegexOptions.IgnoreCase);

                Match match;
                while ((match = regex.Match(remainingResponse)).Success)
                {
                    string jsonStr = match.Value;
                    JObject json = JObject.Parse(jsonStr);
                    int mouseX = (int)json["mouse-x"];
                    int mouseY = (int)json["mouse-y"];
                    bool shiftDown = (bool)json["shift-down"];
                    bool altDown = (bool)json["alt-down"];
                    bool controlDown = (bool)json["control-down"];
                    bool leftClick = (bool)json["left-click"];
                    bool rightClick = (bool)json["right-click"];
                    bool holdClick = (bool)json["hold-click"];
                    string textInput = (string)json["text-input"];
                    bool enterKey = (bool)json["enter-key"];
                    bool backspaceKey = (bool)json["backspace-key"];
                    bool escapeKey = (bool)json["escape-key"];
                    bool requestResponse = (bool)json["request-response"];

                    actions.liveActions.Add(new LiveAction() { timestamp = DateTime.Now, mouseX = mouseX, mouseY = mouseY, shiftDown = shiftDown, altDown = altDown, controlDown = controlDown, leftClick = leftClick, rightClick = rightClick, holdClick = holdClick, textInput = textInput, enterKey = enterKey, backspaceKey = backspaceKey, escapeKey = escapeKey, requestResponse = requestResponse });

                    remainingResponse = remainingResponse.Substring(match.Index + match.Length).Trim();
                }

                actions.explanation = remainingResponse.Trim();

                if (actions.liveActions.Count == 0)
                {
                    throw new Exception("No valid JSON found");
                }

                return actions;
            }
            catch (Exception ex)
            {
                Program.WriteLine("Failed to parse API response: \n" + ex + "\n");
                LogAPI("Failed to parse API response: \n" + ex + "\n" + response);
                return null;
            }
        }

        public static void LoadCache()
        {
            if (File.Exists(cacheFile))
            {
                using (Stream stream = File.Open(cacheFile, FileMode.Open))
                {
                    APICache cache = Serializer.Deserialize<APICache>(stream);
                    promptHistory = cache.promptHistory;
                    promptTokens = cache.promptTokens;
                }
            }
        }

        public static void SaveCache()
        {
            using (Stream stream = File.Open(cacheFile, FileMode.Create))
            {
                APICache cache = new APICache { promptHistory = promptHistory, promptTokens = promptTokens };
                Serializer.Serialize(stream, cache);
            }
        }

    }

    [ProtoContract]
    public class MessagePair
    {
        [ProtoMember(1)]
        public string User { get; set; }
        [ProtoMember(2)]
        public string Assistant { get; set; }
    }

    [ProtoContract]
    public class APICache
    {
        [ProtoMember(1)]
        public List<MessagePair> promptHistory { get; set; }
        [ProtoMember(2)]
        public int promptTokens { get; set; }
    }
}