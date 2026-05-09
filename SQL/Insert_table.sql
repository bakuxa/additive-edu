INSERT INTO "Role" (role_name) VALUES 
    ('Студент'),
    ('Преподаватель');

INSERT INTO "Group" (group_name) VALUES 
    ('ЦИС-47'),
    ('АДС-15');

INSERT INTO "User" (
    email, password_hash, last_name, first_name, middle_name, 
    role_id, registration_date, blocked, phone, photo_url, group_id
) VALUES 
    ('bakanova@student.ystu.ru', 'hash_bakanova123', 'Баканова', 'Юлия', 'Николаевна',
     (SELECT RoleID FROM "Role" WHERE role_name = 'Студент'), 
     '2025-09-01 10:00:00', FALSE, '+7(910)123-45-67', '/avatars/bakanova.jpg',
     (SELECT GroupID FROM "Group" WHERE group_name = 'ЦИС-47')),
    ('petrov@student.ystu.ru', 'hash_petrov123', 'Петров', 'Иван', 'Алексеевич',
     (SELECT RoleID FROM "Role" WHERE role_name = 'Студент'), 
     '2025-09-01 10:00:00', FALSE, '+7(910)234-56-78', '/avatars/petrov.jpg',
     (SELECT GroupID FROM "Group" WHERE group_name = 'ЦИС-47')),
    ('sidorova@student.ystu.ru', 'hash_sidorova123', 'Сидорова', 'Мария', 'Сергеевна',
     (SELECT RoleID FROM "Role" WHERE role_name = 'Студент'), 
     '2025-09-01 10:00:00', FALSE, '+7(910)345-67-89', '/avatars/sidorova.jpg',
     (SELECT GroupID FROM "Group" WHERE group_name = 'АДС-15')),
    ('gulyaev@ystu.ru', 'hash_gulyaev123', 'Гуляев', 'Андрей', 'Сергеевич',
     (SELECT RoleID FROM "Role" WHERE role_name = 'Преподаватель'), 
     '2024-08-15 09:00:00', FALSE, '+7(4852)12-34-56', '/avatars/gulyaev.jpg', NULL),
    ('smirnova@ystu.ru', 'hash_smirnova123', 'Смирнова', 'Елена', 'Васильевна',
     (SELECT RoleID FROM "Role" WHERE role_name = 'Преподаватель'), 
     '2024-08-15 09:00:00', FALSE, '+7(4852)23-45-67', '/avatars/smirnova.jpg', NULL);

INSERT INTO "Type" (type_name) VALUES 
    ('Тест'),
    ('Квест'),
    ('Симулятор');

INSERT INTO "Module" (module_title, module_description, module_number, difficulty_level, is_published) VALUES 
    ('Введение в аддитивные технологии', 
     'Основные понятия, история развития, классификация технологий 3D-печати', 
     1, 1, TRUE),
    ('FDM-печать', 
     'Технология послойного наплавления: принцип работы, материалы, настройка параметров', 
     2, 2, TRUE),
    ('Фотополимерная печать (SLA/DLP)', 
     'Стереолитография: принцип работы, смолы, области применения', 
     3, 2, TRUE),
    ('Лазерное спекание (SLS)', 
     'Селективное лазерное спекание порошковых материалов', 
     4, 3, TRUE);

INSERT INTO "Lesson" (module_id, lesson_title, lesson_description, lesson_order, theory_content) VALUES 
    ((SELECT ModuleID FROM "Module" WHERE module_title = 'FDM-печать'),
     'Принцип работы FDM-принтера',
     'Как работает экструдер, каретка, нагревательный стол',
     1,
     '<h3>Принцип работы FDM-принтера</h3><p>FDM — технология послойного наплавления. Пластиковая нить подается в экструдер, нагревается и выдавливается через сопло.</p>'),
    ((SELECT ModuleID FROM "Module" WHERE module_title = 'FDM-печать'),
     'Материалы для FDM-печати',
     'Виды пластиков: PLA, ABS, PETG',
     2,
     '<h3>Материалы для FDM-печати</h3><p>PLA — биоразлагаемый пластик, простой в печати. ABS — прочный, требует подогрева стола.</p>'),
    ((SELECT ModuleID FROM "Module" WHERE module_title = 'FDM-печать'),
     'Настройка параметров печати',
     'Температура, скорость, высота слоя',
     3,
     '<h3>Настройка параметров печати</h3><p>Температура сопла: PLA — 190–220°C, ABS — 220–250°C.</p>');

