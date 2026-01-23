using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

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

        private int GetStudentId(int userId)
        {
            using SqlConnection conn = new SqlConnection(
                "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;");
            conn.Open();

            string query = "SELECT student_id FROM Student WHERE user_id=@userId";
            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            return (int)cmd.ExecuteScalar();
        }


        public IActionResult OnPost()
        {
            // Connection string to your SQL Server database
            string connectionString ="Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";


            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT user_id,role FROM dbo.Users WHERE username=@username AND password=@password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", Username);
                    cmd.Parameters.AddWithValue("@password", Password);

                    //var role = cmd.ExecuteScalar();

                    var reader = cmd.ExecuteReader();//runs a SELECT query aka the query we have and return the rows aka return the info for the user whos username and pass match
                    if (reader.Read())
                    {
                        int userId = (int)reader["user_id"];
                        string role = reader["role"].ToString();

                        if (role.ToString() == "student") { 
                             int studentId = GetStudentId(userId);
                        return RedirectToAction("Dashboard", "Student", new { studentId });} //Dashboard is the method, Student is StudentController where the method is
                    }
                    ErrorMessage = "Invalid username or password";
                    return Page();
                    //if (role != null)
                    //{
                    //    // Redirect based on role
                    //    if (role.ToString() == "student")
                    //        return RedirectToPage("StudentDashboard"); 
                    //    else if (role.ToString() == "professor")
                    //        return RedirectToPage("ProfessorDashboard"); 
                    //}
                    //else
                    //{
                    //    // Invalid login
                    //    ErrorMessage = "Invalid username or password";
                    //    return Page();
                    //}
                }
            }

            return Page();
        }
    }
}
