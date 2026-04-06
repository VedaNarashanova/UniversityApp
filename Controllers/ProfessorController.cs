
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using UniversityApp.Models.Professor;
//using System.Data;

//public class ProfessorController : Controller
//{
//    //private readonly string connectionString =
//    //    "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

//    private readonly string connectionString;

//    public ProfessorController(IConfiguration configuration)
//    {
//        //connectionString = configuration.GetConnectionString("UniversityDB");
//        connectionString = configuration.GetConnectionString("UniversityDB")
//                       ?? throw new Exception("Connection string 'UniversityDB' not found!");
//    }

//    //GET:  DASHBOARD
//    [HttpGet]
//    public IActionResult Dashboard(int professorId)
//    {
//        var model = new ProfessorDashboardViewModel
//        {
//            ProfessorId = professorId,
//            Classes = new List<ProfessorClassViewModel>()
//        };

//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        // Get professor classes
//        using (SqlCommand cmd = new SqlCommand("dbo.sp_GetProfessorClasses", conn))
//        {
//            cmd.CommandType = CommandType.StoredProcedure;
//            cmd.Parameters.AddWithValue("@ProfessorId", professorId);

//            using SqlDataReader reader = cmd.ExecuteReader();
//            while (reader.Read())
//            {
//                model.Classes.Add(new ProfessorClassViewModel
//                {
//                    ClassId = (int)reader["class_id"],
//                    ClassName = reader["name"].ToString(),
//                    Students = new List<StudentSimpleViewModel>()
//                });
//            }
//        }

//        // Get students per class
//        foreach (var cls in model.Classes)
//        {
//            using SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentsByClass", conn);
//            cmd.CommandType = CommandType.StoredProcedure;
//            cmd.Parameters.AddWithValue("@ClassId", cls.ClassId);

//            using SqlDataReader reader = cmd.ExecuteReader();
//            while (reader.Read())
//            {
//                //if we want to filter only the students with grade above 8 
//                // if (gradeValue.HasValue && gradeValue.Value > 8){}
//                cls.Students.Add(new StudentSimpleViewModel
//                {
//                    StudentId = (int)reader["student_id"],
//                    Name = reader["name"].ToString(),
//                    Surname = reader["surname"].ToString(),
//                    Index = reader["index_number"].ToString(),
//                    Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
//                });
//            }

//            var grades = cls.Students
//                .Where(s => s.Grade.HasValue)
//                .Select(s => s.Grade.Value)
//                .ToList();

//            cls.Stats = new ClassStatistics
//            {
//                TotalStudents = cls.Students.Count,
//                GradedStudents = grades.Count,
//                AverageGrade = grades.Count > 0 ? grades.Average() : (double?)null,
//                MedianGrade = grades.Count > 0 ? CalculateMedian(grades) : (double?)null,
//                ModeGrade = grades.Count > 0 ? CalculateMode(grades) : (int?)null
//            };
//        }

//        return View(model);
//    }

//    // POST: SET GRADE
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public IActionResult SetGrade(int studentId, int classId, int grade, int professorId)
//    {
//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        using SqlCommand cmd = new SqlCommand("dbo.sp_SetGrade", conn);
//        cmd.CommandType = CommandType.StoredProcedure;
//        cmd.Parameters.AddWithValue("@StudentId", studentId);
//        cmd.Parameters.AddWithValue("@ClassId", classId);
//        cmd.Parameters.AddWithValue("@Grade", grade);

//        cmd.ExecuteNonQuery();

//        return RedirectToAction("Dashboard", new { professorId });
//    }

//    // GET:ADD CLASS
//    [HttpGet]
//    public IActionResult AddClass(int professorId)
//    {
//        return View(new CreateClassViewModel { ProfessorId = professorId });
//    }
//    //POST:ADD CLASS
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public IActionResult AddClass(CreateClassViewModel model)
//    {
//        if (!ModelState.IsValid)
//            return View(model);

//        try
//        {
//            using SqlConnection conn = new SqlConnection(connectionString);
//            conn.Open();

