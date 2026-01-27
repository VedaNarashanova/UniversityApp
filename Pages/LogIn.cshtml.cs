using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;

namespace UniversityApp.Pages
{
    public class LogInModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        // 🔹 Get student_id from user_id
        private int GetStudentId(int userId)
        {
            using SqlConnection conn = new SqlConnection(
                "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;");
            conn.Open();

            string query = "SELECT student_id FROM Student WHERE user_id = @userId";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            object result = cmd.ExecuteScalar();

            if (result == null)
                return 0;

            return Convert.ToInt32(result);
        }

        public IActionResult OnPost()
        {
            string connectionString =
                "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            string query = "SELECT user_id, role FROM dbo.Users WHERE username = @username AND password = @password";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@username", Username);
            cmd.Parameters.AddWithValue("@password", Password);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                int userId = (int)reader["user_id"];
                string role = reader["role"].ToString();

                reader.Close(); // REQUIRED before new DB query

                if (role == "student")
                {
                    int studentId = GetStudentId(userId);

                    System.Diagnostics.Debug.WriteLine("LOGIN studentId = " + studentId);

                    return RedirectToAction(
                        "Dashboard",
                        "Student",
                        new { studentId = studentId }
                    );
                }

                // (later you can add professor redirect here)
            }

            ErrorMessage = "Invalid username or password";
            return Page();
        }
    }
}
