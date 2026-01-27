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
        public int StudentId { get; set; }



        public List<AvailableClass> AvailableClasses { get; set; } = new List<AvailableClass>();
        public class AvailableClass
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; }
            public string Professor { get; set; }
            public int Semester
            {
                get; set;
            }
        }
    }
}
