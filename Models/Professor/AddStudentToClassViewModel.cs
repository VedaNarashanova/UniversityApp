using System.ComponentModel.DataAnnotations;

namespace UniversityApp.Models.Professor
{
    public class AddStudentToClassViewModel
    {
        public int ProfessorId { get; set; }

        [Required]
        public string StudentIndex { get; set; }

        public List<int> SelectedClassIds { get; set; } = new();

        public List<ProfessorClassViewModel> Classes { get; set; } = new();

        public bool ClassesLoaded { get; set; } = false;
    }
}
