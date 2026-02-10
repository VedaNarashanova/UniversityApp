using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using UniversityApp.Models.Professor;
using System.Data;

public class ProfessorController : Controller
{
    private readonly string connectionString =
        "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

    //[HttpGet]
    //public IActionResult Dashboard(int professorId)
    //{
    //    var model = new ProfessorDashboardViewModel();
    //    model.Classes = new List<ProfessorClassViewModel>();
    //    model.ProfessorId = professorId;

    //    using (SqlConnection conn = new SqlConnection(connectionString))
    //    {
    //        conn.Open();

    //        // Get classes taught by professor
    //        //string classQuery = @"
    //        //    SELECT class_id, name
    //        //    FROM Class
    //        //    WHERE professor_id = @professorId";



    //        using (SqlCommand cmd = new SqlCommand("sp_GetProfessorClasses", conn))
    //        {
    //            cmd.CommandType = CommandType.StoredProcedure;
    //            cmd.Parameters.AddWithValue("@ProfessorId", professorId);

    //            using (SqlDataReader reader = cmd.ExecuteReader())
    //            {
    //                while (reader.Read())
    //                {
    //                    model.Classes.Add(new ProfessorClassViewModel
    //                    {
    //                        ClassId = (int)reader["class_id"],
    //                        ClassName = reader["name"].ToString(),
    //                        Students = new List<StudentSimpleViewModel>()
    //                    });
    //                }
    //            }
    //        }

    //        // For each class → get students
    //        foreach (var cls in model.Classes)
    //        {
    //            string studentQuery = @"
    //                SELECT s.student_id, s.name, s.surname, s.index_number, e.grade
    //                FROM Enrollment e
    //                JOIN Student s ON e.student_id = s.student_id
    //                WHERE e.class_id = @classId";

