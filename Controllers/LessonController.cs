using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;
using Task = AdditiveEdu.Models.Task;
using Type = AdditiveEdu.Models.Type;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LessonController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{lessonId}")]
        public async Task<IActionResult> GetLesson(int lessonId)
        {
            var lesson = await _context.Lessons
                .Where(l => l.LessonID == lessonId)
                .Select(l => new
                {
                    l.LessonID,
                    l.ModuleID,
                    l.LessonTitle,
                    l.LessonDescription,
                    l.LessonOrder,
                    l.TheoryContent,
                    Module = _context.Modules
                        .Where(m => m.ModuleID == l.ModuleID)
                        .Select(m => new { m.ModuleTitle, m.ModuleNumber })
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (lesson == null)
                return NotFound(new { message = "Урок не найден" });

            return Ok(lesson);
        }

        [HttpGet("{lessonId}/tasks")]
        public async Task<IActionResult> GetLessonTasks(int lessonId)
        {
            var tasks = await _context.Tasks
                .Where(t => t.LessonID == lessonId && t.IsActive)
                .Select(t => new
                {
                    t.TaskID,
                    t.TaskTitle,
                    t.TaskDescription,
                    t.DifficultyLevel,
                    t.MaxScore,
                    TypeName = _context.Types
                        .Where(typ => typ.TypeID == t.TypeID)
                        .Select(typ => typ.TypeName)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpGet("task/{taskId}/questions")]
        public async Task<IActionResult> GetTaskQuestions(int taskId)
        {
            var questions = await _context.Questions
                .Where(q => q.TaskID == taskId)
                .OrderBy(q => q.QuestionOrder)
                .Select(q => new
                {
                    q.QuestionID,
                    q.QuestionText,
                    q.QuestionLevel,
                    q.QuestionWeight,
                    Answers = _context.Answers
                        .Where(a => a.QuestionID == q.QuestionID)
                        .Select(a => new
                        {
                            a.AnswerID,
                            a.AnswerText,
                            a.IsCorrect
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(questions);
        }

        [HttpPost("submit-answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerDto submitDto)
        {
            try
            {
                var question = await _context.Questions
                    .Include(q => q.Answers)
                    .FirstOrDefaultAsync(q => q.QuestionID == submitDto.QuestionId);
                
                if (question == null)
                    return BadRequest(new { success = false, message = "Вопрос не найден" });

                var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
                bool isCorrect = correctAnswer?.AnswerID == submitDto.AnswerId;

                return Ok(new
                {
                    success = true,
                    isCorrect = isCorrect,
                    earnedXp = isCorrect ? question.QuestionWeight : 0,
                    message = isCorrect ? $"Правильный ответ! +{question.QuestionWeight} XP" : "Неправильный ответ"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        public class SubmitAnswerDto
        {
            public int UserId { get; set; }
            public int LessonId { get; set; }
            public int TaskId { get; set; }
            public int QuestionId { get; set; }
            public int AnswerId { get; set; }
        }
    }
}