//            using SqlCommand cmd = new SqlCommand("dbo.sp_AddClass", conn);
//            cmd.CommandType = CommandType.StoredProcedure;
//            cmd.Parameters.AddWithValue("@Name", model.Name);
//            cmd.Parameters.AddWithValue("@Semester", model.Semester);
//            cmd.Parameters.AddWithValue("@ProfessorId", model.ProfessorId);

//            cmd.ExecuteNonQuery();

//            return RedirectToAction("Dashboard", new { professorId = model.ProfessorId });
//        }
//        catch (SqlException ex)
//        {
//            // Check for unique constraint violation
//            if (ex.Number == 2627 || ex.Number == 2601) // 2627 = UNIQUE constraint, 2601 = duplicate index
//            {
//                ModelState.AddModelError("", $"A class with the name '{model.Name}' already exists.");
//                return View(model);
//            }

//            // Other SQL errors
//            ModelState.AddModelError("", "Database error: " + ex.Message);
//            return View(model);
//        }
//        catch (Exception ex)
//        {
//            ModelState.AddModelError("", "Error: " + ex.Message);
//            return View(model);
//        }
//    }

//    //GET: ADD STUDENTS
//    [HttpGet]
//    public IActionResult AddStudents(int professorId)
//    {
//        var model = new AddStudentToClassViewModel
//        {
//            ProfessorId = professorId
//        };

//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        using SqlCommand cmd = new SqlCommand("dbo.sp_GetProfessorClassesSimple", conn);
//        cmd.CommandType = CommandType.StoredProcedure;
//        cmd.Parameters.AddWithValue("@ProfessorId", professorId);

//        using SqlDataReader reader = cmd.ExecuteReader();
//        while (reader.Read())
//        {
//            model.Classes.Add(new ProfessorClassViewModel
//            {
//                ClassId = (int)reader["class_id"],
//                ClassName = reader["name"].ToString()
//            });
//        }

//        return View(model);
//    }

//    //POST: Load Classes
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public IActionResult LoadAvailableClasses(AddStudentToClassViewModel model)
//    {
//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        int studentId;
//        using (SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentIdByIndex", conn))
//        {
//            cmd.CommandType = CommandType.StoredProcedure;
//            cmd.Parameters.AddWithValue("@IndexNumber", model.StudentIndex);

//            var result = cmd.ExecuteScalar();
//            if (result == null)
//            {
//                ModelState.AddModelError("", "Student not found.");
//                return View("AddStudents", model);
//            }

//            studentId = (int)result;
//        }

//        using SqlCommand classCmd = new SqlCommand("dbo.sp_GetAvailableClassesForStudent", conn);
//        classCmd.CommandType = CommandType.StoredProcedure;
//        classCmd.Parameters.AddWithValue("@ProfessorId", model.ProfessorId);
//        classCmd.Parameters.AddWithValue("@StudentId", studentId);

//        using SqlDataReader reader = classCmd.ExecuteReader();
//        while (reader.Read())
//        {
//            model.Classes.Add(new ProfessorClassViewModel
//            {
//                ClassId = (int)reader["class_id"],
//                ClassName = reader["name"].ToString()
//            });
//        }

//        model.ClassesLoaded = true;
//        return View("AddStudents", model);
//    }

//    //POST: ADD STUDENT
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public IActionResult AddStudents(AddStudentToClassViewModel model)
//    {
//        if (model.SelectedClassIds == null || !model.SelectedClassIds.Any())
//            return RedirectToAction("AddStudents", new { professorId = model.ProfessorId });

//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        int studentId;
//        using (SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentIdByIndex", conn))
//        {
//            cmd.CommandType = CommandType.StoredProcedure;
//            cmd.Parameters.AddWithValue("@IndexNumber", model.StudentIndex);
//            studentId = (int)cmd.ExecuteScalar();
//        }

