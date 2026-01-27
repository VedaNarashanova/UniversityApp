using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using UniversityApp.Models;
using static UniversityApp.Models.StudentDashboardViewModel;

namespace UniversityApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly string connectionString =
            "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public IActionResult Dashboard(int studentId)
        {
            var model = new StudentDashboardViewModel();
            model.StudentId = studentId;
            model.ClassesAndGrades = new List<StudentClassGrade>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Load classes and grades
                string query = @"
                    SELECT c.name AS ClassName, e.grade
                    FROM Enrollment e
                    JOIN Class c ON e.class_id = c.class_id
                    WHERE e.student_id = @studentId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
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

                // Load personal info
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
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult Dashboard(StudentDashboardViewModel model)
        {
            if (model == null || model.StudentId == 0)
                return RedirectToAction("Dashboard", new { studentId = model.StudentId, activeTab = "personal" });


            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string updateQuery = @"
                    UPDATE Student
                    SET address = @address,
                        city = @city,
                        email = @email,
                        telephone = @telephone
                    WHERE student_id = @studentId";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@address", model.Address ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    cmd.Parameters.AddWithValue("@telephone", model.Phone ?? "");
                    cmd.Parameters.AddWithValue("@studentId", model.StudentId);

                    cmd.ExecuteNonQuery();
                }
            }

            // After save, reload the page with updated info
            return RedirectToAction("Dashboard", new { studentId = model.StudentId, activeTab = "personal" });
        }

        [HttpGet]
        public IActionResult Dashboard(int studentId, string activeTab = null)
        {
            //ViewData["ActiveTab"] = activeTab ?? "classes";
            var model = new StudentDashboardViewModel();
            model.StudentId = studentId;
            model.ClassesAndGrades = new List<StudentClassGrade>();
            model.AvailableClasses = new List<AvailableClass>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Existing: get enrolled classes and grades
                string query = @"
                            SELECT c.class_id, c.name AS ClassName, e.grade
                            FROM Enrollment e
                            JOIN Class c ON e.class_id = c.class_id
                            WHERE e.student_id = @studentId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
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

                // Personal info (existing)
                string infoQuery = @"
                                 SELECT name, surname, date_of_birth, address, city, email, telephone, index_number
                                 FROM Student WHERE student_id=@studentId";
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

                // NEW: Get classes not yet enrolled by the student
                string availableQuery = @"
                                SELECT c.class_id, c.name AS ClassName, c.semester, u.username AS Professor
                                FROM Class c
                                JOIN Users u ON c.professor_id = u.user_id
                                WHERE c.class_id NOT IN (SELECT class_id FROM Enrollment WHERE student_id=@studentId)";
                using (SqlCommand cmd = new SqlCommand(availableQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.AvailableClasses.Add(new AvailableClass
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

            ViewBag.ActiveTab = activeTab; // pass tab info to view
            return View(model);
        }

        [HttpPost]
        public IActionResult Enroll(int studentId, int classId)
        {
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

            // Redirect back to dashboard showing Classes tab
            return RedirectToAction("Dashboard", new { studentId = studentId, activeTab = "classes" });
        }
    }
}
