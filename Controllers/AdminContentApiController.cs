using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AdditiveEdu.Data;
using AdditiveEdu.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AdditiveEdu.Controllers
{
    [ApiController]
    [Route("api/admin/content")]
    public class AdminContentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminContentApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========== СТАТИСТИКА ==========
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var modulesCount = await _context.Modules.CountAsync();
                var lessonsCount = await _context.Lessons.CountAsync();
                var tasksCount = await _context.Tasks.CountAsync();
                var activeTasksCount = await _context.Tasks.CountAsync(t => t.IsActive);

                return Ok(new { modulesCount, lessonsCount, tasksCount, activeTasksCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== МОДУЛИ ==========
        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            try
            {
                var modules = await _context.Modules
                    .OrderBy(m => m.ModuleNumber)
                    .Select(m => new
                    {
                        moduleId = m.ModuleID,
                        moduleNumber = m.ModuleNumber,
                        moduleTitle = m.ModuleTitle,
                        moduleDescription = m.ModuleDescription,
                        difficultyLevel = m.DifficultyLevel,
                        isPublished = m.IsPublished,
                        lessons = _context.Lessons
                            .Where(l => l.ModuleID == m.ModuleID)
                            .OrderBy(l => l.LessonOrder)
                            .Select(l => new
                            {
                                lessonId = l.LessonID,
                                lessonTitle = l.LessonTitle,
                                lessonDescription = l.LessonDescription,
                                lessonOrder = l.LessonOrder,
                                theoryContent = l.TheoryContent,
                                tasks = _context.Tasks
                                    .Where(t => t.LessonID == l.LessonID)
                                    .Select(t => new
                                    {
                                        taskId = t.TaskID,
                                        taskTitle = t.TaskTitle,
                                        taskDescription = t.TaskDescription,
                                        difficultyLevel = t.DifficultyLevel,
                                        maxScore = t.MaxScore,
                                        isActive = t.IsActive,
                                        typeName = _context.Types
                                            .Where(typ => typ.TypeID == t.TypeID)
                                            .Select(typ => typ.TypeName)
                                            .FirstOrDefault() ?? "Неизвестный тип"
                                    })
                                    .ToList()
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(modules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("modules/{id}")]
        public async Task<IActionResult> GetModule(int id)
        {
            try
            {
                var module = await _context.Modules
                    .Where(m => m.ModuleID == id)
                    .Select(m => new
                    {
                        moduleId = m.ModuleID,
                        moduleNumber = m.ModuleNumber,
                        moduleTitle = m.ModuleTitle,
                        moduleDescription = m.ModuleDescription,
                        difficultyLevel = m.DifficultyLevel,
                        isPublished = m.IsPublished
                    })
                    .FirstOrDefaultAsync();

                if (module == null) return NotFound(new { message = "Модуль не найден" });
                return Ok(module);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("modules")]
        public async Task<IActionResult> CreateModule([FromBody] CreateModuleDto dto)
        {
            try
            {
                var module = new Module
                {
                    ModuleNumber = dto.ModuleNumber,
                    ModuleTitle = dto.ModuleTitle,
                    ModuleDescription = dto.ModuleDescription ?? "",
                    DifficultyLevel = dto.DifficultyLevel,
                    IsPublished = dto.IsPublished
                };
                _context.Modules.Add(module);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, moduleId = module.ModuleID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("modules/{id}")]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] CreateModuleDto dto)
        {
            try
            {
                var module = await _context.Modules.FindAsync(id);
                if (module == null) return NotFound(new { message = "Модуль не найден" });

                module.ModuleNumber = dto.ModuleNumber;
                module.ModuleTitle = dto.ModuleTitle;
                module.ModuleDescription = dto.ModuleDescription ?? "";
                module.DifficultyLevel = dto.DifficultyLevel;
                module.IsPublished = dto.IsPublished;
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("modules/{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            try
            {
                var module = await _context.Modules.FindAsync(id);
                if (module == null) return NotFound(new { message = "Модуль не найден" });

                var lessons = await _context.Lessons.Where(l => l.ModuleID == id).ToListAsync();
                foreach (var lesson in lessons)
                {
                    var tasks = await _context.Tasks.Where(t => t.LessonID == lesson.LessonID).ToListAsync();
                    foreach (var task in tasks)
                    {
                        var questions = await _context.Questions.Where(q => q.TaskID == task.TaskID).ToListAsync();
                        foreach (var question in questions)
                        {
                            var answers = await _context.Answers.Where(a => a.QuestionID == question.QuestionID).ToListAsync();
                            _context.Answers.RemoveRange(answers);
                        }
                        _context.Questions.RemoveRange(questions);

                        var questStages = await _context.QuestStages.Where(qs => qs.TaskID == task.TaskID).ToListAsync();
                        _context.QuestStages.RemoveRange(questStages);
                    }
                    _context.Tasks.RemoveRange(tasks);
                }
                _context.Lessons.RemoveRange(lessons);
                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== УРОКИ ==========
        [HttpGet("lessons/by-module/{moduleId}")]
        public async Task<IActionResult> GetLessonsByModule(int moduleId)
        {
            try
            {
                var lessons = await _context.Lessons
                    .Where(l => l.ModuleID == moduleId)
                    .OrderBy(l => l.LessonOrder)
                    .Select(l => new
                    {
                        lessonId = l.LessonID,
                        lessonTitle = l.LessonTitle,
                        lessonDescription = l.LessonDescription,
                        lessonOrder = l.LessonOrder,
                        theoryContent = l.TheoryContent
                    })
                    .ToListAsync();

                return Ok(lessons);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("lessons/{id}")]
        public async Task<IActionResult> GetLesson(int id)
        {
            try
            {
                var lesson = await _context.Lessons
                    .Where(l => l.LessonID == id)
                    .Select(l => new
                    {
                        lessonId = l.LessonID,
                        moduleId = l.ModuleID,
                        lessonTitle = l.LessonTitle,
                        lessonDescription = l.LessonDescription,
                        lessonOrder = l.LessonOrder,
                        theoryContent = l.TheoryContent
                    })
                    .FirstOrDefaultAsync();

                if (lesson == null) return NotFound(new { message = "Урок не найден" });
                return Ok(lesson);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("lessons")]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
        {
            try
            {
                var lesson = new Lesson
                {
                    ModuleID = dto.ModuleId,
                    LessonTitle = dto.LessonTitle,
                    LessonDescription = dto.LessonDescription ?? "",
                    LessonOrder = dto.LessonOrder,
                    TheoryContent = dto.TheoryContent ?? ""
                };
                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, lessonId = lesson.LessonID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("lessons/{id}")]
        public async Task<IActionResult> UpdateLesson(int id, [FromBody] CreateLessonDto dto)
        {
            try
            {
                var lesson = await _context.Lessons.FindAsync(id);
                if (lesson == null) return NotFound(new { message = "Урок не найден" });

                lesson.ModuleID = dto.ModuleId;
                lesson.LessonTitle = dto.LessonTitle;
                lesson.LessonDescription = dto.LessonDescription ?? "";
                lesson.LessonOrder = dto.LessonOrder;
                lesson.TheoryContent = dto.TheoryContent ?? "";
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("lessons/{id}")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            try
            {
                var lesson = await _context.Lessons.FindAsync(id);
                if (lesson == null) return NotFound(new { message = "Урок не найден" });

                var tasks = await _context.Tasks.Where(t => t.LessonID == id).ToListAsync();
                foreach (var task in tasks)
                {
                    var questions = await _context.Questions.Where(q => q.TaskID == task.TaskID).ToListAsync();
                    foreach (var question in questions)
                    {
                        var answers = await _context.Answers.Where(a => a.QuestionID == question.QuestionID).ToListAsync();
                        _context.Answers.RemoveRange(answers);
                    }
                    _context.Questions.RemoveRange(questions);

                    var questStages = await _context.QuestStages.Where(qs => qs.TaskID == task.TaskID).ToListAsync();
                    _context.QuestStages.RemoveRange(questStages);
                }
                _context.Tasks.RemoveRange(tasks);
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== ЗАДАНИЯ ==========
        [HttpGet("tasks/by-lesson/{lessonId}")]
        public async Task<IActionResult> GetTasksByLesson(int lessonId)
        {
            try
            {
                var tasks = await _context.Tasks
                    .Where(t => t.LessonID == lessonId)
                    .Select(t => new
                    {
                        taskId = t.TaskID,
                        taskTitle = t.TaskTitle,
                        taskDescription = t.TaskDescription,
                        difficultyLevel = t.DifficultyLevel,
                        maxScore = t.MaxScore,
                        isActive = t.IsActive,
                        typeName = _context.Types
                            .Where(typ => typ.TypeID == t.TypeID)
                            .Select(typ => typ.TypeName)
                            .FirstOrDefault() ?? "Неизвестный тип"
                    })
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("tasks/{id}")]
        public async Task<IActionResult> GetTask(int id)
        {
            try
            {
                var task = await _context.Tasks
                    .Where(t => t.TaskID == id)
                    .Select(t => new
                    {
                        taskId = t.TaskID,
                        lessonId = t.LessonID,
                        taskTitle = t.TaskTitle,
                        taskDescription = t.TaskDescription,
                        difficultyLevel = t.DifficultyLevel,
                        maxScore = t.MaxScore,
                        isActive = t.IsActive,
                        typeName = _context.Types
                            .Where(typ => typ.TypeID == t.TypeID)
                            .Select(typ => typ.TypeName)
                            .FirstOrDefault() ?? "Неизвестный тип"
                    })
                    .FirstOrDefaultAsync();

                if (task == null) return NotFound(new { message = "Задание не найдено" });
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("tasks")]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto dto)
        {
            try
            {
                var type = await _context.Types.FirstOrDefaultAsync(t => t.TypeName == dto.TypeName);
                if (type == null)
                {
                    type = new AdditiveEdu.Models.Type { TypeName = dto.TypeName };
                    _context.Types.Add(type);
                    await _context.SaveChangesAsync();
                }

                var task = new AdditiveEdu.Models.Task
                {
                    LessonID = dto.LessonId,
                    TypeID = type.TypeID,
                    TaskTitle = dto.TaskTitle,
                    TaskDescription = dto.TaskDescription ?? "",
                    DifficultyLevel = dto.DifficultyLevel,
                    MaxScore = dto.MaxScore,
                    IsActive = dto.IsActive
                };
                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, taskId = task.TaskID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("tasks/{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] CreateTaskDto dto)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null) return NotFound(new { message = "Задание не найдено" });

                var type = await _context.Types.FirstOrDefaultAsync(t => t.TypeName == dto.TypeName);
                if (type == null)
                {
                    type = new AdditiveEdu.Models.Type { TypeName = dto.TypeName };
                    _context.Types.Add(type);
                    await _context.SaveChangesAsync();
                }

                task.LessonID = dto.LessonId;
                task.TypeID = type.TypeID;
                task.TaskTitle = dto.TaskTitle;
                task.TaskDescription = dto.TaskDescription ?? "";
                task.DifficultyLevel = dto.DifficultyLevel;
                task.MaxScore = dto.MaxScore;
                task.IsActive = dto.IsActive;
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("tasks/{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null) return NotFound(new { message = "Задание не найдено" });

                var questions = await _context.Questions.Where(q => q.TaskID == id).ToListAsync();
                foreach (var question in questions)
                {
                    var answers = await _context.Answers.Where(a => a.QuestionID == question.QuestionID).ToListAsync();
                    _context.Answers.RemoveRange(answers);
                }
                _context.Questions.RemoveRange(questions);

                var questStages = await _context.QuestStages.Where(qs => qs.TaskID == id).ToListAsync();
                _context.QuestStages.RemoveRange(questStages);

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== ТИПЫ ЗАДАНИЙ ==========
        [HttpGet("types")]
        public async Task<IActionResult> GetTypes()
        {
            try
            {
                var types = await _context.Types
                    .Select(t => new { typeId = t.TypeID, typeName = t.TypeName })
                    .ToListAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ========== ВОПРОСЫ ТЕСТА ==========
        [HttpGet("questions/by-task/{taskId}")]
        public async Task<IActionResult> GetQuestionsByTask(int taskId)
        {
            try
            {
                var questions = await _context.Questions
                    .Where(q => q.TaskID == taskId)
                    .OrderBy(q => q.QuestionOrder)
                    .Select(q => new
                    {
                        questionId = q.QuestionID,
                        questionText = q.QuestionText,
                        questionLevel = q.QuestionLevel,
                        questionOrder = q.QuestionOrder,
                        questionWeight = q.QuestionWeight,
                        answers = _context.Answers
                            .Where(a => a.QuestionID == q.QuestionID)
                            .Select(a => new
                            {
                                answerId = a.AnswerID,
                                answerText = a.AnswerText,
                                isCorrect = a.IsCorrect
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(questions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("questions/{id}")]
        public async Task<IActionResult> GetQuestion(int id)
        {
            try
            {
                var question = await _context.Questions
                    .Where(q => q.QuestionID == id)
                    .Select(q => new
                    {
                        questionId = q.QuestionID,
                        taskId = q.TaskID,
                        questionText = q.QuestionText,
                        questionLevel = q.QuestionLevel,
                        questionOrder = q.QuestionOrder,
                        questionWeight = q.QuestionWeight,
                        answers = _context.Answers
                            .Where(a => a.QuestionID == q.QuestionID)
                            .Select(a => new
                            {
                                answerId = a.AnswerID,
                                answerText = a.AnswerText,
                                isCorrect = a.IsCorrect
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (question == null) return NotFound(new { message = "Вопрос не найден" });
                return Ok(question);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("questions")]
        public async Task<IActionResult> CreateQuestion([FromBody] CreateQuestionDto dto)
        {
            try
            {
                var question = new Question
                {
                    TaskID = dto.TaskId,
                    QuestionText = dto.QuestionText,
                    QuestionLevel = dto.QuestionLevel,
                    QuestionOrder = dto.QuestionOrder,
                    QuestionWeight = dto.QuestionWeight
                };
                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                if (dto.Answers != null && dto.Answers.Any())
                {
                    foreach (var answerDto in dto.Answers)
                    {
                        var answer = new Answer
                        {
                            QuestionID = question.QuestionID,
                            AnswerText = answerDto.AnswerText,
                            IsCorrect = answerDto.IsCorrect
                        };
                        _context.Answers.Add(answer);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true, questionId = question.QuestionID });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("questions/{id}")]
        public async Task<IActionResult> UpdateQuestion(int id, [FromBody] CreateQuestionDto dto)
        {
            try
            {
                var question = await _context.Questions.FindAsync(id);
                if (question == null) return NotFound(new { message = "Вопрос не найден" });

                question.QuestionText = dto.QuestionText;
                question.QuestionLevel = dto.QuestionLevel;
                question.QuestionOrder = dto.QuestionOrder;
                question.QuestionWeight = dto.QuestionWeight;
                await _context.SaveChangesAsync();

                var existingAnswers = await _context.Answers.Where(a => a.QuestionID == id).ToListAsync();
                _context.Answers.RemoveRange(existingAnswers);
                await _context.SaveChangesAsync();

                if (dto.Answers != null && dto.Answers.Any())
                {
                    foreach (var answerDto in dto.Answers)
                    {
                        var answer = new Answer
                        {
                            QuestionID = question.QuestionID,
                            AnswerText = answerDto.AnswerText,
                            IsCorrect = answerDto.IsCorrect
                        };
                        _context.Answers.Add(answer);
                    }
                    await _context.SaveChangesAsync();
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("questions/{id}")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            try
            {
                var question = await _context.Questions.FindAsync(id);
                if (question == null) return NotFound(new { message = "Вопрос не найден" });

                var answers = await _context.Answers.Where(a => a.QuestionID == id).ToListAsync();
                _context.Answers.RemoveRange(answers);
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

               // ========== ЭТАПЫ КВЕСТА ==========
        [HttpGet("quest-stages/by-task/{taskId}")]
        public async Task<IActionResult> GetQuestStagesByTask(int taskId)
        {
            try
            {
                var stages = await _context.QuestStages
                    .Where(qs => qs.TaskID == taskId)
                    .OrderBy(qs => qs.StageOrder)
                    .Select(qs => new
                    {
                        questStageId = qs.QuestStageID,
                        taskId = qs.TaskID,
                        stageTitle = qs.StageTitle,
                        stageDescription = qs.StageDescription,
                        stageOrder = qs.StageOrder,
                        successCondition = qs.SuccessCondition
                    })
                    .ToListAsync();

                return Ok(stages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("quest-stages/{id}")]
        public async Task<IActionResult> GetQuestStage(int id)
        {
            try
            {
                var stage = await _context.QuestStages
                    .Where(qs => qs.QuestStageID == id)
                    .Select(qs => new
                    {
                        questStageId = qs.QuestStageID,
                        taskId = qs.TaskID,
                        stageTitle = qs.StageTitle,
                        stageDescription = qs.StageDescription,
                        stageOrder = qs.StageOrder,
                        successCondition = qs.SuccessCondition
                    })
                    .FirstOrDefaultAsync();

                if (stage == null) return NotFound(new { message = "Этап не найден" });
                return Ok(stage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("quest-stages")]
        public async Task<IActionResult> CreateQuestStage([FromBody] CreateQuestStageDto dto)
        {
            try
            {
                Console.WriteLine($"Создание этапа квеста: TaskId={dto.TaskId}, Title={dto.StageTitle}");
                
                var stage = new QuestStage
                {
                    TaskID = dto.TaskId,
                    StageTitle = dto.StageTitle,
                    StageDescription = dto.StageDescription ?? "",
                    StageOrder = dto.StageOrder,
                    SuccessCondition = dto.SuccessCondition ?? ""
                };
                _context.QuestStages.Add(stage);
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"Этап создан с ID: {stage.QuestStageID}");
                return Ok(new { success = true, questStageId = stage.QuestStageID });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка создания этапа: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPut("quest-stages/{id}")]
        public async Task<IActionResult> UpdateQuestStage(int id, [FromBody] CreateQuestStageDto dto)
        {
            try
            {
                Console.WriteLine($"Обновление этапа квеста: ID={id}, TaskId={dto.TaskId}, Title={dto.StageTitle}");
                
                var stage = await _context.QuestStages.FindAsync(id);
                if (stage == null) return NotFound(new { message = "Этап не найден" });

                stage.TaskID = dto.TaskId;
                stage.StageTitle = dto.StageTitle;
                stage.StageDescription = dto.StageDescription ?? "";
                stage.StageOrder = dto.StageOrder;
                stage.SuccessCondition = dto.SuccessCondition ?? "";
                await _context.SaveChangesAsync();
                
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обновления этапа: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpDelete("quest-stages/{id}")]
        public async Task<IActionResult> DeleteQuestStage(int id)
        {
            try
            {
                var stage = await _context.QuestStages.FindAsync(id);
                if (stage == null) return NotFound(new { message = "Этап не найден" });

                _context.QuestStages.Remove(stage);
                await _context.SaveChangesAsync();
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

// ========== СОПОСТАВЛЕНИЕ (через Question.QuestionText) ==========
        [HttpGet("matching/by-task/{taskId}")]
        public async Task<IActionResult> GetMatchingData(int taskId)
        {
            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.TaskID == taskId);
            
            if (question == null || string.IsNullOrEmpty(question.QuestionText))
            {
                return Ok(new { leftItems = new List<string>(), rightItems = new List<string>(), correctMatches = new List<object>() });
            }
            
            try
            {
                // Декодируем JSON строку (убираем escape последовательности)
                string jsonString = question.QuestionText;
                if (jsonString.Contains("\\u"))
                {
                    // Декодируем Unicode escape последовательности
                    jsonString = System.Text.RegularExpressions.Regex.Unescape(jsonString);
                }
                
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;
                
                var leftItems = new List<string>();
                // Пробуем получить leftItems в разных регистрах
                if (root.TryGetProperty("leftItems", out var leftArray))
                {
                    foreach (var item in leftArray.EnumerateArray())
                        leftItems.Add(item.GetString() ?? "");
                }
                else if (root.TryGetProperty("LeftItems", out var leftArray2))
                {
                    foreach (var item in leftArray2.EnumerateArray())
                        leftItems.Add(item.GetString() ?? "");
                }
                
                var rightItems = new List<string>();
                if (root.TryGetProperty("rightItems", out var rightArray))
                {
                    foreach (var item in rightArray.EnumerateArray())
                        rightItems.Add(item.GetString() ?? "");
                }
                else if (root.TryGetProperty("RightItems", out var rightArray2))
                {
                    foreach (var item in rightArray2.EnumerateArray())
                        rightItems.Add(item.GetString() ?? "");
                }
                
                var correctMatches = new List<object>();
                if (root.TryGetProperty("correctMatches", out var matchesArray))
                {
                    foreach (var match in matchesArray.EnumerateArray())
                    {
                        int left = 0, right = 0;
                        if (match.TryGetProperty("left", out var leftProp))
                            left = leftProp.GetInt32();
                        else if (match.TryGetProperty("Left", out var leftProp2))
                            left = leftProp2.GetInt32();
                            
                        if (match.TryGetProperty("right", out var rightProp))
                            right = rightProp.GetInt32();
                        else if (match.TryGetProperty("Right", out var rightProp2))
                            right = rightProp2.GetInt32();
                            
                        correctMatches.Add(new { left, right });
                    }
                }
                else if (root.TryGetProperty("CorrectMatches", out var matchesArray2))
                {
                    foreach (var match in matchesArray2.EnumerateArray())
                    {
                        int left = 0, right = 0;
                        if (match.TryGetProperty("left", out var leftProp))
                            left = leftProp.GetInt32();
                        else if (match.TryGetProperty("Left", out var leftProp2))
                            left = leftProp2.GetInt32();
                            
                        if (match.TryGetProperty("right", out var rightProp))
                            right = rightProp.GetInt32();
                        else if (match.TryGetProperty("Right", out var rightProp2))
                            right = rightProp2.GetInt32();
                            
                        correctMatches.Add(new { left, right });
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Matching data loaded: leftItems={leftItems.Count}, rightItems={rightItems.Count}");
                
                return Ok(new { leftItems, rightItems, correctMatches });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing matching data: {ex.Message}");
                return Ok(new { leftItems = new List<string>(), rightItems = new List<string>(), correctMatches = new List<object>() });
            }
        }
        
        [HttpPost("matching/by-task/{taskId}")]
        public async Task<IActionResult> SaveMatchingData(int taskId, [FromBody] SaveMatchingDataDto dto)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Saving matching data for task {taskId}: leftItems={dto.LeftItems?.Count ?? 0}, rightItems={dto.RightItems?.Count ?? 0}");
                
                var question = await _context.Questions
                    .FirstOrDefaultAsync(q => q.TaskID == taskId);
                
                // Сохраняем с маленькими буквами для единообразия
                var jsonData = JsonSerializer.Serialize(new
                {
                    leftItems = dto.LeftItems ?? new List<string>(),
                    rightItems = dto.RightItems ?? new List<string>(),
                    correctMatches = (dto.CorrectMatches ?? new List<MatchPairDto>()).Select(m => new { left = m.Left, right = m.Right })
                });
                
                System.Diagnostics.Debug.WriteLine($"Saving JSON: {jsonData}");
                
                if (question == null)
                {
                    question = new Question
                    {
                        TaskID = taskId,
                        QuestionText = jsonData,
                        QuestionLevel = 1,
                        QuestionOrder = 1,
                        QuestionWeight = dto.MaxScore > 0 ? dto.MaxScore : 100
                    };
                    _context.Questions.Add(question);
                }
                else
                {
                    question.QuestionText = jsonData;
                    if (dto.MaxScore > 0) question.QuestionWeight = dto.MaxScore;
                }
                
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Matching data saved successfully");
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving matching data: {ex.Message}");
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
        [HttpDelete("matching/by-task/{taskId}")]
public async Task<IActionResult> DeleteMatchingData(int taskId)
{
    var question = await _context.Questions
        .FirstOrDefaultAsync(q => q.TaskID == taskId);
    
    if (question != null)
    {
        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
    }
    
    return Ok(new { success = true });
}
    }

    // DTO классы
    public class CreateModuleDto
    {
        public int ModuleNumber { get; set; }
        public string ModuleTitle { get; set; } = "";
        public string ModuleDescription { get; set; } = "";
        public int DifficultyLevel { get; set; }
        public bool IsPublished { get; set; }
    }

    public class CreateLessonDto
    {
        public int ModuleId { get; set; }
        public string LessonTitle { get; set; } = "";
        public string LessonDescription { get; set; } = "";
        public int LessonOrder { get; set; }
        public string TheoryContent { get; set; } = "";
    }

    public class CreateTaskDto
    {
        public int LessonId { get; set; }
        public string TypeName { get; set; } = "";
        public string TaskTitle { get; set; } = "";
        public string TaskDescription { get; set; } = "";
        public int DifficultyLevel { get; set; }
        public int MaxScore { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateQuestionDto
    {
        public int TaskId { get; set; }
        public string QuestionText { get; set; } = "";
        public int QuestionLevel { get; set; }
        public int QuestionOrder { get; set; }
        public int QuestionWeight { get; set; }
        public List<AnswerDto> Answers { get; set; } = new List<AnswerDto>();
    }

    public class AnswerDto
    {
        public string AnswerText { get; set; } = "";
        public bool IsCorrect { get; set; }
    }

        public class CreateQuestStageDto
    {
        public int TaskId { get; set; }
        public string StageTitle { get; set; } = "";
        public string StageDescription { get; set; } = "";
        public int StageOrder { get; set; }
        public string SuccessCondition { get; set; } = "";
    }
        public class QuestStageResponseDto
    {
        public int QuestStageId { get; set; }
        public int TaskId { get; set; }
        public string StageTitle { get; set; } = "";
        public string StageDescription { get; set; } = "";
        public int StageOrder { get; set; }
        public string SuccessCondition { get; set; } = "";
    }

       public class SaveMatchingDataDto
    {
        public List<string> LeftItems { get; set; } = new();
        public List<string> RightItems { get; set; } = new();
        public List<MatchPairDto> CorrectMatches { get; set; } = new();
        public int MaxScore { get; set; } = 100;
    }

    public class MatchPairDto
    {
        public int Left { get; set; }
        public int Right { get; set; }
    }
}