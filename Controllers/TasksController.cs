
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ToDoList.Data;
using ToDoList.Models;

namespace ToDoList.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public TasksController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // عرض المهام والبحث حسب اليوم والوقت
        public async Task<IActionResult> Index(
            DateTime? searchDate,
            TimeSpan? startTime,
            TimeSpan? endTime)
        {
            var tasks = _context.Tasks.AsQueryable();

            // البحث حسب اليوم
            if (searchDate.HasValue)
            {
                var date = searchDate.Value.Date;
                var nextDate = date.AddDays(1);

                tasks = tasks.Where(t =>
                    t.Deadline.HasValue &&
                    t.Deadline.Value >= date &&
                    t.Deadline.Value < nextDate);
            }

            // البحث من وقت معين
            if (startTime.HasValue)
            {
                tasks = tasks.Where(t =>
                    t.Deadline.HasValue &&
                    t.Deadline.Value.TimeOfDay >= startTime.Value);
            }

            // البحث حتى وقت معين
            if (endTime.HasValue)
            {
                tasks = tasks.Where(t =>
                    t.Deadline.HasValue &&
                    t.Deadline.Value.TimeOfDay <= endTime.Value);
            }

            var result = await tasks
                .OrderBy(t => t.Deadline)
                .ToListAsync();

            return View(result);
        }

        // صفحة إضافة مهمة
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // حفظ المهمة الجديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem task)
        {
            if (task.Deadline == null)
            {
                ModelState.AddModelError(
                    nameof(task.Deadline),
                    "Please select a deadline.");
            }
            else if (task.Deadline.Value <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(task.Deadline),
                    "Deadline must be in the future.");
            }

            if (task.UploadFile != null &&
                task.UploadFile.Length > 100 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(task.UploadFile),
                    "Maximum file size is 100 MB.");
            }

            if (!ModelState.IsValid)
                return View(task);

            if (task.UploadFile != null &&
                task.UploadFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath, "uploads");

                Directory.CreateDirectory(uploadsFolder);

                var originalName = Path.GetFileName(
                    task.UploadFile.FileName);

                var extension = Path.GetExtension(originalName);

                var uniqueName = Guid.NewGuid().ToString("N")
                    + extension;

                var filePath = Path.Combine(
                    uploadsFolder, uniqueName);

                using (var stream = new FileStream(
                    filePath, FileMode.Create))
                {
                    await task.UploadFile.CopyToAsync(stream);
                }

                task.FileName = originalName;
                task.FilePath = uniqueName;
            }

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // صفحة تعديل المهمة
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
                return NotFound();

            return View(task);
        }

        // حفظ التعديلات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, TaskItem input)
        {
            if (id != input.Id)
                return NotFound();

            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
                return NotFound();

            if (input.Deadline == null)
            {
                ModelState.AddModelError(
                    nameof(input.Deadline),
                    "Please select a deadline.");
            }
            else if (input.Deadline.Value <= DateTime.Now)
            {
                ModelState.AddModelError(
                    nameof(input.Deadline),
                    "Deadline must be in the future.");
            }

            if (input.UploadFile != null &&
                input.UploadFile.Length > 100 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    nameof(input.UploadFile),
                    "Maximum file size is 100 MB.");
            }

            if (!ModelState.IsValid)
                return View(input);

            task.Title = input.Title;
            task.Description = input.Description;
            task.Deadline = input.Deadline;

            // استبدال الملف القديم عند اختيار ملف جديد
            if (input.UploadFile != null &&
                input.UploadFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath, "uploads");

                Directory.CreateDirectory(uploadsFolder);

                var originalName = Path.GetFileName(
                    input.UploadFile.FileName);

                var extension = Path.GetExtension(originalName);

                var uniqueName = Guid.NewGuid().ToString("N")
                    + extension;

                var newPath = Path.Combine(
                    uploadsFolder, uniqueName);

                using (var stream = new FileStream(
                    newPath, FileMode.Create))
                {
                    await input.UploadFile.CopyToAsync(stream);
                }

                // حذف الملف القديم
                if (!string.IsNullOrEmpty(task.FilePath))
                {
                    var oldPath = Path.Combine(
                        uploadsFolder,
                        Path.GetFileName(task.FilePath));

                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                task.FileName = originalName;
                task.FilePath = uniqueName;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // تنزيل الملف المرفق
        public async Task<IActionResult> Download(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null ||
                string.IsNullOrEmpty(task.FilePath))
                return NotFound();

            var safeFileName = Path.GetFileName(task.FilePath);

            var fullPath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                safeFileName);

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            return PhysicalFile(
                fullPath,
                "application/octet-stream",
                task.FileName ?? "download");
        }

        // فتح الملف في المتصفح
        public async Task<IActionResult> OpenFile(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null ||
                string.IsNullOrEmpty(task.FilePath))
                return NotFound();

            var fullPath = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                Path.GetFileName(task.FilePath));

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var provider =
                new Microsoft.AspNetCore.StaticFiles
                    .FileExtensionContentTypeProvider();

            if (!provider.TryGetContentType(
                fullPath, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return PhysicalFile(fullPath, contentType);
        }

        // حذف المهمة والملف المرفق
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.Tasks.FindAsync(id);

            if (task == null)
                return NotFound();

            if (!string.IsNullOrEmpty(task.FilePath))
            {
                var fullPath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    Path.GetFileName(task.FilePath));

                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}

