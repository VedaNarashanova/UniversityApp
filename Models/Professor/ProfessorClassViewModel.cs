namespace UniversityApp.Models.Professor
{
    public class ProfessorClassViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<StudentSimpleViewModel> Students { get; set; }

        public ClassStatistics Stats { get; set; } // <-- Add this
    }


    public class ClassStatistics
    {
        public int TotalStudents { get; set; }
        public int GradedStudents { get; set; }
        public double? AverageGrade { get; set; }
        public double? MedianGrade { get; set; }
        public int? ModeGrade { get; set; }
    }

}
