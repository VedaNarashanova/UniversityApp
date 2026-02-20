
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.Data.SqlClient;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UniversityApp.Models;

//namespace UniversityApp.Controllers
//{
//    public class StudentController : Controller
//    {
//        private readonly string connectionString =
//            "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

//        //--------------------------------------------------------------------------------------------------------------------------------------------------
//        // GET Dashboard - Show Student dashboard with classes, grades, personal info, and statistics
//        [HttpGet]
//        public IActionResult Dashboard(int studentId, string activeTab = "classes")
//        {
//            var model = new StudentDashboardViewModel
//            {
//                StudentId = studentId,
//                ClassesAndGrades = new List<StudentClassGrade>(),
//                AvailableClasses = new List<StudentDashboardViewModel.AvailableClass>(),
//                GradeStats = new StudentDashboardViewModel.StudentGradeStats()
//            };

//            using (SqlConnection conn = new SqlConnection(connectionString))
//            {
//                conn.Open();

//                // --- Enrolled classes & grades ---
//                string query = @"
//                    SELECT c.class_id, c.name AS ClassName, e.grade
//                    FROM Enrollment e
//                    JOIN Class c ON e.class_id = c.class_id
//                    WHERE e.student_id = @studentId";

//                using (SqlCommand cmd = new SqlCommand(query, conn))
//                {
//                    cmd.Parameters.AddWithValue("@studentId", studentId);
//                    using (SqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            model.ClassesAndGrades.Add(new StudentClassGrade
//                            {
//                                ClassName = reader["ClassName"].ToString(),
//                                Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
//                            });
//                        }
//                    }
//                }

//                // --- Calculate statistics ---
//                var gradedGrades = model.ClassesAndGrades
//                                        .Where(c => c.Grade.HasValue)
//                                        .Select(c => c.Grade.Value)
//                                        .OrderBy(g => g)
//                                        .ToList();

//                model.GradeStats.TotalClasses = model.ClassesAndGrades.Count;
//                model.GradeStats.GradedClasses = gradedGrades.Count;

//                if (gradedGrades.Any())
//                {
//                    model.GradeStats.AverageGrade = gradedGrades.Average();

//                    // Median
//                    int count = gradedGrades.Count;
//                    if (count % 2 == 1)
//                        model.GradeStats.MedianGrade = gradedGrades[count / 2];
//                    else
//                        model.GradeStats.MedianGrade = (gradedGrades[(count / 2) - 1] + gradedGrades[count / 2]) / 2.0;

//                    // Mode
//                    model.GradeStats.ModeGrade = gradedGrades
//                                                 .GroupBy(g => g)
//                                                 .OrderByDescending(g => g.Count())
//                                                 .ThenBy(g => g.Key)
//                                                 .First().Key;
//                }

//                // --- Personal info ---
//                string infoQuery = @"
//                    SELECT name, surname, date_of_birth, address, city, email, telephone, index_number
//                    FROM Student
//                    WHERE student_id = @studentId";

//                using (SqlCommand cmd = new SqlCommand(infoQuery, conn))
//                {
//                    cmd.Parameters.AddWithValue("@studentId", studentId);
//                    using (SqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        if (reader.Read())
//                        {
//                            model.Name = reader["name"].ToString();
//                            model.Surname = reader["surname"].ToString();
//                            model.DateOfBirth = (DateTime)reader["date_of_birth"];
//                            model.Address = reader["address"].ToString();
//                            model.City = reader["city"].ToString();
//                            model.Email = reader["email"].ToString();
//                            model.Phone = reader["telephone"].ToString();
//                            model.Index = reader["index_number"].ToString();
//                        }
//                    }
//                }

//                // --- Classes not enrolled ---
//                string availableQuery = @"
//                    SELECT c.class_id, c.name AS ClassName, c.semester, u.username AS Professor
//                    FROM Class c
//                    JOIN Users u ON c.professor_id = u.user_id
//                    WHERE c.class_id NOT IN (SELECT class_id FROM Enrollment WHERE student_id = @studentId)";

