using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using UniversityApp.Models;

namespace UniversityApp.Controllers
{
    public class StudentController : Controller
    {
        private readonly string connectionString = //database connecting info
            "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

        public IActionResult Dashboard(int studentId)
        {
            //var list = new List<StudentClassGrade>();
            var model = new StudentDashboardViewModel();
            model.ClassesAndGrades = new List<StudentClassGrade>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string query = @"
                    SELECT c.name AS ClassName, e.grade
                    FROM Enrollment e
                    JOIN Class c ON e.class_id = c.class_id
                    WHERE e.student_id = @studentId";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentId);
                    using (SqlDataReader reader = cmd.ExecuteReader())//run the queary, read it aka getting all the classes and grades for that student
                    {
                        while (reader.Read())////looping through the classes and grades
                        {
                            //list.Add(new StudentClassGrade
                            //{
                            //    ClassName = reader["ClassName"].ToString(),
                            //    Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
                            //});
                            model.ClassesAndGrades.Add(new StudentClassGrade
                            {
                                ClassName = reader["ClassName"].ToString(),
                                Grade = reader["grade"] == DBNull.Value ? null : (int)reader["grade"]
                            });
                        }
                    }
                }

                // Get personal info
                string infoQuery = @"
                    SELECT name, surname, date_of_birth, address, city, email, telephone,index_number
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
    }
}

