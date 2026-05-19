// ========================
// ЭЛЕМЕНТЫ DOM
// ========================
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const loadFileBtn = document.getElementById('loadFileBtn');
const loadArchiveBtn = document.getElementById('loadArchiveBtn');
const translateBtn = document.getElementById('translateBtn');
const downloadBtn = document.getElementById('downloadBtn');

// Уведомления
const progressToast = document.getElementById('progressToast');
const successToast = document.getElementById('successToast');

// Модальное окно
const modal = document.getElementById('filenameModal');
const filenameInput = document.getElementById('filenameInput');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelModalBtn = document.getElementById('cancelModalBtn');
const submitFilenameBtn = document.getElementById('submitFilenameBtn');

// ========================
// УВЕДОМЛЕНИЯ
// ========================
function showInProgressMessage() {
    progressToast.classList.remove('show');
    void progressToast.offsetWidth;
    progressToast.classList.add('show');
    setTimeout(() => {
        progressToast.classList.remove('show');
    }, 2000);
}

function showSuccessSavedMessage() {
    successToast.classList.remove('show');
    void successToast.offsetWidth;
    successToast.classList.add('show');
    setTimeout(() => {
        successToast.classList.remove('show');
    }, 2000);
}

// ========================
// УПРАВЛЕНИЕ КНОПКОЙ TRANSLATE
// ========================
function updateTranslateButtonState() {
    translateBtn.disabled = leftTextarea.value.trim() === '';
}

// ========================
// TRANSLATE — отправляет Kotlin-код на сервер и выводит сгенерированный C++
// ========================
async function translateText() {
    if (translateBtn.disabled) return;
    
    const kotlinCode = leftTextarea.value;

    // Лёгкая анимация нажатия кнопки
    translateBtn.style.transform = 'scale(0.97)';
    setTimeout(() => { translateBtn.style.transform = ''; }, 120);
    
    try {
        // !!! СЮДА ВСТАВЛЯЙТЕ ССЫЛКУ, КОТОРУЮ ВАМ ДАЕТ LOCALTUNNEL !!!
        // Обязательно добавьте /translate в конец адреса
        const tunnelUrl = 'https://mkn-kotlin-compiler.loca.lt/translate';

        const response = await fetch(tunnelUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                // Этот заголовок заставляет localtunnel пропускать встроенную заглушку сомнительного сайта
                'Bypass-Tunnel-Reminder': 'true'
            },
            body: JSON.stringify({ code: kotlinCode })
        });

        if (!response.ok) {
            // Если .NET сервер вернул ошибку (например, BadRequest при ошибке синтаксиса)
            const errorData = await response.json();
            throw new Error(errorData.error || 'Ошибка компиляции');
        }

        const data = await response.json();
        
        // Записываем полученный C++ код в правое поле
        rightTextarea.value = data.cppCode;
        
    } catch (error) {
        console.error("Ошибка трансляции:", error);
        
        // Выводим ошибку в правое поле красивым текстом, чтобы пользователь понял, что пошло не так
        rightTextarea.value = `/* \n[ОШИБКА ТРАНСЛЯЦИИ]\nНе удалось связаться с сервером или парсер ANTLR обнаружил ошибку:\n${error.message}\n*/`;
    } finally {
        // В любом случае обновляем высоту правого текстового поля
        autoResize(rightTextarea);
    }
}

// ========================
// DOWNLOAD
// ========================
function openFilenameModal() {
    filenameInput.value = '';
    modal.classList.add('show');
    setTimeout(() => filenameInput.focus(), 100);
}

function closeModal() {
    modal.classList.remove('show');
}

