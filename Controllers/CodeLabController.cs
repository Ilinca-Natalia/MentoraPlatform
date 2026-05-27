using MentoraPlatform.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Web.Mvc;

public class CodeLabController : Controller
{
    // Pagina de selecție (Lobby)
    public ActionResult Index()
    {
        return View();
    }

    // Editorul Web (cel pe care l-am făcut deja)
    public ActionResult WebEditor()
    {
        return View(); // Folder: Views/CodeLab/WebEditor.cshtml
    }

    // Consola SQL
    public ActionResult SqlEditor()
    {
        return View(); // Folder: Views/CodeLab/SqlEditor.cshtml
    }

    // Editorul de Algoritmi
    public ActionResult AlgorithmEditor()
    {
        return View(); // Folder: Views/CodeLab/AlgorithmEditor.cshtml
    }

    [HttpPost]
    public ActionResult ExecuteSql(string query)
    {
        try
        {
            // ATENȚIE: În mod normal, aici ar trebui filtrate comenzile de tip DELETE/DROP 
            // pentru a proteja baza de date a licenței.

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
                            // Folosim Dictionary în loc de ExpandoObject pentru control total
                            var row = new Dictionary<string, object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var colName = reader.GetName(i);
                                var val = reader[i];

                                // Esențial: Convertim valoarea la string dacă nu e null
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