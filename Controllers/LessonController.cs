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
[HttpPost("submit-test")]
public async Task<IActionResult> SubmitTest([FromBody] SubmitTestDto testDto)
{
    try
    {
        Console.WriteLine("=== SUBMIT TEST ===");
        Console.WriteLine($"UserId: {testDto.UserId}, TaskId: {testDto.TaskId}");
        Console.WriteLine($"Answers count: {testDto.Answers?.Count ?? 0}");
        
        // Выводим каждый ответ для отладки
        foreach (var answer in testDto.Answers)
        {
            Console.WriteLine($"Answer: QuestionId={answer.QuestionId}, SelectedAnswerId={answer.SelectedAnswerId}");
        }
        
        int totalScore = 0;
        int maxScore = 0;
        
        foreach (var answer in testDto.Answers)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionID == answer.QuestionId);
            
            if (question == null)
            {
                Console.WriteLine($"Question {answer.QuestionId} not found!");
                continue;
            }
            
            Console.WriteLine($"Question {question.QuestionID}: weight={question.QuestionWeight}");
            maxScore += question.QuestionWeight;
            
            var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
            if (correctAnswer != null)
            {
                Console.WriteLine($"Correct answer: {correctAnswer.AnswerID}, User answer: {answer.SelectedAnswerId}");
                if (correctAnswer.AnswerID == answer.SelectedAnswerId)
                {
                    totalScore += question.QuestionWeight;
                    Console.WriteLine($"Correct! +{question.QuestionWeight}");
                }
            }
        }
        
        Console.WriteLine($"TotalScore: {totalScore}, MaxScore: {maxScore}");
        
        // Проверяем, не пройден ли уже тест
        var existingResult = await _context.TaskResults
            .FirstOrDefaultAsync(tr => tr.UserID == testDto.UserId && tr.TaskID == testDto.TaskId);
        
        if (existingResult == null)
        {
            var taskResult = new TaskResult
            {
                UserID = testDto.UserId,
                TaskID = testDto.TaskId,
                Score = totalScore,
                AttemptNumber = 1,
                CompletionStatus = "completed",
                CompletedAt = DateTime.UtcNow
            };
            _context.TaskResults.Add(taskResult);
            
            // Обновляем опыт
            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == testDto.UserId);
            if (rating == null)
            {
                rating = new Rating
                {
                    UserID = testDto.UserId,
                    TotalScore = totalScore,
                    CurrentLevel = 1,
                    Experience = totalScore,
                    PositionInRating = 0
                };
                _context.Ratings.Add(rating);
            }
            else
            {
                rating.Experience += totalScore;
                rating.TotalScore += totalScore;
                rating.CurrentLevel = (rating.Experience / 100) + 1;
            }
            
            // Обновляем прогресс урока
            var lessonProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.UserID == testDto.UserId && lp.LessonID == testDto.LessonId);
            
            if (lessonProgress == null)
            {
                lessonProgress = new LessonProgress
                {
                    UserID = testDto.UserId,
                    LessonID = testDto.LessonId,
                    ProgressPercent = maxScore > 0 ? (totalScore * 100 / maxScore) : 0,
                    IsCompleted = totalScore == maxScore,
                    CompletionStatus = totalScore == maxScore ? "completed" : "in_progress"
                };
                _context.LessonProgresses.Add(lessonProgress);
            }
            else
            {
                lessonProgress.ProgressPercent = maxScore > 0 ? (totalScore * 100 / maxScore) : 0;
                lessonProgress.IsCompleted = totalScore == maxScore;
                lessonProgress.CompletionStatus = totalScore == maxScore ? "completed" : "in_progress";
            }
            
            await _context.SaveChangesAsync();
            Console.WriteLine("Saved to database!");
        }
        
        var updatedRating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == testDto.UserId);
        
        return Ok(new
        {
            success = true,
            totalScore = totalScore,
            maxScore = maxScore,
            xpGained = totalScore,
            newTotalXp = updatedRating?.Experience ?? 0,
            newLevel = updatedRating?.CurrentLevel ?? 1
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}
        [HttpGet("{lessonId}/progress/{userId}")]
        public async Task<IActionResult> GetLessonProgress(int lessonId, int userId)
        {
            var lessonProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.UserID == userId && lp.LessonID == lessonId);
            
            var allTasks = await _context.Tasks
                .Where(t => t.LessonID == lessonId && t.IsActive)
                .ToListAsync();
            
            var completedTasks = await _context.TaskResults
                .Where(tr => tr.UserID == userId && allTasks.Select(t => t.TaskID).Contains(tr.TaskID))
                .ToListAsync();
            
            int totalScore = completedTasks.Sum(tr => tr.Score);
            int maxScore = allTasks.Sum(t => t.MaxScore);
            
            return Ok(new
            {
                isCompleted = lessonProgress?.IsCompleted ?? false,
                progressPercent = lessonProgress?.ProgressPercent ?? 0,
                totalScore = totalScore,
                maxScore = maxScore,
                completedTasksCount = completedTasks.Count,
                totalTasksCount = allTasks.Count
            });
        }

        // DTO классы
        public class SubmitTestDto
        {
            public int UserId { get; set; }
            public int LessonId { get; set; }
            public int TaskId { get; set; }
            public List<AnswerItemDto> Answers { get; set; } = new();
        }

        public class AnswerItemDto
        {
            public int QuestionId { get; set; }
            public int SelectedAnswerId { get; set; }
        }
    }
}