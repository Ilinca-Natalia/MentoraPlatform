using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MentoraPlatform.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MentoraPlatform.Services
{
    public class AIService
    {
        // Pune aici cheia ta de API pe care ai generat-o
        private readonly string _apiKey = "tu-pune-aici-cheia-ta-de-API-OpenAI";
        public async Task<QuizPreviewViewModel> GenerateQuizAsync(string lessonContent)
        {
            // 1. Curățăm textul de tag-uri HTML pentru a nu consuma tokeni inutili
            string cleanText = Regex.Replace(lessonContent, "<.*?>", string.Empty);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

                // 2. Construim cererea către OpenAI
                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[] {
                        new {
                            role = "system",
                            content = "Ești un asistent pedagogic de elită. Generezi teste grilă în format JSON. Răspunzi DOAR cu obiectul JSON, fără text suplimentar."
                        },
                        new {
                            role = "user",
                            content = $@"Bazat pe următorul text: '{cleanText}', generează un test cu FIX 10 întrebări.
                            
                            Cerințe:
                            - Fiecare întrebare să aibă 4 variante de răspuns.
                            - Doar una singură să fie corectă (IsCorrect: true).
                            - Formatul JSON trebuie să fie:
                            {{ 
                                'QuizTitle': 'Titlu test', 
                                'Questions': [ 
                                    {{ 
                                        'Text': 'Întrebarea?', 
                                        'Choices': [ 
                                            {{ 'Text': 'Varianta A', 'IsCorrect': false }},
                                            {{ 'Text': 'Varianta B', 'IsCorrect': true }}
                                        ] 
                                    }} 
                                ] 
                            }}"
                        }
                    },
                    temperature = 0.7
                };

                var jsonRequest = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseString = await response.Content.ReadAsStringAsync();
                        dynamic result = JsonConvert.DeserializeObject(responseString);
                        string rawJson = result.choices[0].message.content;

                        // Curățăm posibilele marcaje de tip Markdown (```json)
                        rawJson = Regex.Replace(rawJson, "```json|```", "").Trim();

                        // Deserializăm în modelul tău
                        return JsonConvert.DeserializeObject<QuizPreviewViewModel>(rawJson);
                    }
                }
                catch (Exception ex)
                {
                    // Log error if needed
                }

                // Dacă API-ul eșuează, returnăm datele de test (Mock) ca să nu crape aplicația la prezentare
                return await GetMockData();
            }
        }

        private async Task<QuizPreviewViewModel> GetMockData()
        {
            // Date de rezervă (Fallback)
            return new QuizPreviewViewModel
            {
                QuizTitle = "Test Generat (Mod Siguranță)",
                Questions = new List<QuestionPreview> {
                    new QuestionPreview {
                        Text = "Exemplu întrebare?",
                        Choices = new List<ChoicePreview> {
                            new ChoicePreview { Text = "Răspuns corect", IsCorrect = true },
                            new ChoicePreview { Text = "Răspuns greșit", IsCorrect = false }
                        }
                    }
                }
            };
        }
        public async Task<string> GetCourseRecommendationAsync(string userMessage, string context)
{
    using (var client = new HttpClient())
    {
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[] {
                new { 
                    role = "system", 
                    content = $@"Ești un asistent AI pentru platforma Mentora. 
                                Analizează cererea utilizatorului raportată la baza de date: {context}. 
                                Caută informația în: Titlul cursului, Descrierea cursului, Titlurile lecțiilor și Conținutul lecțiilor.
                                Răspunde DOAR cu ID-ul (cifra) cursului cel mai potrivit. Dacă nu există nicio potrivire, răspunde cu 0." 
                },
                new { role = "user", content = userMessage }
            },
            temperature = 0.2
        };

        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json"));
        var responseString = await response.Content.ReadAsStringAsync();
        dynamic result = JsonConvert.DeserializeObject(responseString);
        
        string content = result.choices[0].message.content.ToString();
        return content.Trim();
    }
}
    }
}