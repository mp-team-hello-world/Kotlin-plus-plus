// ========================
// ПОЛУЧАЕМ ЭЛЕМЕНТЫ
// ========================
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const loadFileBtn = document.getElementById('loadFileBtn');
const loadArchiveBtn = document.getElementById('loadArchiveBtn');
const translateBtn = document.getElementById('translateBtn');
const downloadBtn = document.getElementById('downloadBtn');

// Элементы для уведомления (красный фон)
const progressToast = document.getElementById('progressToast');

// Элементы модального окна
const modal = document.getElementById('filenameModal');
const filenameInput = document.getElementById('filenameInput');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelModalBtn = document.getElementById('cancelModalBtn');
const submitFilenameBtn = document.getElementById('submitFilenameBtn');

// ========================
// ФУНКЦИЯ УВЕДОМЛЕНИЯ "В ПРОЦЕССЕ"
// ========================
function showInProgressMessage() {
    // Показываем красный тост
    progressToast.classList.add('show');
    // Скрываем через 2 секунды
    setTimeout(() => {
        progressToast.classList.remove('show');
    }, 2000);
}

// ========================
// УПРАВЛЕНИЕ СОСТОЯНИЕМ КНОПКИ TRANSLATE (серая, если левое поле пустое)
// ========================
function updateTranslateButtonState() {
    const leftText = leftTextarea.value.trim();
    if (leftText === '') {
        translateBtn.disabled = true;
        translateBtn.style.opacity = '0.6';
    } else {
        translateBtn.disabled = false;
        translateBtn.style.opacity = '1';
    }
}

// ========================
// ФУНКЦИЯ TRANSLATE: копирует левый текст в правое поле
// ========================
function translateText() {
    // Дополнительная проверка (на всякий случай, кнопка disabled не даст нажать)
    if (leftTextarea.value.trim() === '') {
        return;
    }
    // Копируем содержимое левого поля в правое
    rightTextarea.value = leftTextarea.value;
    // Автоматически расширяем правое поле
    autoResize(rightTextarea);
    // Даём визуальный фидбек (лёгкая анимация)
    translateBtn.style.transform = 'scale(0.98)';
    setTimeout(() => {
        translateBtn.style.transform = '';
    }, 120);
}

// ========================
// ФУНКЦИЯ ЗАГРУЗКИ ФАЙЛА .cpp (с вводом имени)
// ========================
function openFilenameModal() {
    // Очищаем поле ввода
    filenameInput.value = '';
    // Показываем модальное окно
    modal.classList.add('show');
    // Фокусируемся на поле ввода
    setTimeout(() => {
        filenameInput.focus();
    }, 100);
}

function closeModal() {
    modal.classList.remove('show');
}

