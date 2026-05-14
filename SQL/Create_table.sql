-- 1. Таблица «Роль» (Role)
CREATE TABLE "Role" (
    RoleID SERIAL PRIMARY KEY,
    role_name VARCHAR(20) NOT NULL UNIQUE
);

-- 2. Таблица «Группа» (Group)
CREATE TABLE "Group" (
    GroupID SERIAL PRIMARY KEY,
    group_name VARCHAR(20) NOT NULL UNIQUE
);

-- 3. Таблица «Пользователь» (User)
CREATE TABLE "User" (
    UserID SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    first_name VARCHAR(50) NOT NULL,
    middle_name VARCHAR(50),
    role_id INTEGER NOT NULL REFERENCES "Role"(RoleID),
    registration_date TIMESTAMP NOT NULL,
    blocked BOOLEAN NOT NULL DEFAULT FALSE,
    phone VARCHAR(20),
    photo_url VARCHAR(400),
    group_id INTEGER REFERENCES "Group"(GroupID)
);

-- 4. Таблица «Учебный модуль» (Module)
CREATE TABLE "Module" (
    ModuleID SERIAL PRIMARY KEY,
    module_title VARCHAR(100) NOT NULL,
    module_description TEXT,
    module_number INTEGER NOT NULL,
    difficulty_level INTEGER NOT NULL,
    is_published BOOLEAN NOT NULL DEFAULT FALSE
);

-- 5. Таблица «Урок» (Lesson)
CREATE TABLE "Lesson" (
    LessonID SERIAL PRIMARY KEY,
    module_id INTEGER NOT NULL REFERENCES "Module"(ModuleID),
    lesson_title VARCHAR(100) NOT NULL,
    lesson_description VARCHAR(200),
    lesson_order INTEGER NOT NULL,
    theory_content TEXT
);

-- 6. Таблица «Тип задания» (Type)
CREATE TABLE "Type" (
    TypeID SERIAL PRIMARY KEY,
    type_name VARCHAR(20) NOT NULL UNIQUE
);

-- 7. Таблица «Задание» (Task)
CREATE TABLE "Task" (
    TaskID SERIAL PRIMARY KEY,
    lesson_id INTEGER NOT NULL REFERENCES "Lesson"(LessonID),
    type_id INTEGER NOT NULL REFERENCES "Type"(TypeID),
    task_title VARCHAR(100) NOT NULL,
    task_description TEXT,
    difficulty_level INTEGER NOT NULL,
    max_score INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL
);

-- 8. Таблица «Тестовый вопрос» (Question)
CREATE TABLE "Question" (
    QuestionID SERIAL PRIMARY KEY,
    task_id INTEGER NOT NULL REFERENCES "Task"(TaskID),
    question_text TEXT NOT NULL,
    question_level INTEGER NOT NULL,
    question_order INTEGER NOT NULL,
    question_weight INTEGER NOT NULL
);

-- 9. Таблица «Вариант ответа» (Answer)
CREATE TABLE "Answer" (
    AnswerID SERIAL PRIMARY KEY,
    question_id INTEGER NOT NULL REFERENCES "Question"(QuestionID),
    answer_text TEXT NOT NULL,
    is_correct BOOLEAN NOT NULL DEFAULT FALSE
);

-- 10. Таблица «Этап квеста» (QuestStage)
CREATE TABLE "QuestStage" (
    QuestStageID SERIAL PRIMARY KEY,
    task_id INTEGER NOT NULL REFERENCES "Task"(TaskID),
    stage_title VARCHAR(100) NOT NULL,
    stage_description TEXT,
    stage_order INTEGER NOT NULL,
    success_condition TEXT
);

-- 11. Таблица «Сценарий симуляции» (Simulation)
CREATE TABLE "Simulation" (
    SimulationID SERIAL PRIMARY KEY,
    task_id INTEGER NOT NULL UNIQUE REFERENCES "Task"(TaskID),
    simulation_title VARCHAR(100) NOT NULL,
    input_parameters TEXT,
    expected_result TEXT,
    evaluation_criteria TEXT
);

-- 12. Таблица «Результат задания» (TaskResult)
CREATE TABLE "TaskResult" (
    ResultID SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES "User"(UserID),
    task_id INTEGER NOT NULL REFERENCES "Task"(TaskID),
    score INTEGER NOT NULL,
    attempt_number INTEGER NOT NULL,
    completion_status VARCHAR(20) NOT NULL,
    completed_at TIMESTAMP
);

-- 13. Таблица «Прогресс по уроку» (LessonProgress)
CREATE TABLE "LessonProgress" (
    ProgressID SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES "User"(UserID),
    lesson_id INTEGER NOT NULL REFERENCES "Lesson"(LessonID),
    progress_percent INTEGER NOT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    completion_status VARCHAR(20) NOT NULL
);

-- 14. Таблица «Достижение» (Achievement)
CREATE TABLE "Achievement" (
    AchievementID SERIAL PRIMARY KEY,
    achievement_title VARCHAR(100) NOT NULL,
    achievement_description TEXT,
    points_reward INTEGER NOT NULL,
    condition_description TEXT
);

-- 15. Таблица «Пользовательское достижение» (UserAchievement)
CREATE TABLE "UserAchievement" (
    UserAchievementID SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES "User"(UserID),
    achievement_id INTEGER NOT NULL REFERENCES "Achievement"(AchievementID),
    received_at TIMESTAMP NOT NULL
);

-- 16. Таблица «Рейтинг пользователя» (Rating)
CREATE TABLE "Rating" (
    RatingID SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL UNIQUE REFERENCES "User"(UserID),
    total_score INTEGER NOT NULL DEFAULT 0,
    current_level INTEGER NOT NULL DEFAULT 1,
    position_in_rating INTEGER,
    experience INTEGER NOT NULL DEFAULT 0
);