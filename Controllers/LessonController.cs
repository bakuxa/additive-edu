using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;
using Task = AdditiveEdu.Models.Task;
using Type = AdditiveEdu.Models.Type;
using System.Text.Json;

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
            // Получаем все активные задания урока
            var allTasks = await _context.Tasks
                .Where(t => t.LessonID == lessonId && t.IsActive)
                .Select(t => t.TaskID)
                .ToListAsync();
            
            // Если в уроке нет заданий - проверяем, есть ли запись в LessonProgress
            if (allTasks.Count == 0)
            {
                // Урок без заданий считается пройденным, только если есть запись в LessonProgress
                var lessonProgress = await _context.LessonProgresses
                    .FirstOrDefaultAsync(lp => lp.UserID == userId && lp.LessonID == lessonId);
                
                return Ok(new { isCompleted = lessonProgress?.IsCompleted ?? false });
            }
            
            // Если задания есть - проверяем, все ли они выполнены
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
            // Проверяем, есть ли задания в уроке
            var allTasks = await _context.Tasks
                .Where(t => t.LessonID == dto.LessonId && t.IsActive)
                .Select(t => t.TaskID)
                .ToListAsync();
            
            // Если задания есть - проверяем, все ли они выполнены
            if (allTasks.Count > 0)
            {
                var completedTaskIds = await _context.TaskResults
                    .Where(tr => tr.UserID == dto.UserId && allTasks.Contains(tr.TaskID))
                    .Select(tr => tr.TaskID)
                    .ToListAsync();
                
                bool allTasksCompleted = allTasks.All(taskId => completedTaskIds.Contains(taskId));
                
                if (!allTasksCompleted)
                {
                    return BadRequest(new { success = false, message = "Не все задания выполнены" });
                }
            }
            
            // Если заданий нет или все задания выполнены - отмечаем урок как пройденный
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

// GET: api/lesson/task/{taskId}/quest-stages
[HttpGet("task/{taskId}/quest-stages")]
public async Task<IActionResult> GetQuestStages(int taskId)
{
    var stagesFromDb = await _context.QuestStages
        .Where(qs => qs.TaskID == taskId)
        .OrderBy(qs => qs.StageOrder)
        .Select(qs => new
        {
            qs.QuestStageID,
            qs.StageTitle,
            qs.StageDescription,
            qs.StageOrder
        })
        .ToListAsync();

    if (stagesFromDb == null || stagesFromDb.Count == 0)
        return NotFound(new { message = "Этапы квеста не найдены" });

    var stages = stagesFromDb.Select(qs => new
    {
        qs.QuestStageID,
        qs.StageTitle,
        qs.StageOrder,
        StageData = JsonSerializer.Deserialize<object>(qs.StageDescription ?? "{}")
    }).ToList();

    return Ok(stages);
}

