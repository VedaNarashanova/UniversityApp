using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UniversityApp.Models;

public class ProfessorController : Controller
{
    private readonly string connectionString =
        "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

    [HttpGet]
    public IActionResult Dashboard(int professorId)
    {
        var model = new ProfessorDashboardViewModel();
        model.Classes = new List<ProfessorClassViewModel>();
        model.ProfessorId = professorId;

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // Get classes taught by professor
            string classQuery = @"
                SELECT class_id, name
                FROM Class
                WHERE professor_id = @professorId";

            using (SqlCommand cmd = new SqlCommand(classQuery, conn))
            {
                cmd.Parameters.AddWithValue("@professorId", professorId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Classes.Add(new ProfessorClassViewModel
                        {
                            ClassId = (int)reader["class_id"],
                            ClassName = reader["name"].ToString(),
                            Students = new List<StudentSimpleViewModel>()
                        });
                    }
                }
            }

            // For each class → get students
            foreach (var cls in model.Classes)
            {
                string studentQuery = @"
                    SELECT s.student_id, s.name, s.surname, s.index_number, e.grade
                    FROM Enrollment e
                    JOIN Student s ON e.student_id = s.student_id
                    WHERE e.class_id = @classId";

                using (SqlCommand cmd = new SqlCommand(studentQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@classId", cls.ClassId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            cls.Students.Add(new StudentSimpleViewModel
                            {
                                StudentId = (int)reader["student_id"],
                                Name = reader["name"].ToString(),
                                Surname = reader["surname"].ToString(),
                                Index = reader["index_number"].ToString(),
                                Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
                            });
                        }
                    }
                }
            }
        }
        model.ProfessorId = professorId;
        return View(model);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetGrade(int studentId, int classId, int grade, int professorId)
    {
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        string query = @"
                    UPDATE Enrollment
                    SET grade = @grade
                    WHERE student_id = @studentId AND class_id = @classId";

        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@grade", grade);
        cmd.Parameters.AddWithValue("@studentId", studentId);
        cmd.Parameters.AddWithValue("@classId", classId);

        cmd.ExecuteNonQuery();

        // reload dashboard
        return RedirectToAction("Dashboard", new {professorId});
    }


    public IActionResult AddClass(int professorId)
    {
        var model = new CreateClassViewModel
        {
            ProfessorId = professorId
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddClass(CreateClassViewModel model)
    {
        if(!ModelState.IsValid)
            return View(model);

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        string query = @"INSERT INTO Class (name,semester,professor_id) VALUES (@name, @semester, @professorId)";

        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@name", model.Name);
        cmd.Parameters.AddWithValue("@semester", model.Semester);
        cmd.Parameters.AddWithValue("@professorId", model.ProfessorId);

        cmd.ExecuteNonQuery();

        return RedirectToAction("Dashboard", new {professorId = model.ProfessorId});
    }
}
