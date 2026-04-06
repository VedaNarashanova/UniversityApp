using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using UniversityApp.Models;

namespace UniversityApp.Controllers
{
    public class StudentRegisterController : Controller
    {
        // Store the connection string once in a readonly variable
        private readonly string connectionString;

        public StudentRegisterController(IConfiguration configuration)
        {
            // Read connection string from appsettings.json
            connectionString = configuration.GetConnectionString("UniversityDB")
                               ?? throw new Exception("Connection string 'UniversityDB' not found!");
        }

        
        // GET
        [HttpGet]
        public IActionResult Register()
        {
            var model = new StudentRegisterViewModel
            {
                SuccessMessage = TempData["SuccessMessage"] as string
            };

            return View(model);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(StudentRegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ErrorMessage = "Validation failed";
                return View(model);
            }

            try
            {
                using SqlConnection conn = new SqlConnection(connectionString);
                conn.Open();

                //Check if username exists
                using (SqlCommand checkCmd = new SqlCommand("sp_CheckUsername", conn))
                {
                    checkCmd.CommandType = CommandType.StoredProcedure;
                    checkCmd.Parameters.AddWithValue("@username", model.Username);

                    int count = (int)checkCmd.ExecuteScalar();

                    if (count > 0)
                    {
                        model.ErrorMessage = "This username is already taken.";
                        return View(model); // opens the view with the same name as the controller action
                    }
                }

                //Register student using stored procedure
                using (SqlCommand cmd = new SqlCommand("sp_RegisterStudent", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@username", model.Username);
                    cmd.Parameters.AddWithValue("@password", model.Password);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@surname", model.Surname);
                    cmd.Parameters.AddWithValue("@dob", model.DateOfBirth);
                    cmd.Parameters.AddWithValue("@address", model.Address);
                    cmd.Parameters.AddWithValue("@city", model.City);
                    cmd.Parameters.AddWithValue("@tel", model.Telephone);
                    cmd.Parameters.AddWithValue("@email", model.Email);

                    string generatedIndex = cmd.ExecuteScalar().ToString();

                    TempData["SuccessMessage"] =
                        $"Student {model.Name} registered successfully! Index: {generatedIndex}";
                }

                return RedirectToAction("Register");
            }
            catch (Exception ex)
            {
                model.ErrorMessage = "Error: " + ex.Message;
                return View(model);
            }
        }
    }
}