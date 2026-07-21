using MentoraPlatform.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.AspNet.Identity;

namespace MentoraPlatform.Controllers
{
    [Authorize]
    public class CodeLabController : Controller
    {
        private readonly string _rapidApiKey = "your key here";

        // GET: CodeLab 
        public ActionResult Index()
        {
            return View();
        }

        // GET: CodeLab/WebEditor 
        public ActionResult WebEditor(int? id)
        {
            if (id.HasValue && id.Value > 0)
            {
                using (var context = new ApplicationDbContext())
                {
                    string currentUserId = User.Identity.GetUserId();
                    var project = context.CodeProjects.Find(id.Value);

                    if (project != null && project.UserId == currentUserId)
                    {
                        return View(project);
                    }
                }
            }

            return View(new CodeProject { Title = "Proiect Web Nou" });
        }

        // GET: CodeLab/SqlEditor 
        public ActionResult SqlEditor()
        {
            return View();
        }

        // GET: CodeLab/AlgorithmEditor 
        public ActionResult AlgorithmEditor()
        {
            return View();
        }

        // POST: CodeLab/SaveProject 
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult SaveProject(string title, string htmlCode, string cssCode, string jsCode, int? projectId)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return Json(new { success = false, message = "Vă rugăm să introduceți un titlu pentru proiect!" });
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    string currentUserId = User.Identity.GetUserId();
                    CodeProject project;

                    if (projectId.HasValue && projectId.Value > 0)
                    {
                        project = context.CodeProjects.Find(projectId.Value);
                        if (project == null || project.UserId != currentUserId)
                        {
                            return Json(new { success = false, message = "Proiectul nu a fost găsit sau nu vă aparține!" });
                        }

                        project.Title = title;
                        project.HtmlCode = htmlCode;
                        project.CssCode = cssCode;
                        project.JsCode = jsCode;
                        project.CreatedAt = DateTime.Now;
                    }
                    else
                    {
                        project = new CodeProject
                        {
                            Title = title,
                            HtmlCode = htmlCode,
                            CssCode = cssCode,
                            JsCode = jsCode,
                            UserId = currentUserId,
                            CreatedAt = DateTime.Now
                        };
                        context.CodeProjects.Add(project);
                    }

                    context.SaveChanges();
                    return Json(new { success = true, message = "Proiectul a fost stocat cu succes în baza de date locală!", id = project.Id });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Eroare baze de date: {ex.Message}" });
            }
        }

        // POST: CodeLab/CompileAlgorithm
        [HttpPost]
        [ValidateInput(false)]
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
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("x-rapidapi-key", _rapidApiKey);
                    client.DefaultRequestHeaders.Add("x-rapidapi-host", "judge0-ce.p.rapidapi.com");

                    var requestPayload = new { source_code = sourceCode, language_id = languageId };
                    string jsonPayload = JsonConvert.SerializeObject(requestPayload);

                    var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    httpContent.Headers.ContentType.Parameters.Clear();

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

        // POST: CodeLab/ExecuteSql 
        [HttpPost]
        public ActionResult ExecuteSql(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Interogarea SQL este goală!" });
            }

            try
            {
                string upperQuery = query.Trim().ToUpper();
                if (upperQuery.StartsWith("DROP") || upperQuery.StartsWith("DELETE") || upperQuery.StartsWith("TRUNCATE") || upperQuery.StartsWith("ALTER"))
                {
                    return Json(new { success = false, message = "Securitate: Comenzile distructive sunt dezactivate în sandbox-ul bazei de date." });
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
}