//                using (SqlCommand cmd = new SqlCommand(availableQuery, conn))
//                {
//                    cmd.Parameters.AddWithValue("@studentId", studentId);
//                    using (SqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        while (reader.Read())
//                        {
//                            model.AvailableClasses.Add(new StudentDashboardViewModel.AvailableClass
//                            {
//                                ClassId = (int)reader["class_id"],
//                                ClassName = reader["ClassName"].ToString(),
//                                Professor = reader["Professor"].ToString(),
//                                Semester = Convert.ToInt32(reader["semester"])
//                            });
//                        }
//                    }
//                }
//            }

//            ViewBag.ActiveTab = activeTab;
//            return View(model);
//        }

//        //--------------------------------------------------------------------------------------------------------------------------------------------------
//        // POST Dashboard: save personal info
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult Dashboard(StudentDashboardViewModel model, string formType)
//        {
//            if (formType != "PersonalInfo" || model == null || model.StudentId == 0)
//                return RedirectToAction("Dashboard", new { studentId = model?.StudentId ?? 0 });

//            bool emailInvalid = string.IsNullOrWhiteSpace(model.Email) ||
//                                !(model.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
//                                  model.Email.EndsWith("@yahoo.com", StringComparison.OrdinalIgnoreCase));

//            string phoneClean = model.Phone?.Replace(" ", "") ?? "";
//            bool phoneInvalid = !System.Text.RegularExpressions.Regex.IsMatch(phoneClean, @"^\d{9}$");

//            using (SqlConnection conn = new SqlConnection(connectionString))
//            {
//                conn.Open();

//                // Save valid or fallback info
//                string emailToSave = emailInvalid ?
//                    new SqlCommand("SELECT email FROM Student WHERE student_id=@studentId", conn) { Parameters = { new SqlParameter("@studentId", model.StudentId) } }.ExecuteScalar()?.ToString() ?? ""
//                    : model.Email;

//                string phoneToSave = phoneInvalid ?
//                    new SqlCommand("SELECT telephone FROM Student WHERE student_id=@studentId", conn) { Parameters = { new SqlParameter("@studentId", model.StudentId) } }.ExecuteScalar()?.ToString() ?? ""
//                    : phoneClean;

//                string updateQuery = @"
//                    UPDATE Student
//                    SET address=@address, city=@city, email=@email, telephone=@telephone
//                    WHERE student_id=@studentId";

//                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
//                {
//                    cmd.Parameters.AddWithValue("@address", model.Address ?? "");
//                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
//                    cmd.Parameters.AddWithValue("@email", emailToSave);
//                    cmd.Parameters.AddWithValue("@telephone", phoneToSave);
//                    cmd.Parameters.AddWithValue("@studentId", model.StudentId);
//                    cmd.ExecuteNonQuery();
//                }

//                // Reload read-only fields
//                string infoQuery = @"
//                    SELECT name, surname, date_of_birth, index_number
//                    FROM Student
//                    WHERE student_id = @studentId";

//                using (SqlCommand cmd = new SqlCommand(infoQuery, conn))
//                {
//                    cmd.Parameters.AddWithValue("@studentId", model.StudentId);
//                    using (SqlDataReader reader = cmd.ExecuteReader())
//                    {
//                        if (reader.Read())
//                        {
//                            model.Name = reader["name"].ToString();
//                            model.Surname = reader["surname"].ToString();
//                            model.DateOfBirth = (DateTime)reader["date_of_birth"];
//                            model.Index = reader["index_number"].ToString();
//                        }
//                    }
//                }

//                model.ClassesAndGrades ??= new List<StudentClassGrade>();
//                model.AvailableClasses ??= new List<StudentDashboardViewModel.AvailableClass>();
//            }

//            if (emailInvalid)
//                ModelState.AddModelError("Email", "Email must end with @gmail.com or @yahoo.com.");
//            if (phoneInvalid)
//                ModelState.AddModelError("Phone", "Phone format has to be 123 456 789");

//            if (emailInvalid || phoneInvalid)
//            {
//                ViewBag.ActiveTab = "personal";
//                return View(model);
//            }

//            return RedirectToAction("Dashboard", new { studentId = model.StudentId, activeTab = "personal" });
//        }

//        //------------------------------------------------------------------------------------------------------------------------------------------
//        // POST Enroll: enroll student into a class
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public IActionResult Enroll(int studentId, int classId)
//        {
//            using (SqlConnection conn = new SqlConnection(connectionString))
//            {
//                conn.Open();
//                string insertQuery = "INSERT INTO Enrollment(student_id, class_id) VALUES(@studentId, @classId)";
//                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
//                {
//                    cmd.Parameters.AddWithValue("@studentId", studentId);
//                    cmd.Parameters.AddWithValue("@classId", classId);
//                    cmd.ExecuteNonQuery();
//                }
//            }