function triggerDownload() {
    let fileName = filenameInput.value.trim();
    if (fileName === '') fileName = 'document';
    fileName = fileName.replace(/[^a-zA-Z0-9_\-]/g, '_');
    if (fileName.length === 0) fileName = 'downloaded_file';
    
    const fullFileName = `${fileName}.cpp`;
    let contentToSave = rightTextarea.value;
    if (contentToSave.trim() === '') {
        contentToSave = '// Generated C++ file\n\n#include <iostream>\n\nint main() {\n    std::cout << "Hello, World!" << std::endl;\n    return 0;\n}';
    }
    
    const blob = new Blob([contentToSave], { type: 'text/plain' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.download = fullFileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
    
    showSuccessSavedMessage();
    closeModal();
}

// ========================
// РАСЧЕТ ОПТИМАЛЬНОЙ ВЫСОТЫ TEXTAREA
// ========================
function calculateOptimalHeight() {
    // Получаем элементы
    const header = document.querySelector('.header-title');
    const panels = document.querySelector('.panels');
    
    if (!header || !panels) return 320; // fallback высота
    
    // Расстояние от верха заголовка до верха экрана
    const headerTop = header.getBoundingClientRect().top;
    
    // Высота окна
    const windowHeight = window.innerHeight;
    
    // Расстояние от верха panels до верха экрана
    const panelsTop = panels.getBoundingClientRect().top;
    
    // Отступ снизу: такой же как сверху, плюс небольшой дополнительный отступ (30px)
    const bottomOffset = headerTop + 30;
    
    // Высота для textarea = высота окна - отступ сверху до panels - отступ снизу
    let availableHeight = windowHeight - panelsTop - bottomOffset;
    
    // Ограничения
    const minHeight = 200;
    const maxHeight = 800;
    availableHeight = Math.max(minHeight, Math.min(maxHeight, availableHeight));
    
    return availableHeight;
}

// ========================
// УСТАНОВКА ВЫСОТЫ ДЛЯ ОБОИХ TEXTAREA
// ========================
let isManualResizing = false;

function setBothTextareasHeight(height) {
    leftTextarea.style.height = height + 'px';
    leftTextarea.style.overflowY = 'auto';
    rightTextarea.style.height = height + 'px';
    rightTextarea.style.overflowY = 'auto';
}

// Обновление высоты при ресайзе окна
function updateHeightOnResize() {
    if (isManualResizing) return; // Не мешаем ручному ресайзу
    
    const newHeight = calculateOptimalHeight();
    setBothTextareasHeight(newHeight);
}

// ========================
// СИНХРОННЫЙ РУЧНОЙ РЕСАЙЗ
// ========================
function setupManualResize(textarea, otherTextarea, sideName) {
    // Создаём wrapper
    let wrapper = textarea.parentElement;
    if (!wrapper.classList.contains('textarea-wrapper')) {
        const newWrapper = document.createElement('div');
        newWrapper.className = 'textarea-wrapper';
        textarea.parentNode.insertBefore(newWrapper, textarea);
        newWrapper.appendChild(textarea);
        wrapper = newWrapper;
    }
    
    // Создаём полоску
    let handle = wrapper.querySelector('.resize-handle');
    if (!handle) {
        handle = document.createElement('div');
        handle.className = 'resize-handle';
        wrapper.appendChild(handle);
    }
    
    handle.style.cursor = 'ns-resize';
    
    handle.addEventListener('mousedown', function(e) {
        e.preventDefault();
        e.stopPropagation();
        
        console.log(sideName + ': Mouse DOWN');
        isManualResizing = true;
        
        const startY = e.clientY;
        const startHeight = textarea.offsetHeight;
        
        function onMouseMove(moveEvent) {
            const delta = moveEvent.clientY - startY;
            let newHeight = startHeight + delta;
            
            // Ограничения
            const minHeight = 150;
            const maxHeight = 800;
            newHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));
            
            console.log(sideName + ': newHeight = ' + newHeight);
            
            // Меняем оба textarea
            textarea.style.height = newHeight + 'px';
            textarea.style.overflowY = 'auto';
            
            otherTextarea.style.height = newHeight + 'px';
            otherTextarea.style.overflowY = 'auto';
        }
        
        function onMouseUp() {
            console.log(sideName + ': Mouse UP');
            isManualResizing = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        }
        
        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
    
    console.log(sideName + ': Ресайз настроен (синхронный)');
}

// ========================
// ОБРАБОТЧИКИ КНОПОК
// ========================
loadFileBtn.addEventListener('click', () => {
    const fileInput = document.createElement('input');
    fileInput.type = 'file';
    
    fileInput.onchange = (event) => {
        const file = event.target.files[0];
        if (!file) return;
        
        const reader = new FileReader();
        reader.onload = (e) => {
            leftTextarea.value = e.target.result;
            updateTranslateButtonState();
        };
        reader.readAsText(file, 'UTF-8');
    };
    
    fileInput.click();
});

loadArchiveBtn.addEventListener('click', showInProgressMessage);

translateBtn.addEventListener('click', () => {
    if (!translateBtn.disabled) translateText();
});

downloadBtn.addEventListener('click', openFilenameModal);

// Модальное окно
closeModalBtn.addEventListener('click', closeModal);
cancelModalBtn.addEventListener('click', closeModal);
submitFilenameBtn.addEventListener('click', triggerDownload);
modal.addEventListener('click', (e) => {
    if (e.target === modal) closeModal();
});
filenameInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        e.preventDefault();
        triggerDownload();
    }
});

// ========================
// СОБЫТИЯ ДЛЯ TEXTAREA
// ========================
leftTextarea.addEventListener('input', () => {
    updateTranslateButtonState();
});

leftTextarea.addEventListener('paste', () => {
    setTimeout(() => {
        updateTranslateButtonState();
    }, 10);
});

// ========================
// ОБРАБОТЧИК РЕСАЙЗА ОКНА
// ========================
window.addEventListener('resize', () => {
    setTimeout(updateHeightOnResize, 100);
});

// ========================
// ИНИЦИАЛИЗАЦИЯ
// ========================
function init() {
    console.log('Инициализация с автоматической подстройкой высоты...');
    
    // Рассчитываем и устанавливаем оптимальную высоту
    const optimalHeight = calculateOptimalHeight();
    setBothTextareasHeight(optimalHeight);
    
    // Настраиваем синхронный ручной ресайз
    setupManualResize(leftTextarea, rightTextarea, 'LEFT');
    setupManualResize(rightTextarea, leftTextarea, 'RIGHT');
    
    // Обновляем состояние кнопки
    updateTranslateButtonState();
    
    // Скрываем уведомления
    progressToast.classList.remove('show');
    successToast.classList.remove('show');
    modal.classList.remove('show');
    
    console.log('Инициализация завершена, высота:', optimalHeight);
}

// Запускаем инициализацию после полной загрузки страницы
document.addEventListener('DOMContentLoaded', init);
