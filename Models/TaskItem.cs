
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoList.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(150)]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Deadline is required")]
        public DateTime? Deadline { get; set; }

        // اسم الملف الأصلي
        public string? FileName { get; set; }

        // المسار الداخلي للملف
        public string? FilePath { get; set; }

        // الملف الذي يختاره المستخدم، ولا يتم تخزينه كعمود
        [NotMapped]
        public IFormFile? UploadFile { get; set; }
    }
}