INSERT INTO "Task" (lesson_id, type_id, task_title, task_description, difficulty_level, max_score, is_active) VALUES 
    ((SELECT LessonID FROM "Lesson" WHERE lesson_title = 'Принцип работы FDM-принтера'),
     (SELECT TypeID FROM "Type" WHERE type_name = 'Тест'),
     'Тест: Принцип работы FDM-принтера',
     'Проверка знаний по устройству FDM-принтера',
     2, 100, TRUE),
    ((SELECT LessonID FROM "Lesson" WHERE lesson_title = 'Материалы для FDM-печати'),
     (SELECT TypeID FROM "Type" WHERE type_name = 'Тест'),
     'Тест: Материалы для FDM-печати',
     'Проверка знаний о пластиках',
     2, 100, TRUE);

INSERT INTO "Question" (task_id, question_text, question_level, question_order, question_weight) VALUES 
    ((SELECT TaskID FROM "Task" WHERE task_title = 'Тест: Принцип работы FDM-принтера'),
     'Что означает аббревиатура FDM?', 1, 1, 25),
    ((SELECT TaskID FROM "Task" WHERE task_title = 'Тест: Принцип работы FDM-принтера'),
     'Какой элемент FDM-принтера отвечает за расплавление пластика?', 1, 2, 25),
    ((SELECT TaskID FROM "Task" WHERE task_title = 'Тест: Принцип работы FDM-принтера'),
     'Для чего нужен нагревательный стол в FDM-принтере?', 1, 3, 25),
    ((SELECT TaskID FROM "Task" WHERE task_title = 'Тест: Принцип работы FDM-принтера'),
     'Какое программное обеспечение используется для подготовки G-code?', 2, 4, 25);

INSERT INTO "Answer" (question_id, answer_text, is_correct) VALUES
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Что означает аббревиатура FDM?'), 'Fused Deposition Modeling', TRUE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Что означает аббревиатура FDM?'), 'Fast Digital Manufacturing', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Что означает аббревиатура FDM?'), 'Fiber Deposition Method', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Что означает аббревиатура FDM?'), 'Filament Direct Modeling', FALSE);

INSERT INTO "Answer" (question_id, answer_text, is_correct) VALUES
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какой элемент FDM-принтера отвечает за расплавление пластика?'), 'Экструдер (хотэнд)', TRUE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какой элемент FDM-принтера отвечает за расплавление пластика?'), 'Нагревательный стол', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какой элемент FDM-принтера отвечает за расплавление пластика?'), 'Радиатор охлаждения', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какой элемент FDM-принтера отвечает за расплавление пластика?'), 'Датчик filament', FALSE);

INSERT INTO "Answer" (question_id, answer_text, is_correct) VALUES
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Для чего нужен нагревательный стол в FDM-принтере?'), 'Для улучшения адгезии и предотвращения деформации', TRUE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Для чего нужен нагревательный стол в FDM-принтере?'), 'Для быстрого охлаждения детали', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Для чего нужен нагревательный стол в FDM-принтере?'), 'Для подачи пластика в экструдер', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Для чего нужен нагревательный стол в FDM-принтере?'), 'Для автоматической калибровки сопла', FALSE);

INSERT INTO "Answer" (question_id, answer_text, is_correct) VALUES
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какое программное обеспечение используется для подготовки G-code?'), 'Cura / PrusaSlicer', TRUE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какое программное обеспечение используется для подготовки G-code?'), 'AutoCAD', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какое программное обеспечение используется для подготовки G-code?'), 'Blender', FALSE),
    ((SELECT QuestionID FROM "Question" WHERE question_text = 'Какое программное обеспечение используется для подготовки G-code?'), 'Photoshop', FALSE);

INSERT INTO "Achievement" (achievement_title, achievement_description, points_reward, condition_description) VALUES 
    ('Первый слой', 'Завершить первый урок', 50, 'Завершить первый урок'),
    ('Знаток FDM', 'Сдать тест по FDM-печати на 100%', 100, 'Правильно ответить на все вопросы'),
    ('Мастер настройки', 'Достичь качества печати 90% в симуляторе', 150, 'Качество печати >= 90%');