//        foreach (int classId in model.SelectedClassIds)
//        {
//            using SqlCommand cmd = new SqlCommand("dbo.sp_AddStudentToClass", conn);
//            cmd.CommandType = CommandType.StoredProcedure;
//            cmd.Parameters.AddWithValue("@StudentId", studentId);
//            cmd.Parameters.AddWithValue("@ClassId", classId);
//            cmd.ExecuteNonQuery();
//        }

//        return RedirectToAction("Dashboard", new { professorId = model.ProfessorId });
//    }

//    //POST: REMOVE STUDENT
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public IActionResult RemoveStudent(int studentId, int classId, int professorId)
//    {
//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        using SqlCommand cmd = new SqlCommand("dbo.sp_RemoveStudentFromClass", conn);
//        cmd.CommandType = CommandType.StoredProcedure;
//        cmd.Parameters.AddWithValue("@StudentId", studentId);
//        cmd.Parameters.AddWithValue("@ClassId", classId);

//        cmd.ExecuteNonQuery();

//        return RedirectToAction("Dashboard", new { professorId });
//    }

//    //Calculations for statistics
//    private double CalculateMedian(List<int> numbers)
//    {
//        numbers.Sort();
//        int n = numbers.Count;
//        return n % 2 == 1
//            ? numbers[n / 2]
//            : (numbers[n / 2 - 1] + numbers[n / 2]) / 2.0;
//    }

//    private int? CalculateMode(List<int> numbers)
//    {
//        return numbers
//            .GroupBy(x => x)
//            .OrderByDescending(g => g.Count())
//            .ThenBy(g => g.Key)
//            .FirstOrDefault()?.Key;
//    }


//    // GET: EDIT CLASS
//    [HttpGet]
//    public IActionResult EditClass(int classId)
//    {
//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        using SqlCommand cmd = new SqlCommand("SELECT class_id, name, semester, professor_id FROM Class WHERE class_id = @ClassId", conn);
//        cmd.Parameters.AddWithValue("@ClassId", classId);

//        using SqlDataReader reader = cmd.ExecuteReader();
//        if (reader.Read())
//        {
//            var model = new CreateClassViewModel
//            {
//                ClassId = classId,
//                ProfessorId = (int)reader["professor_id"],
//                Name = reader["name"].ToString(),
//                Semester = Convert.ToInt32(reader["semester"])
//            };
//            TempData["ClassId"] = classId; // pass ID to POST
//            return View(model);
//        }

//        return RedirectToAction("Dashboard");
//    }

//    //POST: EDIT CLASS
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public IActionResult EditClass(CreateClassViewModel model)
//    {
//        if (!ModelState.IsValid)
//            return View(model);

//        int classId = (int)TempData["ClassId"];

//        try
//        {
//            using SqlConnection conn = new SqlConnection(connectionString);
//            conn.Open();

//            using SqlCommand cmd = new SqlCommand("UPDATE Class SET name=@Name, semester=@Semester WHERE class_id=@ClassId", conn);
//            cmd.Parameters.AddWithValue("@Name", model.Name);
//            cmd.Parameters.AddWithValue("@Semester", model.Semester);
//            cmd.Parameters.AddWithValue("@ClassId", classId);

//            cmd.ExecuteNonQuery();

//            TempData["SuccessMessage"] = $"Class '{model.Name}' updated successfully!";
//            return RedirectToAction("Dashboard", new { professorId = model.ProfessorId });
//        }
//        catch (SqlException ex) when (ex.Number == 2627)
//        {
//            ModelState.AddModelError("Name", "A class with this name already exists.");
//            return View(model);
//        }
//    }

//    // POST: DELETE CLASS
//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public IActionResult DeleteClass(int classId, int professorId)
//    {
//        using SqlConnection conn = new SqlConnection(connectionString);
//        conn.Open();

//        using SqlCommand cmd = new SqlCommand("DELETE FROM Class WHERE class_id=@ClassId", conn);
//        cmd.Parameters.AddWithValue("@ClassId", classId);

//        cmd.ExecuteNonQuery();

//        TempData["SuccessMessage"] = "Class deleted successfully!";
//        return RedirectToAction("Dashboard", new { professorId });
//    }

//}


