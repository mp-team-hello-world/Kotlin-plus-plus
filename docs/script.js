// ========================
// ЭЛЕМЕНТЫ DOM
// ========================
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const loadFileBtn = document.getElementById('loadFileBtn');
const loadArchiveBtn = document.getElementById('loadArchiveBtn');
const translateBtn = document.getElementById('translateBtn');
const downloadBtn = document.getElementById('downloadBtn');

// Уведомления (центрированные)
const progressToast = document.getElementById('progressToast');   // красное
const successToast = document.getElementById('successToast');     // зеленое

// Модальное окно (только для ввода имени, без лишних надписей)
const modal = document.getElementById('filenameModal');
const filenameInput = document.getElementById('filenameInput');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelModalBtn = document.getElementById('cancelModalBtn');
const submitFilenameBtn = document.getElementById('submitFilenameBtn');

// ========================
// ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ ДЛЯ УВЕДОМЛЕНИЙ
// ========================

// Показать красное уведомление "в процессе"
function showInProgressMessage() {
    progressToast.classList.remove('show');
    // Форсируем reflow
    void progressToast.offsetWidth;
    progressToast.classList.add('show');
    setTimeout(() => {
        progressToast.classList.remove('show');
    }, 2000);
}

// Показать зеленое уведомление "файл успешно сохранен"
function showSuccessSavedMessage() {
    successToast.classList.remove('show');
    void successToast.offsetWidth;
    successToast.classList.add('show');
    setTimeout(() => {
        successToast.classList.remove('show');
    }, 2000);
}

// ========================
// УПРАВЛЕНИЕ КНОПКОЙ TRANSLATE (серая, пока левое поле пустое)
// ========================
function updateTranslateButtonState() {
    const leftText = leftTextarea.value.trim();
    if (leftText === '') {
        translateBtn.disabled = true;
    } else {
        translateBtn.disabled = false;
    }
}

// ========================
// TRANSLATE — копирует левый текст в правое поле
// ========================
function translateText() {
    if (translateBtn.disabled) return;
    rightTextarea.value = leftTextarea.value;
    autoResize(rightTextarea);
    // Лёгкая анимация нажатия
    translateBtn.style.transform = 'scale(0.97)';
    setTimeout(() => {
        translateBtn.style.transform = '';
    }, 120);
}

// ========================
// DOWNLOAD: модальное окно → ввод имени → загрузка .cpp
// ========================
function openFilenameModal() {
    filenameInput.value = '';
    modal.classList.add('show');
    setTimeout(() => {
        filenameInput.focus();
    }, 100);
}

function closeModal() {
    modal.classList.remove('show');
}

function triggerDownload() {
    let fileName = filenameInput.value.trim();
    if (fileName === '') {
        fileName = 'document';
    }
    // Защита от недопустимых символов в имени файла
    fileName = fileName.replace(/[^a-zA-Z0-9_\-]/g, '_');
    if (fileName.length === 0) fileName = 'downloaded_file';
    
    const fullFileName = `${fileName}.cpp`;
    // Содержимое для сохранения — берём из правой области (результат работы)
    let contentToSave = rightTextarea.value;
    if (contentToSave.trim() === '') {
        // Шаблон по умолчанию, если в правой области пусто
        contentToSave = '// Generated C++ file\n// Add your code to the right area before downloading.\n\n#include <iostream>\n\nint main() {\n    std::cout << "Hello, World!" << std::endl;\n    return 0;\n}';
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
    
    // Зелёное уведомление об успешном сохранении
    showSuccessSavedMessage();
    
    // Закрываем модальное окно
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
        if (textarea.id === 'leftText') {
            updateTranslateButtonState();
        }
    });
    window.addEventListener('resize', () => autoResize(textarea));
}

// ========================
// НАЗНАЧЕНИЕ ОБРАБОТЧИКОВ КНОПОК
// ========================
loadFileBtn.addEventListener('click', () => {
    // Создаём скрытый input для выбора файла
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    
    // Сработает, когда пользователь выберет файл
    fileInput.onchange = (event) => {
        const file = event.target.files[0];
        if (!file) return;
        
        const reader = new FileReader();
        
        reader.onload = (e) => {
            leftTextarea.value = e.target.result;
        };
        
        reader.readAsText(file, 'UTF-8');
    };
    
    // Открываем системное окно
    fileInput.click();
});

loadArchiveBtn.addEventListener('click', showInProgressMessage);

translateBtn.addEventListener('click', () => {
    if (!translateBtn.disabled) {
        translateText();
    }
});

downloadBtn.addEventListener('click', openFilenameModal);

// ========================
// МОДАЛЬНОЕ ОКНО: закрытие и сохранение
// ========================
closeModalBtn.addEventListener('click', closeModal);
cancelModalBtn.addEventListener('click', closeModal);
submitFilenameBtn.addEventListener('click', triggerDownload);

// Закрытие по клику на фон
modal.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
});

// Enter в поле ввода имени файла
filenameInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        e.preventDefault();
        triggerDownload();
    }
});

// ========================
// ДОПОЛНИТЕЛЬНЫЕ СОБЫТИЯ ДЛЯ АВТОРАЗМЕРА И ПАСТЫ
// ========================
leftTextarea.addEventListener('paste', () => {
    setTimeout(() => {
        autoResize(leftTextarea);
        updateTranslateButtonState();
    }, 10);
});

rightTextarea.addEventListener('paste', () => {
    setTimeout(() => autoResize(rightTextarea), 10);
});

leftTextarea.addEventListener('input', updateTranslateButtonState);

// ========================
// ИНИЦИАЛИЗАЦИЯ
// ========================
function init() {
    updateTranslateButtonState();
    attachAutoResize(leftTextarea);
    attachAutoResize(rightTextarea);
    
    // Гарантируем, что уведомления скрыты при старте
    progressToast.classList.remove('show');
    successToast.classList.remove('show');
    modal.classList.remove('show');
}

document.addEventListener('DOMContentLoaded', init);
