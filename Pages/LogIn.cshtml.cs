//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.Data.SqlClient;
//using System;

//namespace UniversityApp.Pages
//{
//    public class LogInModel : PageModel
//    {
//        [BindProperty]
//        public string Username { get; set; }

//        [BindProperty]
//        public string Password { get; set; }

//        public string ErrorMessage { get; set; }

//        public void OnGet()
//        {
//        }

//        // 🔹 Get student_id from user_id
//        private int GetStudentId(int userId)
//        {
//            using SqlConnection conn = new SqlConnection(
//                "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;");
//            conn.Open();

//            string query = "SELECT student_id FROM Student WHERE user_id = @userId";
//            using SqlCommand cmd = new SqlCommand(query, conn);
//            cmd.Parameters.AddWithValue("@userId", userId);

//            object result = cmd.ExecuteScalar();

//            if (result == null)
//                return 0;

//            return Convert.ToInt32(result);
//        }

//        public IActionResult OnPost()
//        {
//            // 🔹 Check if fields are empty first
//            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
//            {
//                ErrorMessage = "Please enter both username and password";
//                Username = string.Empty;
//                Password = string.Empty;
//                return Page();
//            }

//            string connectionString =
//                "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

//            using SqlConnection conn = new SqlConnection(connectionString);
//            conn.Open();

//            string query = "SELECT user_id, role, password FROM dbo.Users WHERE username = @username";

//            using SqlCommand cmd = new SqlCommand(query, conn);
//            cmd.Parameters.AddWithValue("@username", Username);  // safe now, Username is not null

//            using SqlDataReader reader = cmd.ExecuteReader();

//            if (reader.Read())
//            {
//                string storedPassword = reader["password"].ToString();

//                if (storedPassword != Password)
//                {
//                    ErrorMessage = "Invalid username or password";
//                    Username = string.Empty;
//                    Password = string.Empty;
//                    return Page();
//                }

//                int userId = (int)reader["user_id"];
//                string role = reader["role"].ToString();
//                reader.Close();

//                if (role == "student")
//                {
//                    int studentId = GetStudentId(userId);
//                    return RedirectToAction("Dashboard", "Student", new { studentId = studentId });
//                }

//                if (role == "professor")
//                {
//                    return RedirectToAction("Dashboard", "Professor", new { professorId = userId });
//                }
//            }
//            else
//            {
//                // Username not found
//                ErrorMessage = "Invalid username or password";
//                Username = string.Empty;
//                Password = string.Empty;
//            }

//            return Page();
//        }
//    }
//}
