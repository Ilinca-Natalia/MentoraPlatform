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
        private readonly string _apiKey = "your key here"; // Replace with your actual OpenAI API key
        public async Task<QuizPreviewViewModel> GenerateQuizAsync(string lessonContent)
        {
            string cleanText = Regex.Replace(lessonContent, "<.*?>", string.Empty);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

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
                            - Formatul JSON trebuie să fie strict: {{ 'QuizTitle': 'Titlu', 'Questions': [ {{ 'Text': '?', 'Choices': [ {{ 'Text': '?', 'IsCorrect': bool }} ] }} ] }}"
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

                        
                        rawJson = Regex.Replace(rawJson, "```json|```", "").Trim();

                        return JsonConvert.DeserializeObject<QuizPreviewViewModel>(rawJson);
                    }
                }
                catch (Exception ex)
                {
                    
                }

                return await GetMockData();
            }
        }

        private async Task<QuizPreviewViewModel> GetMockData()
        {
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