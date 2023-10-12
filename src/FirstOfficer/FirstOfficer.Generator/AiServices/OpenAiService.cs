using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FirstOfficer.Generator.AiServices
{
    internal class OpenAiService : IAiService
    {
        public async Task<string> GetSqlFromExpression(string expression)
        {

            string responsePayLoad;
            var rootObject = new Root();
            do
            {
                var httpClient = new HttpClient();
                var payLoad = GetRequestJson(expression);
                var content = new StringContent(payLoad, Encoding.UTF8, "application/json");
                var request =
                    new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                request.Headers.Add("OpenAI-Organization", orgId);
                //request.Headers.Add("User-Agent", "FirstOfficer/0.0.1");
                request.Content = content;
                var response = await httpClient.SendAsync(request);

                responsePayLoad = await response.Content.ReadAsStringAsync();

                rootObject = JsonSerializer.Deserialize<Root>(responsePayLoad);

            } while (rootObject == null || !ValidatePayload(rootObject.Choices.First().Message.Content));

            return rootObject.Choices.First().Message.Content;

        }

        private bool ValidatePayload(string responsePayLoad)
        {
            return !responsePayLoad.Contains(@"""") && responsePayLoad.Contains("@") && responsePayLoad.Contains("WHERE") && responsePayLoad.EndsWith(";");
        }


        private static string GetRequestJson(string expression)
        {
            return $@"
                    {{
                         ""model"": ""gpt-3.5-turbo"",
                         ""messages"": [
                            {{""role"": ""system"", ""content"":""This is for a code generation application for converting LamdbaExpressions into Postgres SQL statements in C#""}}, 
                            {{""role"": ""system"", ""content"":""use ANY for arrays""}},                     
                            {{""role"": ""system"", ""content"":""Single parameter for each Value""}}, 
                            {{""role"": ""system"", ""content"":""proceed the parameter with @""}}, 
                            {{""role"": ""system"", ""content"":""use ILIKE""}}, 
                            {{""role"": ""system"", ""content"":""End statement with a semi-colon""}},      
                            {{""role"": ""system"", ""content"":""Table name is books""}},
                            {{""role"": ""system"", ""content"":""Content response in ```sql only""}},      
                            {{""role"": ""system"", ""content"":""Database columns names are in snake case.""}},
                            {{""role"": ""user"", ""content"":""books.Where({expression});""}}],
                         ""temperature"": 0.7
                    }}
                    ";

        }
        
        public class Root
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }

            [JsonPropertyName("object")]
            public string Object { get; set; }

            [JsonPropertyName("created")]
            public long Created { get; set; }

            [JsonPropertyName("model")]
            public string Model { get; set; }

            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; }

            [JsonPropertyName("usage")]
            public Usage Usage { get; set; }
        }

        public class Choice
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("message")]
            public Message Message { get; set; }

            [JsonPropertyName("finish_reason")]
            public string FinishReason { get; set; }
        }

        public class Message
        {
            [JsonPropertyName("role")]
            public string Role { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }
        }

        public class Usage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }




    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //
    //

    //








    //




































































    private string key = "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";
        private string orgId = "org-xJKGxz3TSdMxJLV8CELnKw2n";



    }
}
