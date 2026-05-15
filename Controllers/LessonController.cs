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
        [HttpPost("save-result")]
        public async Task<IActionResult> SaveTestResult([FromBody] SaveResultDto resultDto)
        {
            try
            {
                Console.WriteLine($"=== SaveTestResult called ===");
                Console.WriteLine($"UserId: {resultDto.UserId}, Score: {resultDto.Score}");
                
                var existingResult = await _context.TaskResults
                    .FirstOrDefaultAsync(tr => tr.UserID == resultDto.UserId && tr.TaskID == resultDto.TaskId);
                
                if (existingResult != null)
                {
                    return Ok(new { success = true, alreadySaved = true });
                }
                
                var taskResult = new TaskResult
                {
                    UserID = resultDto.UserId,
                    TaskID = resultDto.TaskId,
                    Score = resultDto.Score,
                    AttemptNumber = 1,
                    CompletionStatus = "completed",
                    CompletedAt = DateTime.Now
                };
                _context.TaskResults.Add(taskResult);
                
                UpdateLessonProgress(resultDto.UserId, resultDto.LessonId);
                UpdateUserExperience(resultDto.UserId, resultDto.Score);
                
                // ОДИН РАЗ СОХРАНЯЕМ ВСЕ ИЗМЕНЕНИЯ
                await _context.SaveChangesAsync();
                
                var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == resultDto.UserId);
                Console.WriteLine($"Saved. New XP: {rating?.Experience}, Level: {rating?.CurrentLevel}");
                
                return Ok(new
                {
                    success = true,
                    xpGained = resultDto.Score,
                    newTotalXp = rating?.Experience ?? resultDto.Score,
                    newLevel = rating?.CurrentLevel ?? 1
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private void UpdateLessonProgress(int userId, int lessonId)
        {
            var lessonTasks = _context.Tasks
                .Where(t => t.LessonID == lessonId && t.IsActive)
                .Select(t => t.TaskID)
                .ToList();
            
            var completedTasks = _context.TaskResults
                .Where(tr => tr.UserID == userId && lessonTasks.Contains(tr.TaskID))
                .Select(tr => tr.TaskID)
                .ToList();
            
            int progressPercent = lessonTasks.Count > 0 
                ? (int)((double)completedTasks.Count / lessonTasks.Count * 100) 
                : 0;
            bool isLessonCompleted = progressPercent == 100;
            
            var lessonProgress = _context.LessonProgresses
                .FirstOrDefault(lp => lp.UserID == userId && lp.LessonID == lessonId);
            
            if (lessonProgress == null)
            {
                lessonProgress = new LessonProgress
                {
                    UserID = userId,
                    LessonID = lessonId,
                    ProgressPercent = progressPercent,
                    IsCompleted = isLessonCompleted,
                    CompletionStatus = isLessonCompleted ? "completed" : "in_progress"
                };
                _context.LessonProgresses.Add(lessonProgress);
            }
            else if (lessonProgress.ProgressPercent < progressPercent)
            {
                lessonProgress.ProgressPercent = progressPercent;
                lessonProgress.IsCompleted = isLessonCompleted;
                lessonProgress.CompletionStatus = isLessonCompleted ? "completed" : "in_progress";
            }
            
            // НЕ вызываем SaveChanges здесь
        }

        private void UpdateUserExperience(int userId, int xpGain)
        {
            var rating = _context.Ratings.FirstOrDefault(r => r.UserID == userId);
            
            if (rating == null)
            {
                rating = new Rating
                {
                    UserID = userId,
                    TotalScore = xpGain,
                    CurrentLevel = 1,
                    Experience = xpGain,
                    PositionInRating = 0
                };
                _context.Ratings.Add(rating);
            }
            else
            {
                rating.Experience += xpGain;
                rating.TotalScore += xpGain;
                rating.CurrentLevel = (rating.Experience / 100) + 1;
            }
            
            // НЕ вызываем SaveChanges здесь
        }
                

        // DTO классы
        public class SubmitAnswerDto
        {
            public int UserId { get; set; }
            public int LessonId { get; set; }
            public int TaskId { get; set; }
            public int QuestionId { get; set; }
            public int AnswerId { get; set; }
        }

        public class SaveResultDto
        {
            public int UserId { get; set; }
            public int LessonId { get; set; }
            public int TaskId { get; set; }
            public int Score { get; set; }
            public int MaxScore { get; set; }
            public List<AnswerDetailDto> Answers { get; set; } = new();
        }

        public class AnswerDetailDto
        {
            public int QuestionId { get; set; }
            public int SelectedAnswerId { get; set; }
            public bool IsCorrect { get; set; }
        }
    }
}