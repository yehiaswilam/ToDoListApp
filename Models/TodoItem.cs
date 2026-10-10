using System.ComponentModel.DataAnnotations;

namespace ToDoListApp.Models;

public class TodoItem
{
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime Deadline { get; set; }

    // اسم الملف المحفوظ على السيرفر (GUID + extension)
    public string? FilePath { get; set; }

    // الاسم الأصلي للملف عشان نرجّعه عند الـ Download
    public string? OriginalFileName { get; set; }
}
