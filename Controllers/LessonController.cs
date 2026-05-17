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

        [HttpGet("task/{taskId}/match-data")]
        public async Task<IActionResult> GetMatchData(int taskId)
        {
            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.TaskID == taskId);
            
            if (question == null)
                return NotFound(new { message = "Вопрос не найден" });
            
            if (string.IsNullOrEmpty(question.QuestionText))
                return NotFound(new { message = "Данные для сопоставления не найдены" });
            
            var jsonStr = question.QuestionText;
            jsonStr = jsonStr.Replace("\"\"", "\"");
            
            Console.WriteLine($"Raw JSON: {jsonStr}");
            
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonStr);
                var root = doc.RootElement;
                
                var leftItems = new List<string>();
                if (root.TryGetProperty("leftItems", out var leftArray))
                {
                    foreach (var item in leftArray.EnumerateArray())
                        leftItems.Add(item.GetString() ?? "");
                }
                
                var rightItems = new List<string>();
                if (root.TryGetProperty("rightItems", out var rightArray))
                {
                    foreach (var item in rightArray.EnumerateArray())
                        rightItems.Add(item.GetString() ?? "");
                }
                
                var correctMatches = new List<object>();
                if (root.TryGetProperty("correctMatches", out var matchesArray))
                {
                    foreach (var match in matchesArray.EnumerateArray())
                    {
                        var left = match.GetProperty("left").GetInt32();
                        var right = match.GetProperty("right").GetInt32();
                        correctMatches.Add(new { left, right });
                    }
                }
                
                var result = new
                {
                    leftItems = leftItems,
                    rightItems = rightItems,
                    correctMatches = correctMatches
                };
                
                Console.WriteLine($"Parsed: leftItems count = {leftItems.Count}");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
                return Ok(new { leftItems = new List<string>(), rightItems = new List<string>(), correctMatches = new List<object>() });
            }
        }

        [HttpPost("submit-test")]
        public async Task<IActionResult> SubmitTest([FromBody] SubmitTestDto testDto)
        {
            try
            {
                Console.WriteLine("=== SUBMIT TEST ===");
                
                int totalScore = 0;
                int maxScore = 0;
                
                foreach (var answer in testDto.Answers)
                {
                    var question = await _context.Questions
                        .Include(q => q.Answers)
                        .FirstOrDefaultAsync(q => q.QuestionID == answer.QuestionId);
                    
                    if (question == null) continue;
                    
                    maxScore += question.QuestionWeight;
                    
                    var correctAnswer = question.Answers.FirstOrDefault(a => a.IsCorrect);
                    if (correctAnswer?.AnswerID == answer.SelectedAnswerId)
                    {
                        totalScore += question.QuestionWeight;
                    }
                }
                
                Console.WriteLine($"TotalScore: {totalScore}, MaxScore: {maxScore}");
                
                // Проверяем, не выполнено ли уже это задание
                var existingResult = await _context.TaskResults
                    .FirstOrDefaultAsync(tr => tr.UserID == testDto.UserId && tr.TaskID == testDto.TaskId);
                
                if (existingResult != null)
                {
                    return Ok(new { success = true, alreadyCompleted = true });
                }
                
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
                
                // !!! ИСПРАВЛЕНИЕ: НЕ отмечаем урок как полностью пройденный !!!
                // Только обновляем или создаем запись прогресса с правильным процентом
                var allTasksInLesson = await _context.Tasks
                    .Where(t => t.LessonID == testDto.LessonId && t.IsActive)
                    .Select(t => t.TaskID)
                    .ToListAsync();
                
                var completedTaskIds = await _context.TaskResults
                    .Where(tr => tr.UserID == testDto.UserId && allTasksInLesson.Contains(tr.TaskID))
                    .Select(tr => tr.TaskID)
                    .ToListAsync();
                
                // Добавляем текущее задание в список выполненных (если его там еще нет)
                if (!completedTaskIds.Contains(testDto.TaskId))
                {
                    completedTaskIds.Add(testDto.TaskId);
                }
                
                bool allTasksCompleted = allTasksInLesson.Count > 0 && 
                                         allTasksInLesson.All(taskId => completedTaskIds.Contains(taskId));
                
                int progressPercent = allTasksInLesson.Count > 0 
                    ? (completedTaskIds.Count * 100 / allTasksInLesson.Count) 
                    : 0;
                
                var lessonProgress = await _context.LessonProgresses
                    .FirstOrDefaultAsync(lp => lp.UserID == testDto.UserId && lp.LessonID == testDto.LessonId);
                
                if (lessonProgress == null)
                {
                    lessonProgress = new LessonProgress
                    {
                        UserID = testDto.UserId,
                        LessonID = testDto.LessonId,
                        ProgressPercent = progressPercent,
                        IsCompleted = allTasksCompleted,
                        CompletionStatus = allTasksCompleted ? "completed" : "in_progress"
                    };
                    _context.LessonProgresses.Add(lessonProgress);
                }
                else
                {
                    lessonProgress.ProgressPercent = progressPercent;
                    lessonProgress.IsCompleted = allTasksCompleted;
                    lessonProgress.CompletionStatus = allTasksCompleted ? "completed" : "in_progress";
                }
                
                await _context.SaveChangesAsync();
                
                return Ok(new
                {
                    success = true,
                    totalScore = totalScore,
                    maxScore = maxScore,
                    xpGained = totalScore,
                    newTotalXp = rating.Experience,
                    newLevel = rating.CurrentLevel,
                    allTasksCompleted = allTasksCompleted,
                    completedTasksCount = completedTaskIds.Count,
                    totalTasksCount = allTasksInLesson.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("submit-match-result")]
        public async Task<IActionResult> SubmitMatchResult([FromBody] MatchResultDto resultDto)
        {
            try
            {
                var existingResult = await _context.TaskResults
                    .FirstOrDefaultAsync(tr => tr.UserID == resultDto.UserId && tr.TaskID == resultDto.TaskId);
                
                if (existingResult != null)
                {
                    return Ok(new { success = true, alreadyCompleted = true });
                }
                
                var taskResult = new TaskResult
                {
                    UserID = resultDto.UserId,
                    TaskID = resultDto.TaskId,
                    Score = resultDto.Score,
                    AttemptNumber = 1,
                    CompletionStatus = "completed",
                    CompletedAt = DateTime.UtcNow
                };
                _context.TaskResults.Add(taskResult);
                
                var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == resultDto.UserId);
                if (rating == null)
                {
                    rating = new Rating
                    {
                        UserID = resultDto.UserId,
                        TotalScore = resultDto.Score,
                        CurrentLevel = 1,
                        Experience = resultDto.Score,
                        PositionInRating = 0
                    };
                    _context.Ratings.Add(rating);
                }
                else
                {
                    rating.Experience += resultDto.Score;
                    rating.TotalScore += resultDto.Score;
                    rating.CurrentLevel = (rating.Experience / 100) + 1;
                }
                
                // !!! ИСПРАВЛЕНИЕ: НЕ отмечаем урок как полностью пройденный !!!
                var allTasksInLesson = await _context.Tasks
                    .Where(t => t.LessonID == resultDto.LessonId && t.IsActive)
                    .Select(t => t.TaskID)
                    .ToListAsync();
                
                var completedTaskIds = await _context.TaskResults
                    .Where(tr => tr.UserID == resultDto.UserId && allTasksInLesson.Contains(tr.TaskID))
                    .Select(tr => tr.TaskID)
                    .ToListAsync();
                
                if (!completedTaskIds.Contains(resultDto.TaskId))
                {
                    completedTaskIds.Add(resultDto.TaskId);
                }
                
                bool allTasksCompleted = allTasksInLesson.Count > 0 && 
                                         allTasksInLesson.All(taskId => completedTaskIds.Contains(taskId));
                
                int progressPercent = allTasksInLesson.Count > 0 
                    ? (completedTaskIds.Count * 100 / allTasksInLesson.Count) 
                    : 0;
                
                var lessonProgress = await _context.LessonProgresses
                    .FirstOrDefaultAsync(lp => lp.UserID == resultDto.UserId && lp.LessonID == resultDto.LessonId);
                
                if (lessonProgress == null)
                {
                    lessonProgress = new LessonProgress
                    {
                        UserID = resultDto.UserId,
                        LessonID = resultDto.LessonId,
                        ProgressPercent = progressPercent,
                        IsCompleted = allTasksCompleted,
                        CompletionStatus = allTasksCompleted ? "completed" : "in_progress"
                    };
                    _context.LessonProgresses.Add(lessonProgress);
                }
                else
                {
                    lessonProgress.ProgressPercent = progressPercent;
                    lessonProgress.IsCompleted = allTasksCompleted;
                    lessonProgress.CompletionStatus = allTasksCompleted ? "completed" : "in_progress";
                }
                
                await _context.SaveChangesAsync();
                
                return Ok(new
                {
                    success = true,
                    score = resultDto.Score,
                    newTotalXp = rating.Experience,
                    newLevel = rating.CurrentLevel,
                    allTasksCompleted = allTasksCompleted,
                    completedTasksCount = completedTaskIds.Count,
                    totalTasksCount = allTasksInLesson.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("is-completed/{lessonId}/{userId}")]
        public async Task<IActionResult> IsLessonCompleted(int lessonId, int userId)
        {
            // !!! ИСПРАВЛЕНИЕ: Проверяем, все ли задания выполнены !!!
            var allTasks = await _context.Tasks
                .Where(t => t.LessonID == lessonId && t.IsActive)
                .Select(t => t.TaskID)
                .ToListAsync();
            
            if (allTasks.Count == 0)
            {
                return Ok(new { isCompleted = false });
            }
            
            var completedTaskIds = await _context.TaskResults
                .Where(tr => tr.UserID == userId && allTasks.Contains(tr.TaskID))
                .Select(tr => tr.TaskID)
                .ToListAsync();
            
            bool allTasksCompleted = allTasks.All(taskId => completedTaskIds.Contains(taskId));
            
            return Ok(new { isCompleted = allTasksCompleted });
        }

        [HttpGet("{lessonId}/progress/{userId}")]
        public async Task<IActionResult> GetLessonProgress(int lessonId, int userId)
        {
            // Получаем все задания урока
            var allTasks = await _context.Tasks
                .Where(t => t.LessonID == lessonId && t.IsActive)
                .OrderBy(t => t.TaskID)
                .ToListAsync();
            
            // Получаем все выполненные задания пользователя в этом уроке
            var completedTaskIds = await _context.TaskResults
                .Where(tr => tr.UserID == userId && allTasks.Select(t => t.TaskID).Contains(tr.TaskID))
                .Select(tr => tr.TaskID)
                .ToListAsync();
            
            // Находим первое непройденное задание
            var currentTask = allTasks.FirstOrDefault(t => !completedTaskIds.Contains(t.TaskID));
            
            int totalScore = 0;
            int maxScore = 0;
            
            if (currentTask == null && allTasks.Count > 0)
            {
                // Все задания пройдены - показываем общий результат
                var allTaskResults = await _context.TaskResults
                    .Where(tr => tr.UserID == userId && allTasks.Select(t => t.TaskID).Contains(tr.TaskID))
                    .ToListAsync();
                totalScore = allTaskResults.Sum(tr => tr.Score);
                maxScore = allTasks.Sum(t => t.MaxScore);
            }
            else if (currentTask != null)
            {
                // Показываем результат только за текущее задание
                var currentResult = await _context.TaskResults
                    .FirstOrDefaultAsync(tr => tr.UserID == userId && tr.TaskID == currentTask.TaskID);
                totalScore = currentResult?.Score ?? 0;
                maxScore = currentTask.MaxScore;
            }
            
            bool allCompleted = allTasks.Count > 0 && allTasks.All(t => completedTaskIds.Contains(t.TaskID));
            int progressPercent = allTasks.Count > 0 ? (completedTaskIds.Count * 100 / allTasks.Count) : 0;
            
            return Ok(new
            {
                isCompleted = allCompleted,
                progressPercent = progressPercent,
                totalScore = totalScore,
                maxScore = maxScore,
                completedTasksCount = completedTaskIds.Count,
                totalTasksCount = allTasks.Count
            });
        }

        [HttpGet("task/{taskId}/is-completed/{userId}")]
        public async Task<IActionResult> IsTaskCompleted(int taskId, int userId)
        {
            var result = await _context.TaskResults
                .FirstOrDefaultAsync(tr => tr.UserID == userId && tr.TaskID == taskId);
            
            return Ok(new { isCompleted = result != null });
        }

        [HttpPost("mark-completed")]
        public async Task<IActionResult> MarkLessonCompleted([FromBody] MarkCompletedDto dto)
        {
            // Проверяем, все ли задания действительно выполнены
            var allTasks = await _context.Tasks
                .Where(t => t.LessonID == dto.LessonId && t.IsActive)
                .Select(t => t.TaskID)
                .ToListAsync();
            
            var completedTaskIds = await _context.TaskResults
                .Where(tr => tr.UserID == dto.UserId && allTasks.Contains(tr.TaskID))
                .Select(tr => tr.TaskID)
                .ToListAsync();
            
            bool allTasksCompleted = allTasks.Count > 0 && allTasks.All(taskId => completedTaskIds.Contains(taskId));
            
            if (!allTasksCompleted)
            {
                return BadRequest(new { success = false, message = "Не все задания выполнены" });
            }
            
            var lessonProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.UserID == dto.UserId && lp.LessonID == dto.LessonId);
            
            if (lessonProgress == null)
            {
                lessonProgress = new LessonProgress
                {
                    UserID = dto.UserId,
                    LessonID = dto.LessonId,
                    ProgressPercent = 100,
                    IsCompleted = true,
                    CompletionStatus = "completed"
                };
                _context.LessonProgresses.Add(lessonProgress);
            }
            else if (!lessonProgress.IsCompleted)
            {
                lessonProgress.ProgressPercent = 100;
                lessonProgress.IsCompleted = true;
                lessonProgress.CompletionStatus = "completed";
            }
            
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        public class MarkCompletedDto
        {
            public int UserId { get; set; }
            public int LessonId { get; set; }
        }

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

        public class MatchResultDto
        {
            public int UserId { get; set; }
            public int LessonId { get; set; }
            public int TaskId { get; set; }
            public int Score { get; set; }
        }

        public class MatchDataDto
        {
            public List<string> LeftItems { get; set; } = new();
            public List<string> RightItems { get; set; } = new();
            public List<MatchPairDto> CorrectMatches { get; set; } = new();
        }

        public class MatchPairDto
        {
            public int Left { get; set; }
            public int Right { get; set; }
        }
    }
}