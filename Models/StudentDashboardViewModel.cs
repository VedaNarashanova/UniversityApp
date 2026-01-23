namespace UniversityApp.Models
{
    public class StudentDashboardViewModel
    {
        public List<StudentClassGrade> ClassesAndGrades { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Index { get;set; }
    }
}
