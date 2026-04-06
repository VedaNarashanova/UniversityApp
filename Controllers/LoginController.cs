using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using UniversityApp.Models;

namespace UniversityApp.Controllers
{
    public class LoginController : Controller
    {
        // Store the connection string once and make it readonly
        private readonly string connectionString;

        public LoginController(IConfiguration configuration)
        {
            // Read from appsettings.json and store in readonly variable
            connectionString = configuration.GetConnectionString("UniversityDB")
                              ?? throw new Exception("Connection string 'UniversityDB' not found!");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Username) ||
                string.IsNullOrWhiteSpace(model.Password))
            {
                model.ErrorMessage = "Please enter both username and password";
                model.Username = "";
                model.Password = "";
                return View(model);
            }

            // Use readonly connectionString instead of _configuration.GetConnectionString()
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand("sp_LoginUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@username", model.Username);

            using SqlDataReader reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                int userId = (int)reader["user_id"];
                string storedPassword = reader["password"].ToString();
                string role = reader["role"].ToString();

                if (storedPassword != model.Password)
                {
                    model.ErrorMessage = "Invalid username or password";
                    model.Username = "";
                    model.Password = "";
                    return View(model);
                }

                reader.Close();

                if (role == "student")
                {
                    int studentId = GetStudentId(userId);
                    return RedirectToAction("Dashboard", "Student", new { studentId });
                }

                if (role == "professor")
                {
                    return RedirectToAction("Dashboard", "Professor", new { professorId = userId });
                }
            }

            model.ErrorMessage = "Invalid username or password";
            model.Username = "";
            model.Password = "";
            return View(model);
        }

        private int GetStudentId(int userId)
        {
            using SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();

            using SqlCommand cmd = new SqlCommand("sp_GetStudentId", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@userId", userId);

            object result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }
    }
}