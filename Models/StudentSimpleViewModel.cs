namespace UniversityApp.Models
{
    public class StudentSimpleViewModel
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Index { get; set; }

        public int ? Grade {  get; set; }
    }
}
