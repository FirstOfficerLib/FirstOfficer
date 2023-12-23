using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Configuration;

namespace FirstOfficer.Generator.Services
{
    public class OpenAiService
    {

        private readonly string _key;
        private readonly string _orgId;

        public OpenAiService(IConfiguration configuration)
        {
            _key = configuration["FirstOfficer:OpenAi:Key"] ?? string.Empty;
            _orgId = configuration["FirstOfficer:OpenAi:OrgId"] ?? string.Empty;
        }

        public async Task<string> GetSqlFromExpression(string expression, INamedTypeSymbol symbol)
        {

            var nullable = GetNullableList(expression, symbol);

            expression = HandleNullable(expression, nullable);

            expression = CleanExpression(expression);

            Root? rootObject;
            do
            {
                var httpClient = new HttpClient();
                var payLoad = GetRequestJson(expression, symbol.Name);
                var content = new StringContent(payLoad, Encoding.UTF8, "application/json");
                var request =
                    new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _key);
                request.Headers.Add("OpenAI-Organization", _orgId);
                request.Headers.Add("User-Agent", "FirstOfficer/0.0.0.1");
                request.Content = content;
                var response = await httpClient.SendAsync(request);

                var responsePayLoad = await response.Content.ReadAsStringAsync();

                rootObject = JsonSerializer.Deserialize<Root>(responsePayLoad);

            } while (rootObject == null || !ValidatePayload(rootObject.Choices.First().Message.Content, expression, nullable));

            return rootObject.Choices.First().Message.Content;

        }

        private string HandleNullable(string expression, (string, string)[] nullable)
        {

            foreach (var (item1, item2) in nullable)
            {
             
                var count = 0;
                while (!expression.Contains($"{item1} == {item2}"))
                {
                    expression = CleanExpression(expression.Replace("=", " = ")).Replace("= =", "==");

                    count++;
                    if (count > 10)
                    {
                        throw new Exception("Unable to generate SQL");
                    }
                }

                expression = expression.Replace($"{item1} == {item2}",
                    $"({item1} == {item2} || ({item1} IS NULL && {item2} IS NULL))");
            }

            return expression;
        }

        private string CleanExpression(string expression)
        {
            while (expression.Contains("  "))
            {
                expression = expression.Replace("  ", " ");
            }
            return expression;
        }

        private bool ValidatePayload(string responsePayLoad, string expression, (string, string)[] nullable)
        {
            var isNullCount = Regex.Matches(responsePayLoad.ToUpper(), Regex.Escape("IS NULL")).Count;
            var iLikeCount = Regex.Matches(responsePayLoad.ToUpper(), Regex.Escape("ILIKE")).Count;
            var containsCount = Regex.Matches(expression.ToUpper(), Regex.Escape("CONTAINS")).Count;
            var percentCount = Regex.Matches(responsePayLoad.ToUpper(), Regex.Escape("%")).Count;

            return (nullable.Length * 2) == isNullCount && 
                   iLikeCount == containsCount &&
                   (iLikeCount * 2) == percentCount &&
                   !responsePayLoad.Contains(@"""") && 
                   responsePayLoad.Contains("the_table.") && 
                   responsePayLoad.Contains("@") && 
                   responsePayLoad.Contains("WHERE") && 
                   responsePayLoad.Contains(";") && 
                   !responsePayLoad.ToLower().Contains("parameter_value");
        }

        private (string, string)[] GetNullableList(string expression, INamedTypeSymbol symbol)
        {
            var rtn = new List<(string, string)>();

            var properties = symbol.GetMembers().OfType<IPropertySymbol>().ToArray();
            foreach (var property in properties)
            {
                if (expression.Contains($".{property.Name}"))
                {
                    if (!property.Type.IsReadOnly && property.NullableAnnotation == NullableAnnotation.Annotated)
                    {
                        var index = expression.IndexOf($".{property.Name}", StringComparison.Ordinal);
                        var varName = expression.Substring(0, index).Split('.').First().Split(' ').Last();

                        //get parameter name
                        var endSubstring = expression.Substring(index + 1);
                        var parameterName = endSubstring.Split('.')[1].Split(' ').First();
                        rtn.Add(($"{varName}.{property.Name}", $"Parameter.{parameterName}"));
                    }
                }
            }

            return rtn.ToArray();
        }


        private static string GetRequestJson(string expression, string symbolName)
        {            
            return $@"
                    {{
                         ""model"": ""gpt-3.5-turbo-1106"",
                         ""messages"": [
                            {{""role"": ""system"", ""content"":""This is for a code generation application for converting LamdbaExpressions into Postgres SQL statements in C#""}}, 
                            {{""role"": ""system"", ""content"":""use ANY for arrays""}},                                                 
                            {{""role"": ""system"", ""content"":""Single parameter for each Value""}}, 
                            {{""role"": ""system"", ""content"":""Table name is the_table""}},                               
                            {{""role"": ""system"", ""content"":""add the_table. to the column names""}}, 
                            {{""role"": ""system"", ""content"":""use the fully qualified column names""}}, 
                            {{""role"": ""system"", ""content"":""precede the parameter with @""}},                             
                            {{""role"": ""system"", ""content"":""use ILIKE""}}, 
                            {{""role"": ""system"", ""content"":""Do Not use joins""}}, 
                            {{""role"": ""system"", ""content"":""End statement with a semi-colon""}},                                
                            {{""role"": ""system"", ""content"":""Content response in ```sql only""}},      
                            {{""role"": ""system"", ""content"":""Database columns names are in snake case.""}},                               
                            {{""role"": ""user"", ""content"":""{symbolName}.Where({expression});""}}],
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


    }
}
