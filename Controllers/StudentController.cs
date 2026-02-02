
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using UniversityApp.Models;

namespace UniversityApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly string connectionString =
            "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";


//--------------------------------------------------------------------------------------------------------------------------------------------------
        // GET Dashboard - Show Student dashboard with classes, grades and personal info
        [HttpGet]
        public IActionResult Dashboard(int studentId, string activeTab = "classes")
        {
            var model = new StudentDashboardViewModel();
            model.StudentId = studentId;
            model.ClassesAndGrades = new List<StudentClassGrade>();
            model.AvailableClasses = new List<StudentDashboardViewModel.AvailableClass>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Enrolled classes & grades
                string query = @"
                    SELECT c.class_id, c.name AS ClassName, e.grade
                    FROM Enrollment e
                    JOIN Class c ON e.class_id = c.class_id
                    WHERE e.student_id = @studentId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId); // add a value to the SQL query parameter @studentId, aka replace that with the variable studentId
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.ClassesAndGrades.Add(new StudentClassGrade
                            {
                                ClassName = reader["ClassName"].ToString(),
                                Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
                            });
                        }
                    }
                }

                // Personal info
                string infoQuery = @"
                    SELECT name, surname, date_of_birth, address, city, email, telephone, index_number
                    FROM Student
                    WHERE student_id = @studentId";
                using (SqlCommand cmd = new SqlCommand(infoQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
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
                }

                // Classes not enrolled
                string availableQuery = @"
                    SELECT c.class_id, c.name AS ClassName, c.semester, u.username AS Professor
                    FROM Class c
                    JOIN Users u ON c.professor_id = u.user_id
                    WHERE c.class_id NOT IN (SELECT class_id FROM Enrollment WHERE student_id = @studentId)";
                using (SqlCommand cmd = new SqlCommand(availableQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailableClasses.Add(new StudentDashboardViewModel.AvailableClass
                            {
                                ClassId = (int)reader["class_id"],
                                ClassName = reader["ClassName"].ToString(),
                                Professor = reader["Professor"].ToString(),
                                Semester = Convert.ToInt32(reader["semester"])
                            });
                        }
                    }
                }
            }

            ViewBag.ActiveTab = activeTab;
            return View(model);
        }


        //--------------------------------------------------------------------------------------------------------------------------------------------------
        // POST Dashboard: save personal info
        //[HttpPost]
        //[ValidateAntiForgeryToken] //security check to make sure the form was submitted from the user
        //public IActionResult Dashboard(StudentDashboardViewModel model, string formType)
        //{
        //    if (formType != "PersonalInfo")//tells us from which form the submit is coming from so it doesnt get mixed up with the Enrolment
        //    {
        //        return RedirectToAction("Dashboard", new { studentId = model.StudentId });
        //    }
        //    if (model == null || model.StudentId == 0)
        //        return RedirectToAction("Dashboard", new { studentId = model?.StudentId ?? 0 });

        //    //if (string.IsNullOrWhiteSpace(model.Email) || !(model.Email.EndsWith("@gmail.com") || model.Email.EndsWith("@yahoo.com")))
        //    //{
        //    //    ModelState.AddModelError("Email", "Email must contain text and end with @gmail.com or @yahoo.com.");
        //    //}

        //    if (!ModelState.IsValid)
        //    {
        //        model.ClassesAndGrades = new List<StudentClassGrade>();
        //    }
        //    using (SqlConnection conn = new SqlConnection(connectionString))
        //    {
        //        conn.Open();

        //        string updateQuery = @"
        //            UPDATE Student
        //            SET address=@address, city=@city, email=@email, telephone=@telephone
        //            WHERE student_id=@studentId";
        //        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
        //        {
        //            cmd.Parameters.AddWithValue("@address", model.Address ?? "");
        //            cmd.Parameters.AddWithValue("@city", model.City ?? "");
        //            cmd.Parameters.AddWithValue("@email", model.Email ?? "");
        //            cmd.Parameters.AddWithValue("@telephone", model.Phone ?? "");
        //            cmd.Parameters.AddWithValue("@studentId", model.StudentId);
        //            cmd.ExecuteNonQuery();
        //        }
        //    }

        //    return RedirectToAction("Dashboard", new
        //    {
        //        studentId = model.StudentId,
        //        activeTab = "personal"
        //    });
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Dashboard(StudentDashboardViewModel model, string formType)
        {
            if (formType != "PersonalInfo" || model == null || model.StudentId == 0)
                return RedirectToAction("Dashboard", new { studentId = model?.StudentId ?? 0 });

            // --- Validate email ---
            bool emailInvalid = string.IsNullOrWhiteSpace(model.Email) ||
                                !(model.Email.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase) ||
                                  model.Email.EndsWith("@yahoo.com", StringComparison.OrdinalIgnoreCase));

            // --- Validate phone ---
            string phoneClean = model.Phone?.Replace(" ", "") ?? "";
            bool phoneInvalid = !System.Text.RegularExpressions.Regex.IsMatch(phoneClean, @"^\d{9}$");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // --- Get old email if invalid ---
                string emailToSave = model.Email;
                //if the email is bad
                if (emailInvalid)
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT email FROM Student WHERE student_id=@studentId", conn))
                    {
                        cmd.Parameters.AddWithValue("@studentId", model.StudentId);
                        emailToSave = cmd.ExecuteScalar()?.ToString() ?? "";
                    }
                }

                // --- Get old phone if invalid ---
                string phoneToSave = phoneClean;
                if (phoneInvalid)
                {
                    using (SqlCommand cmd = new SqlCommand("SELECT telephone FROM Student WHERE student_id=@studentId", conn))
                    {
                        cmd.Parameters.AddWithValue("@studentId", model.StudentId);
                        phoneToSave = cmd.ExecuteScalar()?.ToString() ?? "";
                    }
                }

                // --- Save info ---
                string updateQuery = @"
                                    UPDATE Student
                                    SET address=@address, city=@city, email=@email, telephone=@telephone
                                    WHERE student_id=@studentId";
                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@address", model.Address ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@email", emailToSave);
                    cmd.Parameters.AddWithValue("@telephone", phoneToSave);
                    cmd.Parameters.AddWithValue("@studentId", model.StudentId);
                    cmd.ExecuteNonQuery();
                }

                // --- Reload read-only fields ---
                string infoQuery = @"
                                SELECT name, surname, date_of_birth, index_number
                                FROM Student
                                WHERE student_id = @studentId";
                using (SqlCommand cmd = new SqlCommand(infoQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", model.StudentId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.Name = reader["name"].ToString();
                            model.Surname = reader["surname"].ToString();
                            model.DateOfBirth = (DateTime)reader["date_of_birth"];
                            model.Index = reader["index_number"].ToString();
                        }
                    }
                }

                // --- Ensure lists are not null (avoid NullReferenceException) ---
                model.ClassesAndGrades ??= new List<StudentClassGrade>();
                model.AvailableClasses ??= new List<StudentDashboardViewModel.AvailableClass>();
            }

            // --- Show validation errors if any ---
            if (emailInvalid)
                ModelState.AddModelError("Email", "Email must end with @gmail.com or @yahoo.com.");
            if (phoneInvalid)
                ModelState.AddModelError("Phone", "Phone format has to be 123 456 789");

            if (emailInvalid || phoneInvalid)
            {
                ViewBag.ActiveTab = "personal";
                return View(model); // show errors, preserve old email/phone
            }

            // --- All valid → redirect normally ---
            return RedirectToAction("Dashboard", new { studentId = model.StudentId, activeTab = "personal" });
        }








        //------------------------------------------------------------------------------------------------------------------------------------------
        // POST Enroll: enroll student into a class
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enroll(int studentId, int classId)
        {
            //System.Diagnostics.Debug.WriteLine( $"ENROLL HIT → studentId={studentId}, classId={classId}");
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string insertQuery = "INSERT INTO Enrollment(student_id, class_id) VALUES(@studentId, @classId)";
                using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    cmd.Parameters.AddWithValue("@classId", classId);
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Dashboard", new { studentId = studentId, activeTab = "classes" });
        }
    }
}