    //            using (SqlCommand cmd = new SqlCommand(studentQuery, conn))
    //            {
    //                cmd.Parameters.AddWithValue("@classId", cls.ClassId);
    //                using (SqlDataReader reader = cmd.ExecuteReader())
    //                {
    //                    while (reader.Read())
    //                    {
    //                        cls.Students.Add(new StudentSimpleViewModel
    //                        {
    //                            StudentId = (int)reader["student_id"],
    //                            Name = reader["name"].ToString(),
    //                            Surname = reader["surname"].ToString(),
    //                            Index = reader["index_number"].ToString(),
    //                            Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
    //                        });
    //                    }
    //                }
    //            }
    //            // --- Class statistics ---
    //            var grades = cls.Students.Where(s => s.Grade.HasValue).Select(s => s.Grade.Value).ToList();
    //            cls.Stats = new ClassStatistics
    //            {
    //                TotalStudents = cls.Students.Count,
    //                GradedStudents = grades.Count,
    //                AverageGrade = grades.Count > 0 ? grades.Average() : (double?)null,
    //                MedianGrade = grades.Count > 0 ? CalculateMedian(grades) : (double?)null,
    //                ModeGrade = grades.Count > 0 ? CalculateMode(grades) : (int?)null
    //            };
    //        }
    //    }
    //    model.ProfessorId = professorId;
    //    return View(model);
    //}
    [HttpGet]
    public IActionResult Dashboard(int professorId)
    {
        var model = new ProfessorDashboardViewModel
        {
            Classes = new List<ProfessorClassViewModel>(),
            ProfessorId = professorId
        };

        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            // 1️⃣ Get classes taught by professor
            using (SqlCommand cmd = new SqlCommand("dbo.sp_GetProfessorClasses", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ProfessorId", professorId);

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

            // 2️⃣ For each class → get students
            foreach (var cls in model.Classes)
            {
                using (SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentsByClass", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ClassId", cls.ClassId);

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

                // 3️⃣ Class statistics (C# logic)
                var grades = cls.Students
                    .Where(s => s.Grade.HasValue)
                    .Select(s => s.Grade.Value)
                    .ToList();

                cls.Stats = new ClassStatistics
                {
                    TotalStudents = cls.Students.Count,
                    GradedStudents = grades.Count,
                    AverageGrade = grades.Count > 0 ? grades.Average() : (double?)null,
                    MedianGrade = grades.Count > 0 ? CalculateMedian(grades) : (double?)null,
                    ModeGrade = grades.Count > 0 ? CalculateMode(grades) : (int?)null
                };
            }
        }

        return View(model);
    }



    // --- Helper methods ---
    private double CalculateMedian(List<int> numbers)
{
    numbers.Sort();
    int n = numbers.Count;
    if (n % 2 == 1) return numbers[n / 2];
    return (numbers[(n - 1) / 2] + numbers[n / 2]) / 2.0;
}

private int? CalculateMode(List<int> numbers)
{
    var groups = numbers.GroupBy(x => x)
                        .OrderByDescending(g => g.Count())
                        .ThenBy(g => g.Key)
                        .ToList();
    if (groups.Count == 0) return null;
    return groups[0].Key;
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

        string query = @"
                    INSERT INTO Class (name,semester,professor_id)
                    VALUES (@name, @semester, @professorId)";

        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@name", model.Name);
        cmd.Parameters.AddWithValue("@semester", model.Semester);
        cmd.Parameters.AddWithValue("@professorId", model.ProfessorId);

        cmd.ExecuteNonQuery();

        return RedirectToAction("Dashboard", new {professorId = model.ProfessorId});
    }

    //___________________________________________________________________________________________________________

    [HttpGet]
    public IActionResult AddStudents(int professorId)
    {
        var model = new AddStudentToClassViewModel
        {
            ProfessorId = professorId
        };
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        string query = @"
                    SELECT class_id, name
                    FROM Class
                    WHERE professor_id = @professorId";

        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@professorId", professorId);

        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            model.Classes.Add(new ProfessorClassViewModel
            {
                ClassId = (int)reader["class_id"],
                ClassName = reader["name"].ToString()
            });
        }

        return View("AddStudents", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LoadAvailableClasses(AddStudentToClassViewModel model)
    {
        //return Content("HIT LOADAVAILABLECLASSES");
        if (string.IsNullOrEmpty(model.StudentIndex))
        {
            return Content("StudentIndex is EMPTY");
        }
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        // 1️⃣ Get student ID
        int studentId;
        string studentQuery = @"
        SELECT student_id
        FROM Student
        WHERE index_number = @index";

        using (SqlCommand cmd = new SqlCommand(studentQuery, conn))
        {
            cmd.Parameters.AddWithValue("@index", model.StudentIndex);
            var result = cmd.ExecuteScalar();

            if (result == null)
            {
                ModelState.AddModelError("", "Student not found.");
                return View("AddStudents", model);
            }

            studentId = (int)result;
        }

        // 2️⃣ Get professor classes student is NOT enrolled in
        string classQuery = @"
        SELECT c.class_id, c.name
        FROM Class c
        WHERE c.professor_id = @professorId
        AND c.class_id NOT IN (
            SELECT e.class_id
            FROM Enrollment e
            WHERE e.student_id = @studentId
        )";

        using SqlCommand classCmd = new SqlCommand(classQuery, conn);
        classCmd.Parameters.AddWithValue("@professorId", model.ProfessorId);
        classCmd.Parameters.AddWithValue("@studentId", studentId);

        using SqlDataReader reader = classCmd.ExecuteReader();
        while (reader.Read())
        {
            model.Classes.Add(new ProfessorClassViewModel
            {
                ClassId = (int)reader["class_id"],
                ClassName = reader["name"].ToString()
            });
        }

        model.ClassesLoaded = true;
        return View("AddStudents", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddStudents(AddStudentToClassViewModel model)
    {
        if (model.SelectedClassIds == null || !model.SelectedClassIds.Any())
        {
            ModelState.AddModelError("", "No classes were selected.");
        }

        if (!ModelState.IsValid)
        {
            // 🔁 reload available classes again
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            int studentId;
            string studentQuery = "SELECT student_id FROM Student WHERE index_number = @index";
            using (SqlCommand cmd = new SqlCommand(studentQuery, conn))
            {
                cmd.Parameters.AddWithValue("@index", model.StudentIndex);
                studentId = (int)cmd.ExecuteScalar();
            }

            string classQuery = @"
            SELECT c.class_id, c.name
            FROM Class c
            WHERE c.professor_id = @professorId
            AND c.class_id NOT IN (
                SELECT e.class_id
                FROM Enrollment e
                WHERE e.student_id = @studentId
            )";

            using SqlCommand classCmd = new SqlCommand(classQuery, conn);
            classCmd.Parameters.AddWithValue("@professorId", model.ProfessorId);
            classCmd.Parameters.AddWithValue("@studentId", studentId);

            using SqlDataReader reader = classCmd.ExecuteReader();
            while (reader.Read())
            {
                model.Classes.Add(new ProfessorClassViewModel
                {
                    ClassId = (int)reader["class_id"],
                    ClassName = reader["name"].ToString()
                });
            }

            model.ClassesLoaded = true;
            return View("AddStudents", model);
        }

        // ✅ VALID → insert into Enrollment
        using (SqlConnection conn = new SqlConnection(connectionString))
        {
            conn.Open();

            int studentId;
            string studentQuery = "SELECT student_id FROM Student WHERE index_number = @index";
            using (SqlCommand cmd = new SqlCommand(studentQuery, conn))
            {
                cmd.Parameters.AddWithValue("@index", model.StudentIndex);
                studentId = (int)cmd.ExecuteScalar();
            }

            foreach (int classId in model.SelectedClassIds)
            {
                string insertQuery = @"
                IF NOT EXISTS (
                    SELECT 1 FROM Enrollment
                    WHERE student_id = @studentId AND class_id = @classId
                )
                INSERT INTO Enrollment (student_id, class_id)
                VALUES (@studentId, @classId)";

                using SqlCommand cmd = new SqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@studentId", studentId);
                cmd.Parameters.AddWithValue("@classId", classId);
                cmd.ExecuteNonQuery();
            }
        }

        // ✅ REDIRECT (this part was already correct)
        return RedirectToAction("Dashboard", new { professorId = model.ProfessorId });
    }
    //_________________________________________________________________________________________________________

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveStudent(int studentId, int classId, int professorId)
    {
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        string query = @"
        DELETE FROM Enrollment
        WHERE student_id = @studentId AND class_id = @classId";

        using SqlCommand cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@studentId", studentId);
        cmd.Parameters.AddWithValue("@classId", classId);

        cmd.ExecuteNonQuery();

        // redirect back to dashboard
        return RedirectToAction("Dashboard", new { professorId });
    }

}