// GET: api/lesson/quest/progress/{taskId}/{userId}
[HttpGet("quest/progress/{taskId}/{userId}")]
public async Task<IActionResult> GetQuestProgress(int taskId, int userId)
{
    var taskResult = await _context.TaskResults
        .FirstOrDefaultAsync(tr => tr.TaskID == taskId && tr.UserID == userId);
    
    if (taskResult == null)
    {
        return Ok(new { 
            isStarted = false,
            completedStages = new List<int>(),
            currentStage = 1,
            totalScore = 0
        });
    }
    
    if (!string.IsNullOrEmpty(taskResult.CompletionStatus) && taskResult.CompletionStatus.StartsWith("{"))
    {
        try
        {
            var progress = JsonSerializer.Deserialize<QuestProgressDto>(taskResult.CompletionStatus);
            return Ok(new
            {
                isStarted = true,
                isCompleted = false,
                completedStages = progress?.CompletedStages ?? new List<int>(),
                currentStage = progress?.CurrentStage ?? 1,
                totalScore = taskResult.Score
            });
        }
        catch
        {
            return Ok(new { 
                isStarted = true,
                completedStages = new List<int>(),
                currentStage = 1,
                totalScore = taskResult.Score
            });
        }
    }
    
    if (taskResult.CompletionStatus == "completed")
    {
        // Получаем количество этапов отдельным запросом
        var allStagesCount = await _context.QuestStages
            .Where(qs => qs.TaskID == taskId)
            .CountAsync();
        
        return Ok(new
        {
            isStarted = true,
            isCompleted = true,
            completedStages = Enumerable.Range(1, allStagesCount).ToList(),
            currentStage = allStagesCount + 1,
            totalScore = taskResult.Score
        });
    }
    
    return Ok(new { 
        isStarted = true,
        completedStages = new List<int>(),
        currentStage = 1,
        totalScore = taskResult.Score
    });
}
// POST: api/lesson/quest/check-stage
[HttpPost("quest/check-stage")]
public async Task<IActionResult> CheckQuestStage([FromBody] CheckQuestStageDto dto)
{
    try
    {
        var stage = await _context.QuestStages
            .FirstOrDefaultAsync(qs => qs.QuestStageID == dto.StageId);
        
        if (stage == null)
            return NotFound(new { success = false, message = "Этап не найден" });
        
        // Ручной парсинг JSON из StageDescription
        using var doc = JsonDocument.Parse(stage.StageDescription);
        var root = doc.RootElement;
        
        string type = root.GetProperty("type").GetString();
        int points = root.GetProperty("points").GetInt32();
        
        bool isCorrect = false;
        int earnedPoints = 0;
        object correctAnswerObj = null;
        
        switch (type)
        {
            case "single_choice":
                // Получаем правильный ответ
                int correctAnswer = root.GetProperty("correctAnswer").GetInt32();
                correctAnswerObj = root.GetProperty("options")[correctAnswer].GetString();
                
                // Получаем ответ пользователя
                int selectedIndex = -1;
                if (dto.Answer != null)
                {
                    if (dto.Answer is JsonElement jsonElem)
                    {
                        if (jsonElem.ValueKind == JsonValueKind.Object)
                        {
                            // Если пришло как { value: 0 }
                            if (jsonElem.TryGetProperty("value", out var valueProp))
                                selectedIndex = valueProp.GetInt32();
                        }
                        else if (jsonElem.ValueKind == JsonValueKind.Number)
                            selectedIndex = jsonElem.GetInt32();
                        else if (jsonElem.ValueKind == JsonValueKind.String)
                            selectedIndex = int.Parse(jsonElem.GetString());
                    }
                    else if (dto.Answer is int intVal)
                        selectedIndex = intVal;
                    else if (dto.Answer is string strVal)
                        selectedIndex = int.Parse(strVal);
                }
                
                isCorrect = (selectedIndex == correctAnswer);
                Console.WriteLine($"Single choice: selected={selectedIndex}, correct={correctAnswer}, isCorrect={isCorrect}");
                break;
                
            case "true_false":
                bool correctValue = root.GetProperty("correctAnswer").GetBoolean();
                correctAnswerObj = correctValue ? "Верно" : "Неверно";
                
                bool userValue = false;
                if (dto.Answer != null)
                {
                    if (dto.Answer is JsonElement jsonElem)
                    {
                        if (jsonElem.ValueKind == JsonValueKind.True || jsonElem.ValueKind == JsonValueKind.False)
                            userValue = jsonElem.GetBoolean();
                        else if (jsonElem.ValueKind == JsonValueKind.String)
                            userValue = bool.Parse(jsonElem.GetString());
                    }
                    else if (dto.Answer is bool bVal)
                        userValue = bVal;
                }
                
                isCorrect = (userValue == correctValue);
                break;
                
            case "text_input":
            case "fill_blank":
                string correctStr = root.GetProperty("correctAnswer").GetString();
                correctAnswerObj = correctStr;
                
                if (!string.IsNullOrEmpty(dto.TextAnswer))
                {
                    isCorrect = dto.TextAnswer.Trim().Equals(correctStr, StringComparison.OrdinalIgnoreCase);
                    
                    // Проверка на альтернативные ответы
                    if (!isCorrect && root.TryGetProperty("alternatives", out var altArray))
                    {
                        foreach (var alt in altArray.EnumerateArray())
                        {
                            if (dto.TextAnswer.Trim().Equals(alt.GetString(), StringComparison.OrdinalIgnoreCase))
                            {
                                isCorrect = true;
                                break;
                            }
                        }
                    }
                }
                break;
                
            case "multiple_choice":
                var correctAnswers = new HashSet<int>();
                using (var arrEnum = root.GetProperty("correctAnswers").EnumerateArray())
                {
                    foreach (var item in arrEnum)
                        correctAnswers.Add(item.GetInt32());
                }
                correctAnswerObj = string.Join(", ", correctAnswers.Select(i => root.GetProperty("options")[i].GetString()));
                
                if (dto.SelectedIndices != null)
                {
                    var selectedSet = new HashSet<int>(dto.SelectedIndices);
                    isCorrect = selectedSet.SetEquals(correctAnswers);
                }
                break;
                
            case "sequence":
                var correctOrder = new List<int>();
                using (var orderEnum = root.GetProperty("correctOrder").EnumerateArray())
                {
                    foreach (var item in orderEnum)
                        correctOrder.Add(item.GetInt32());
                }
                correctAnswerObj = string.Join(" → ", correctOrder.Select(i => root.GetProperty("items")[i].GetString()));
                
                if (dto.OrderIndices != null)
                {
                    isCorrect = dto.OrderIndices.SequenceEqual(correctOrder);
                }
                break;
        }
        
        if (isCorrect)
        {
            earnedPoints = points;
        }
        
        return Ok(new
        {
            success = true,
            isCorrect = isCorrect,
            earnedPoints = earnedPoints,
            correctAnswer = correctAnswerObj,
            explanation = isCorrect ? "Правильно!" : "Неправильно",
            pointsPossible = points
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}
// POST: api/lesson/quest/save-progress
[HttpPost("quest/save-progress")]
public async Task<IActionResult> SaveQuestProgress([FromBody] SaveQuestProgressDto dto)
{
    try
    {
        var allStages = await _context.QuestStages
            .Where(qs => qs.TaskID == dto.TaskId)
            .OrderBy(qs => qs.StageOrder)
            .ToListAsync();
        
        var existingResult = await _context.TaskResults
            .FirstOrDefaultAsync(tr => tr.TaskID == dto.TaskId && tr.UserID == dto.UserId);
        
        if (existingResult != null && existingResult.CompletionStatus == "completed")
        {
            return Ok(new { success = true, alreadyCompleted = true });
        }
        
        var progressJson = JsonSerializer.Serialize(new
        {
            completedStages = dto.CompletedStages,
            currentStage = dto.CurrentStage,
            stageScores = dto.StageScores
        });
        
        int totalScore = dto.StageScores?.Sum() ?? 0;
        bool isCompleted = dto.CompletedStages.Count == allStages.Count;
        
        if (existingResult == null)
        {
            existingResult = new TaskResult
            {
                UserID = dto.UserId,
                TaskID = dto.TaskId,
                Score = totalScore,
                AttemptNumber = 1,
                CompletionStatus = isCompleted ? "completed" : progressJson,
                CompletedAt = isCompleted ? DateTime.UtcNow : null
            };
            _context.TaskResults.Add(existingResult);
        }
        else
        {
            existingResult.Score = totalScore;
            existingResult.CompletionStatus = isCompleted ? "completed" : progressJson;
            if (isCompleted)
                existingResult.CompletedAt = DateTime.UtcNow;
        }
        
        // ВСЕГДА обновляем рейтинг при завершении квеста
        if (isCompleted)
        {
            var rating = await _context.Ratings.FirstOrDefaultAsync(r => r.UserID == dto.UserId);
            if (rating == null)
            {
                rating = new Rating
                {
                    UserID = dto.UserId,
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
            
            // Обновляем также прогресс урока
            var lessonProgress = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.UserID == dto.UserId && lp.LessonID == dto.LessonId);
            
            if (lessonProgress != null && !lessonProgress.IsCompleted)
            {
                var allTasksInLesson = await _context.Tasks
                    .Where(t => t.LessonID == dto.LessonId && t.IsActive)
                    .Select(t => t.TaskID)
                    .ToListAsync();
                
                var completedTaskIds = await _context.TaskResults
                    .Where(tr => tr.UserID == dto.UserId && allTasksInLesson.Contains(tr.TaskID))
                    .Select(tr => tr.TaskID)
                    .ToListAsync();
                
                bool allTasksCompleted = allTasksInLesson.Count > 0 && 
                                         allTasksInLesson.All(taskId => completedTaskIds.Contains(taskId));
                
                int progressPercent = allTasksInLesson.Count > 0 
                    ? (completedTaskIds.Count * 100 / allTasksInLesson.Count) 
                    : 0;
                
                lessonProgress.ProgressPercent = progressPercent;
                lessonProgress.IsCompleted = allTasksCompleted;
                lessonProgress.CompletionStatus = allTasksCompleted ? "completed" : "in_progress";
            }
        }
        
        await _context.SaveChangesAsync();
        
        // Возвращаем totalScore, чтобы фронтенд мог обновить отображение
        return Ok(new
        {
            success = true,
            totalScore = totalScore,
            isCompleted = isCompleted,
            xpGained = isCompleted ? totalScore : 0
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}

// DTO классы для квеста (добавьте в конец файла LessonController.cs, внутри namespace, перед последней скобкой)
public class QuestProgressDto
{
    public List<int> CompletedStages { get; set; } = new();
    public int CurrentStage { get; set; } = 1;
    public Dictionary<int, int> StageScores { get; set; } = new();
}

public class CheckQuestStageDto
{
    public int StageId { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public object? Answer { get; set; }
    public string? TextAnswer { get; set; }
    public List<int>? SelectedIndices { get; set; }
    public List<int>? OrderIndices { get; set; }
}

public class SaveQuestProgressDto
{
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public int LessonId { get; set; }
    public List<int> CompletedStages { get; set; } = new();
    public int CurrentStage { get; set; }
    public List<int>? StageScores { get; set; }
}

public class QuestStageData
{
    public string Type { get; set; } = "";
    public string Question { get; set; } = "";
    public List<string>? Options { get; set; }
    public object? CorrectAnswer { get; set; }
    public List<int>? CorrectAnswers { get; set; }
    public List<int>? CorrectOrder { get; set; }
    public List<string>? Items { get; set; }
    public List<string>? Alternatives { get; set; }
    public int Points { get; set; }
    public int Tolerance { get; set; }
    public string? Hint { get; set; }
    public string? Explanation { get; set; }
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