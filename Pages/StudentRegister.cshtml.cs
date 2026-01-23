using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.ComponentModel.DataAnnotations;



namespace UniversityApp.Pages
{
    public class StudentRegisterModel : PageModel
    {
        //[BindProperty]
        //[DataType(DataType.Date)]
        //[DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}", ApplyFormatInEditMode = true)]
        //public DateTime? DateOfBirth { get; set; }
        [BindProperty] public string Name { get; set; }
        [BindProperty] public string Surname { get; set; }
        [BindProperty] public DateTime DateOfBirth { get; set; }
        [BindProperty] public string Address { get; set; }
        [BindProperty] public string City { get; set; }
        [BindProperty] public string Telephone { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Username { get; set; }
        [BindProperty] public string Password { get; set; }

        public string ErrorMessage { get; set; }
        public string SuccessMessage { get; set; }

        public void OnGet() { }

        public IActionResult OnPost()
        {
            string connectionString =
                "Server=localhost\\SQLEXPRESS;Database=UniversityDB;Trusted_Connection=True;TrustServerCertificate=True;";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // 1️⃣ Create the user first
                    string insertUserQuery =
                        "INSERT INTO dbo.Users (username, password, role) VALUES (@username, @password, 'student'); " +
                        "SELECT SCOPE_IDENTITY();"; // get the inserted user_id

                    int userId;
                    using (SqlCommand cmd = new SqlCommand(insertUserQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", Username);
                        cmd.Parameters.AddWithValue("@password", Password);

                        userId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    //Generate index number
                    int currentYear = DateTime.Now.Year;
                    string indexPrefix = currentYear.ToString();

                    // get the max index_number starting with current year
                    string getMaxIndexQuery =
                        "SELECT MAX(index_number) FROM dbo.Student WHERE index_number LIKE @prefix + '%'";
                    int nextNumber = 1;

                    using (SqlCommand cmd = new SqlCommand(getMaxIndexQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@prefix", indexPrefix);

                        var result = cmd.ExecuteScalar();
                        if (result != DBNull.Value && result != null)
                        {
                            string maxIndex = (string)result; // e.g., "2026003"
                            nextNumber = int.Parse(maxIndex.Substring(4)) + 1; // take last 3 digits + 1
                        }
                    }

                    string indexNumber = indexPrefix + nextNumber.ToString("D3"); // e.g., "2026004"

                    // 3️⃣ Insert into Student table
                    string insertStudentQuery =
                        "INSERT INTO dbo.Student (user_id, name, surname, date_of_birth, address, city, telephone, email, index_number) " +
                        "VALUES (@user_id, @name, @surname, @dob, @address, @city, @tel, @email, @index_number)";

                    using (SqlCommand cmd = new SqlCommand(insertStudentQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@name", Name);
                        cmd.Parameters.AddWithValue("@surname", Surname);
                        cmd.Parameters.AddWithValue("@dob", DateOfBirth);
                        cmd.Parameters.AddWithValue("@address", Address);
                        cmd.Parameters.AddWithValue("@city", City);
                        cmd.Parameters.AddWithValue("@tel", Telephone);
                        cmd.Parameters.AddWithValue("@email", Email);
                        cmd.Parameters.AddWithValue("@index_number", indexNumber);

                        cmd.ExecuteNonQuery();
                    }

                    SuccessMessage = $"Student {Name} registered successfully! Your index: {indexNumber}";
                    ModelState.Clear(); // clears form
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error: " + ex.Message;
            }

            return Page();
        }
    }
}
