namespace UniversityApp.Models.Professor
{
    public class ProfessorClassViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public List<StudentSimpleViewModel> Students { get; set; }
    }

}
