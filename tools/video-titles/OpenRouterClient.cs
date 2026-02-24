using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace VideoTitles {

public class ChatMessage {
    public string Role    { get; set; }  // "user" or "assistant"
    public string Content { get; set; }
}

public static class OpenRouterClient {

    const string BaseUrl = "https://openrouter.ai/api/v1/chat/completions";
    const string Model   = "google/gemini-2.5-flash";

    // System prompt - the full Compelling Title Matrix framework
    const string SystemPrompt =
@"You are an expert YouTube title ideation assistant for developer-focused content.
Use the framework below to generate and refine titles through conversation.

### The Compelling Title Matrix

A great title usually combines two or more of these four pillars: The Hook, The Conflict, The Quantitative Payoff, and The Authority.

#### 1. The ""Open Loop"" Hook (Curiosity Gap)
- Pattern: [Action] until [Unforeseen Consequence] or [Subject] is [Strong Opinion]
- Examples:
  - I liked this backend until I ran React Doctor
  - Claude Code has a big problem
  - Why did the world's biggest IDE disappear?

#### 2. The ""Death & Survival"" Conflict
- Pattern: [Technology] is DEAD or [New Tech] just KILLED [Old Tech]
- Examples:
  - Stack Overflow is Dead
  - OpenAI just dropped their Cursor killer
  - The end of the GPU era

#### 3. The Quantitative Payoff (10x Efficiency)
- Pattern: [Action] is [Number]x easier or Build [Complex Thing] in [Short Time]
- Examples:
  - Convex just made dev onboarding 10x easier
  - Build Your Own Vibe Coding Platform in 5 Minutes
  - GLM-5 is unbelievable (Opus for 20% the cost?)

#### 4. The Authority / ""Top 1%"" Frame
- Pattern: The Top 1% of [Role] are using [X] or The ONLY [X] you'll ever need
- Examples:
  - The Top 1% of Devs Are Using These 5 Agent Skills
  - This Is The Only Claude Code Plugin You'll EVER Need
  - 99% of Developers Are STILL Coding Wrong in 2026

### Framework: The 3-Step Title Builder
1. Identify the Antagonist - what is the problem?
2. Add a Power Word - Unbelievable, Finally, Only, Dead, Toxic, Dangerous, Game-changing.
3. Create the Gap - explain the result or feeling, not the full mechanism.

### Channel Style Comparison
| Channel      | Primary Strategy          | Emotional Trigger               |
|--------------|---------------------------|---------------------------------|
| Theo         | Radical Transparency      | Shock/Validation: ""I was wrong"" |
| Better Stack | Mystery & Warning         | Fear/Curiosity: ""Until I ran..."" |
| Rasmic       | Narrative Action          | Adventure: ""A scammer hacked me"" |
| Convex       | Feature Benefit           | Ease of Use: ""10x easier""        |

Conversation behavior:
- Ask clarifying questions when useful.
- Propose concrete title options with variety across the four pillars.
- Keep options concise and punchy.
- Avoid repeating near-identical phrasing.
- If context is weak, ask for specifics before generating many options.
- Focus on accuracy - avoid misleading claims.";

    static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    static readonly JavaScriptSerializer _jss = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

    // Sends the full conversation to OpenRouter and returns the assistant reply.
    // transcript and notes are injected as an initial user context turn.
    public static async Task<string> ChatAsync(
        string transcript,
        string notes,
        IList<ChatMessage> history,
        string apiKey)
    {
        // Build the messages array
        var messages = new List<object>();

        // System message
        messages.Add(new { role = "system", content = SystemPrompt });

        // Context block (transcript + notes) as first user turn, if provided
        var contextParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(transcript))
            contextParts.Add("Transcript:\n" + transcript.Trim());
        if (!string.IsNullOrWhiteSpace(notes))
            contextParts.Add("Creator notes:\n" + notes.Trim());

        if (contextParts.Count > 0) {
            messages.Add(new {
                role    = "user",
                content = "Use this context for the full conversation:\n\n" + string.Join("\n\n", contextParts)
            });
        }

        // Conversation history
        foreach (var msg in history) {
            messages.Add(new { role = msg.Role, content = msg.Content });
        }

        // Serialize request
        var requestObj = new {
            model       = Model,
            messages    = messages,
            temperature = 0.8,
            max_tokens  = 1200
        };
        string requestJson = _jss.Serialize(requestObj);

        var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl) {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", "Bearer " + apiKey);
        request.Headers.Add("HTTP-Referer", "https://github.com/mikerosoft");
        request.Headers.Add("X-Title", "mikerosoft/video-titles");

        HttpResponseMessage response;
        try {
            response = await _http.SendAsync(request).ConfigureAwait(false);
        } catch (Exception ex) {
            throw new Exception("Network error: " + ex.Message, ex);
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode) {
            // Try to surface a specific error message from the JSON body
            string apiMsg = null;
            try {
                var errObj = _jss.Deserialize<Dictionary<string, object>>(body);
                if (errObj != null && errObj.ContainsKey("error")) {
                    var errDict = errObj["error"] as Dictionary<string, object>;
                    if (errDict != null && errDict.ContainsKey("message"))
                        apiMsg = errDict["message"] as string;
                }
            } catch { /* ignore parse failures, fall through to generic error */ }

            throw new Exception(apiMsg != null
                ? "API error: " + apiMsg
                : "API error " + (int)response.StatusCode + ": " + body);
        }

        // Parse response: choices[0].message.content
        try {
            var resp = _jss.Deserialize<Dictionary<string, object>>(body);
            var choices = resp["choices"] as System.Collections.ArrayList;
            var first   = choices[0] as Dictionary<string, object>;
            var message = first["message"] as Dictionary<string, object>;
            return message["content"] as string ?? "";
        } catch (Exception ex) {
            throw new Exception("Failed to parse API response: " + ex.Message + "\nBody: " + body, ex);
        }
    }
}

}