using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using UniversityApp.Models.Professor;

public class ProfessorController : Controller
{
    private readonly string connectionString;

    public ProfessorController(IConfiguration configuration)
    {
        connectionString = configuration.GetConnectionString("UniversityDB")
                       ?? throw new Exception("Connection string 'UniversityDB' not found!");
    }

    // DASHBOARD
    [HttpGet]
    public IActionResult Dashboard(int professorId)
    {
        var model = new ProfessorDashboardViewModel
        {
            ProfessorId = professorId,
            Classes = new List<ProfessorClassViewModel>()
        };

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        // Get professor classes (simplified)
        using (SqlCommand cmd = new SqlCommand("dbo.sp_GetProfessorClassesSimple", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ProfessorId", professorId);

            using SqlDataReader reader = cmd.ExecuteReader();
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

        // Get students per class
        foreach (var cls in model.Classes)
        {
            using SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentsByClass", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ClassId", cls.ClassId);

            using SqlDataReader reader = cmd.ExecuteReader();
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

        return View(model);
    }

    // POST: SET GRADE
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetGrade(int studentId, int classId, int grade, int professorId)
    {
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        using SqlCommand cmd = new SqlCommand("dbo.sp_SetGrade", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@ClassId", classId);
        cmd.Parameters.AddWithValue("@Grade", grade);

        cmd.ExecuteNonQuery();

        return RedirectToAction("Dashboard", new { professorId });
    }

    // GET: ADD CLASS
    [HttpGet]
    public IActionResult AddClass(int professorId)
    {
        return View(new CreateClassViewModel { ProfessorId = professorId });
    }

    // POST: ADD CLASS
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddClass(CreateClassViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand("dbo.sp_AddClass", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Name", model.Name);
            cmd.Parameters.AddWithValue("@Semester", model.Semester);
            cmd.Parameters.AddWithValue("@ProfessorId", model.ProfessorId);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Dashboard", new { professorId = model.ProfessorId });
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            ModelState.AddModelError("", $"A class with the name '{model.Name}' already exists.");
            return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error: " + ex.Message);
            return View(model);
        }
    }   

    // POST: DELETE CLASS
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteClass(int classId, int professorId)
    {
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        // Call procedure that deletes enrollments first
        using SqlCommand cmd = new SqlCommand("dbo.sp_DeleteClassAndEnrollments", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ClassId", classId);

        cmd.ExecuteNonQuery();

        TempData["SuccessMessage"] = "Class deleted successfully!";
        return RedirectToAction("Dashboard", new { professorId });
    }

 
    //GET: ADD STUDENTS
    [HttpGet]
    public IActionResult AddStudents(int professorId)
    {
        var model = new AddStudentToClassViewModel
        {
            ProfessorId = professorId
        };

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        using SqlCommand cmd = new SqlCommand("dbo.sp_GetProfessorClassesSimple", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ProfessorId", professorId);

        using SqlDataReader reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            model.Classes.Add(new ProfessorClassViewModel
            {
                ClassId = (int)reader["class_id"],
                ClassName = reader["name"].ToString()
            });
        }

        return View(model);
    }

    //POST: Load Classes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult LoadAvailableClasses(AddStudentToClassViewModel model)
    {
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        int studentId;
        using (SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentIdByIndex", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IndexNumber", model.StudentIndex);

            var result = cmd.ExecuteScalar();
            if (result == null)
            {
                ModelState.AddModelError("", "Student not found.");
                return View("AddStudents", model);
            }

            studentId = (int)result;
        }

        using SqlCommand classCmd = new SqlCommand("dbo.sp_GetAvailableClassesForStudent", conn);
        classCmd.CommandType = CommandType.StoredProcedure;
        classCmd.Parameters.AddWithValue("@ProfessorId", model.ProfessorId);
        classCmd.Parameters.AddWithValue("@StudentId", studentId);

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

    //POST: ADD STUDENT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AddStudents(AddStudentToClassViewModel model)
    {
        if (model.SelectedClassIds == null || !model.SelectedClassIds.Any())
            return RedirectToAction("AddStudents", new { professorId = model.ProfessorId });

        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        int studentId;
        using (SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentIdByIndex", conn))
        {
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@IndexNumber", model.StudentIndex);
            studentId = (int)cmd.ExecuteScalar();
        }

        foreach (int classId in model.SelectedClassIds)
        {
            using SqlCommand cmd = new SqlCommand("dbo.sp_AddStudentToClass", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StudentId", studentId);
            cmd.Parameters.AddWithValue("@ClassId", classId);
            cmd.ExecuteNonQuery();
        }

        return RedirectToAction("Dashboard", new { professorId = model.ProfessorId });
    }

    // POST: REMOVE STUDENT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveStudent(int studentId, int classId, int professorId)
    {
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        using SqlCommand cmd = new SqlCommand("dbo.sp_RemoveStudentFromClass", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@StudentId", studentId);
        cmd.Parameters.AddWithValue("@ClassId", classId);

        cmd.ExecuteNonQuery();

        return RedirectToAction("Dashboard", new { professorId });
    }

    // HELPER calculations
    private double CalculateMedian(List<int> numbers)
    {
        numbers.Sort();
        int n = numbers.Count;
        return n % 2 == 1
            ? numbers[n / 2]
            : (numbers[n / 2 - 1] + numbers[n / 2]) / 2.0;
    }

    private int? CalculateMode(List<int> numbers)
    {
        return numbers
            .GroupBy(x => x)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .FirstOrDefault()?.Key;
    }


    //GET: EDIT CLASS
    [HttpGet]
    public IActionResult EditClass(int classId)
    {
        Debug.WriteLine("EditClass called, classId=" + classId);
        Trace.WriteLine("Debugging trace here");
        using SqlConnection conn = new SqlConnection(connectionString);
        conn.Open();

        // Use stored procedure to get class by ID
        using SqlCommand cmd = new SqlCommand("dbo.sp_GetClassById", conn);
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.Parameters.AddWithValue("@ClassId", classId);


        //Console.WriteLine(model.ClassId);
        using SqlDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            // Prefill model for EditClass view
            var model = new CreateClassViewModel
            {
                ClassId = (int)reader["class_id"],
                ProfessorId = (int)reader["professor_id"],
                Name = reader["name"].ToString(),
                Semester = Convert.ToInt32(reader["semester"])
            };
            System.Diagnostics.Debug.WriteLine($"Model fetched: Name={model.Name}, Semester={model.Semester}, ProfessorId={model.ProfessorId}");
            ModelState.Clear();
            return View(model); // passes model to the view
        }

        TempData["ErrorMessage"] = "Class not found.";
        //return RedirectToAction("Dashboard"); // fallback
        return RedirectToAction("Dashboard", new { professorId = 0 });
    }

    // POST: EDIT CLASS
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EditClass(CreateClassViewModel model)
    {
        // Debugging: log posted values
        System.Diagnostics.Debug.WriteLine($"POST EditClass called: ClassId={model.ClassId}, Name={model.Name}, Semester={model.Semester}, ProfessorId={model.ProfessorId}");

        if (!ModelState.IsValid)
        {
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                System.Diagnostics.Debug.WriteLine("ModelState error: " + error.ErrorMessage);
            }

            // Return the same view with the model so user can correct it
            return View(model);
        }

        try
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand("dbo.sp_UpdateClass", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@ClassId", model.ClassId);
            cmd.Parameters.AddWithValue("@Name", model.Name);
            cmd.Parameters.AddWithValue("@Semester", model.Semester);

            cmd.ExecuteNonQuery();

            TempData["SuccessMessage"] = $"Class '{model.Name}' updated successfully!";

            // Redirect to Dashboard after successful update
            return RedirectToAction("Dashboard", new { professorId = model.ProfessorId });
        }
        catch (SqlException ex) when (ex.Number == 2627)
        {
            ModelState.AddModelError("Name", "A class with this name already exists.");
            return View(model);
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Error: " + ex.Message);
            return View(model);
        }
    }
}