//            return RedirectToAction("Dashboard", new { studentId = studentId, activeTab = "classes" });
//        }
//    }
//}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using UniversityApp.Models;

namespace UniversityApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly string connectionString =
            "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult Dashboard(int studentId, string activeTab = "classes")
        {
            var model = new StudentDashboardViewModel
            {
                StudentId = studentId,
                ClassesAndGrades = new List<StudentClassGrade>(),
                AvailableClasses = new List<StudentDashboardViewModel.AvailableClass>(),
                GradeStats = new StudentDashboardViewModel.StudentGradeStats()
            };

            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            // 1️⃣ Enrolled classes + grades
            using (SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentClassesAndGrades", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StudentId", studentId);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    model.ClassesAndGrades.Add(new StudentClassGrade
                    {
                        ClassName = reader["ClassName"].ToString(),
                        Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
                    });
                }
            }

            // 2️⃣ Grade statistics (C# logic stays)
            var gradedGrades = model.ClassesAndGrades
                .Where(c => c.Grade.HasValue)
                .Select(c => c.Grade.Value)
                .OrderBy(g => g)
                .ToList();

            model.GradeStats.TotalClasses = model.ClassesAndGrades.Count;
            model.GradeStats.GradedClasses = gradedGrades.Count;

            if (gradedGrades.Any())
            {
                model.GradeStats.AverageGrade = gradedGrades.Average();

                int count = gradedGrades.Count;
                model.GradeStats.MedianGrade =
                    count % 2 == 1
                        ? gradedGrades[count / 2]
                        : (gradedGrades[count / 2 - 1] + gradedGrades[count / 2]) / 2.0;

                model.GradeStats.ModeGrade = gradedGrades
                    .GroupBy(g => g)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key)
                    .First().Key;
            }

            // 3️⃣ Personal info
            using (SqlCommand cmd = new SqlCommand("dbo.sp_GetStudentInfo", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StudentId", studentId);

                using SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    model.Name = reader["name"].ToString();
                    model.Surname = reader["surname"].ToString();
                    model.DateOfBirth = (DateTime)reader["date_of_birth"];
                    model.Address = reader["address"].ToString();
                    model.City = reader["city"].ToString();
                    model.Email = reader["email"].ToString();
                    model.Phone = reader["telephone"].ToString();
                    model.Index = reader["index_number"].ToString();
                }
            }

            // 4️⃣ Available classes
            using (SqlCommand cmd = new SqlCommand("dbo.sp_GetAvailableClassesForStudentDashboard", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@StudentId", studentId);

                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    model.AvailableClasses.Add(new StudentDashboardViewModel.AvailableClass
                    {
                        ClassId = (int)reader["class_id"],
                        ClassName = reader["ClassName"].ToString(),
                        Professor = reader["Professor"].ToString(),
                        Semester = (int)reader["semester"]
                    });
                }
            }

            ViewBag.ActiveTab = activeTab;
            return View(model);
        }

        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Dashboard(StudentDashboardViewModel model, string formType)
        {
            if (formType != "PersonalInfo")
                return RedirectToAction("Dashboard", new { studentId = model.StudentId });

            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand("dbo.sp_UpdateStudentPersonalInfo", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StudentId", model.StudentId);
            cmd.Parameters.AddWithValue("@Address", model.Address ?? "");
            cmd.Parameters.AddWithValue("@City", model.City ?? "");
            cmd.Parameters.AddWithValue("@Email", model.Email ?? "");
            cmd.Parameters.AddWithValue("@Telephone", model.Phone ?? "");

            cmd.ExecuteNonQuery();

            return RedirectToAction("Dashboard", new { studentId = model.StudentId, activeTab = "personal" });
        }

        // ------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enroll(int studentId, int classId)
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand("dbo.sp_EnrollStudent", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@StudentId", studentId);
            cmd.Parameters.AddWithValue("@ClassId", classId);

            cmd.ExecuteNonQuery();

            return RedirectToAction("Dashboard", new { studentId, activeTab = "classes" });
        }
    }
}