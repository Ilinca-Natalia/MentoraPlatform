using MentoraPlatform.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Web.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class CodeLabController : Controller
{
    // Cheia ta unică și privată obținută de pe RapidAPI pentru Judge0
    private readonly string _rapidApiKey = "111e6ca7demshbf2cfe32e1d7e42p164ecfjsnf902d1544759";

    // Pagina de selecție (Lobby)
    public ActionResult Index()
    {
        return View();
    }

    // Editorul Web (HTML/CSS/JS)
    public ActionResult WebEditor()
    {
        return View(); // Folder: Views/CodeLab/WebEditor.cshtml
    }

    // Consola SQL
    public ActionResult SqlEditor()
    {
        return View(); // Folder: Views/CodeLab/SqlEditor.cshtml
    }

    // Editorul de Algoritmi (C# / C++)
    public ActionResult AlgorithmEditor()
    {
        return View(); // Folder: Views/CodeLab/AlgorithmEditor.cshtml
    }

    [HttpPost]
    [ValidateInput(false)] // Permite transmiterea caracterelor speciale din codul sursă (<, >, &)
    public async Task<ActionResult> CompileAlgorithm(string sourceCode, int languageId)
    {
        if (string.IsNullOrWhiteSpace(sourceCode))
        {
            return Json(new { success = false, message = "Codul sursă este gol!" });
        }

        try
        {
            using (var client = new HttpClient())
            {
                // 1. Configurarea headerelor esențiale cerute de RapidAPI
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-rapidapi-key", _rapidApiKey);
                client.DefaultRequestHeaders.Add("x-rapidapi-host", "judge0-ce.p.rapidapi.com");

                var requestPayload = new
                {
                    source_code = sourceCode,
                    language_id = languageId
                };

                string jsonPayload = JsonConvert.SerializeObject(requestPayload);

                // 2. REZOLVARE UTF-8: Forțăm conținutul să NU mai trimită modificatorul "charset=utf-8"
                var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                httpContent.Headers.ContentType.Parameters.Clear(); // Șterge automat textul "; charset=utf-8" care bloca Judge0

                // 3. Executăm apelul POST către endpoint-ul corect de submissions
                var response = await client.PostAsync("https://judge0-ce.p.rapidapi.com/submissions?base64_encoded=false&wait=true", httpContent);

                if (response.IsSuccessStatusCode)
                {
                    string responseString = await response.Content.ReadAsStringAsync();
                    return Content(responseString, "application/json");
                }
                else
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    return Json(new { success = false, message = $"Eroare API Intern (Status: {response.StatusCode}) - {errorBody}" });
                }
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Eroare excepție rețea: {ex.Message}" });
        }
    }

    [HttpPost]
    public ActionResult ExecuteSql(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(new { success = false, message = "Interogarea SQL este goală!" });
        }

        try
        {
            // PROTECȚIE LICENȚĂ: Blocăm comenzile distructive pentru a preveni ștergerea bazei de date
            string upperQuery = query.Trim().ToUpper();
            if (upperQuery.StartsWith("DROP") || upperQuery.StartsWith("DELETE") || upperQuery.StartsWith("TRUNCATE") || upperQuery.StartsWith("ALTER"))
            {
                return Json(new { success = false, message = "Securitate: Comenzile de modificare sau ștergere (DROP, DELETE, TRUNCATE, ALTER) sunt dezactivate în acest laborator educațional." });
            }

            using (var context = new ApplicationDbContext())
            {
                var connection = context.Database.Connection;
                if (connection.State == ConnectionState.Closed) connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        var model = new List<dynamic>();
                        var columnNames = new List<string>();

                        for (int i = 0; i < reader.FieldCount; i++)
                            columnNames.Add(reader.GetName(i));

                        while (reader.Read())
                        {
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var colName = reader.GetName(i);
                                var val = reader[i];

                                // Convertim valoarea la string pentru a fi ușor de randat sub formă de tabel în interfață
                                row[colName] = (val == DBNull.Value) ? null : val.ToString();
                            }
                            model.Add(row);
                        }

                        return Json(new { success = true, columns = columnNames, rows = model });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}