function downloadAsCpp() {
    let fileName = filenameInput.value.trim();
    // Если имя не введено, используем значение по умолчанию "document"
    if (fileName === '') {
        fileName = 'document';
    }
    // Убираем потенциально опасные символы (оставляем буквы, цифры, дефис, нижнее подчеркивание)
    fileName = fileName.replace(/[^a-zA-Z0-9_\-]/g, '_');
    if (fileName.length === 0) fileName = 'downloaded_file';
    
    // Формируем расширение .cpp
    const fullFileName = `${fileName}.cpp`;
    
    // Берём содержимое из ПРАВОГО поля (по заданию — скачивается содержимое правой области?)
    // Уточнение: в классическом сценарии download выгружает текст, который отредактирован.
    // Ниже скачиваем содержимое правой области (можно поменять на левую, но логичнее результат translate)
    // Поскольку обычно "Download" сохраняет получившийся/текущий контент. Скачаем содержимое правой области.
    let contentToSave = rightTextarea.value;
    // Если содержимое пустое — добавим комментарий по умолчанию, чтобы файл не был полностью пустым.
    if (contentToSave.trim() === '') {
        contentToSave = '// Сгенерированный C++ файл\n// Добавьте код в правую область перед скачиванием.\n\n#include <iostream>\n\nint main() {\n    std::cout << "Hello, World!" << std::endl;\n    return 0;\n}';
    }
    
    // Создаём Blob и запускаем загрузку
    const blob = new Blob([contentToSave], { type: 'text/plain' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.download = fullFileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    
    // Показываем небольшое уведомление (зелёное, но можно стандартное, но по заданию необязательно)
    // Для удобства сделаем лёгкий фидбек без изменения условий
    const originalText = downloadBtn.innerText;
    downloadBtn.innerText = '✓ Скачано!';
    setTimeout(() => {
        downloadBtn.innerText = originalText;
    }, 1500);
    
    closeModal();
}

// ========================
// АВТОМАТИЧЕСКОЕ РАСШИРЕНИЕ TEXTAREA (ВЕРТИКАЛЬНО)
// ========================
function autoResize(textarea) {
    if (!textarea) return;
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
}

function attachAutoResize(textarea) {
    if (!textarea) return;
    autoResize(textarea);
    textarea.addEventListener('input', function() {
        autoResize(this);
        // При изменении левого поля обновляем состояние кнопки Translate
        if (textarea.id === 'leftText') {
            updateTranslateButtonState();
        }
    });
    window.addEventListener('resize', function() {
        autoResize(textarea);
    });
}

// ========================
// ОБРАБОТЧИКИ КНОПОК
// ========================
// Load File – красное уведомление "в процессе"
loadFileBtn.addEventListener('click', () => {
    showInProgressMessage();
});

// Load Archive – красное уведомление "в процессе"
loadArchiveBtn.addEventListener('click', () => {
    showInProgressMessage();
});

// Translate
translateBtn.addEventListener('click', () => {
    if (translateBtn.disabled) return;
    translateText();
});

// Download – открыть модальное окно для ввода имени файла
downloadBtn.addEventListener('click', () => {
    openFilenameModal();
});

// ========================
// МОДАЛЬНОЕ ОКНО: ОБРАБОТЧИКИ
// ========================
closeModalBtn.addEventListener('click', closeModal);
cancelModalBtn.addEventListener('click', closeModal);
submitFilenameBtn.addEventListener('click', downloadAsCpp);

// Закрытие по клику на фон (overlay)
modal.addEventListener('click', (e) => {
    if (e.target === modal) {
        closeModal();
    }
});

// Нажатие Enter в поле ввода имени файла
filenameInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        e.preventDefault();
        downloadAsCpp();
    }
});

// ========================
// ДОПОЛНИТЕЛЬНО: ПАСТА (ВСТАВКА) ДЛЯ КОРРЕКТНОГО РАСШИРЕНИЯ
// ========================
leftTextarea.addEventListener('paste', function() {
    setTimeout(() => {
        autoResize(leftTextarea);
        updateTranslateButtonState();
    }, 10);
});
rightTextarea.addEventListener('paste', function() {
    setTimeout(() => autoResize(rightTextarea), 10);
});

// Обработка ручного изменения содержимого левого поля (для обновления состояния Translate)
leftTextarea.addEventListener('input', function() {
    updateTranslateButtonState();
});

// ========================
// ИНИЦИАЛИЗАЦИЯ ПРИ ЗАГРУЗКЕ СТРАНИЦЫ
// ========================
function init() {
    // Устанавливаем начальное состояние кнопки Translate (серая, если левое поле пусто)
    updateTranslateButtonState();
    
    // Расширяем оба textarea под их содержимое (даже если есть placeholder)
    attachAutoResize(leftTextarea);
    attachAutoResize(rightTextarea);
    
    // Дополнительная стилистика для кнопки Translate: изначально серая если поле пусто
    if (leftTextarea.value.trim() === '') {
        translateBtn.disabled = true;
    } else {
        translateBtn.disabled = false;
    }
    
    // Проверяем, что уведомление скрыто
    progressToast.classList.remove('show');
    // модальное окно скрыто
    modal.classList.remove('show');
}

// Запуск после полной загрузки DOM
document.addEventListener('DOMContentLoaded', init);