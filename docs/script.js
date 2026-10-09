// ========================
// ЭЛЕМЕНТЫ DOM
// ========================
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const loadFileBtn = document.getElementById('loadFileBtn');
const loadArchiveBtn = document.getElementById('loadArchiveBtn');
const translateBtn = document.getElementById('translateBtn');
const viewTreeBtn = document.getElementById('viewTreeBtn');
const downloadBtn = document.getElementById('downloadBtn');

// Дерево разбора последнего успешного перевода (для кнопки View Tree)
let lastTreeData = null;

// Уведомления
const progressToast = document.getElementById('progressToast');
const successToast = document.getElementById('successToast');

// Модальное окно сохранения файла
const modal = document.getElementById('filenameModal');
const filenameInput = document.getElementById('filenameInput');
const closeModalBtn = document.getElementById('closeModalBtn');
const cancelModalBtn = document.getElementById('cancelModalBtn');
const submitFilenameBtn = document.getElementById('submitFilenameBtn');

// ========================
// ЯЗЫКИ ПРОГРАММИРОВАНИЯ
// ========================
// Полный "enum" языков. Ключ — короткий код (уйдёт в JSON на сервер),
// значение — отображаемое имя, иконка и расширение файла.
const LANGUAGES = {
    kt:  { name: 'Kotlin', flag: '🟣', ext: '.kt'  },
    cpp: { name: 'C++',    flag: '🔵', ext: '.cpp' },
    // java:   { name: 'Java',   flag: '☕', ext: '.java' },
    // python: { name: 'Python', flag: '🐍', ext: '.py'   },
};

// Какие языки доступны для каждой панели (source — левая, target — правая)
const AVAILABLE_LANGUAGES = {
    source: ['kt'],
    target: ['cpp'],
};

// Текущие выбранные языки. Именно эти значения уйдут в JSON на сервер.
let sourceLanguage = 'kt';
let targetLanguage = 'cpp';

// Какой язык сейчас выбираем в модалке: 'source' или 'target'
let languageModalTarget = null;

// DOM модального окна языков
const languageModal = document.getElementById('languageModal');
const languageModalTitle = document.getElementById('languageModalTitle');
const closeLanguageModalBtn = document.getElementById('closeLanguageModalBtn');
const cancelLanguageModalBtn = document.getElementById('cancelLanguageModalBtn');
const languageList = document.getElementById('languageList');

// Кнопки-заголовки панелей
const leftLangBtn = document.getElementById('leftLangBtn');
const rightLangBtn = document.getElementById('rightLangBtn');
const leftLangName = document.getElementById('leftLangName');
const rightLangName = document.getElementById('rightLangName');

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
// TRANSLATE — отправляет code + source + target на сервер
// ========================
async function translateText() {
    if (translateBtn.disabled) return;

    // Собираем JSON для сервера
    const payload = {
        source: sourceLanguage,   // например "kt"
        target: targetLanguage,   // например "cpp"
        code: leftTextarea.value
    };

    // Лёгкая анимация нажатия кнопки
    translateBtn.style.transform = 'scale(0.97)';
    setTimeout(() => { translateBtn.style.transform = ''; }, 120);

    try {
        // !!! СЮДА ВСТАВЛЯЙТЕ ССЫЛКУ, КОТОРУЮ ВАМ ДАЕТ LOCALTUNNEL !!!
        // Обязательно добавьте /translate в конец адреса
        const tunnelUrl = 'https://mkn-kotlin-compiler.loca.lt/translate';
        // const tunnelUrl = 'http://localhost:5000/translate'; // для локального тестирования

        const response = await fetch(tunnelUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                // Этот заголовок заставляет localtunnel пропускать встроенную заглушку
                'Bypass-Tunnel-Reminder': 'true'
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            // Если сервер вернул ошибку (например, BadRequest при ошибке синтаксиса)
            const errorData = await response.json();
            throw new Error(errorData.error || 'Ошибка компиляции');
        }

        const data = await response.json();

        // Записываем полученный код в правое поле
        rightTextarea.value = data.cppCode;

        // Сохраняем дерево разбора и включаем кнопку View Tree
        lastTreeData = data.tree || null;
        viewTreeBtn.disabled = !lastTreeData;

    } catch (error) {
        console.error("Ошибка трансляции:", error);

        // Выводим ошибку в правое поле, чтобы пользователь понял, что пошло не так
        rightTextarea.value = `/* \n[ОШИБКА ТРАНСЛЯЦИИ]\nНе удалось связаться с сервером или парсер ANTLR обнаружил ошибку:\n${error.message}\n*/`;

        lastTreeData = null;
        viewTreeBtn.disabled = true;
    } finally {
        // Если функция autoResize определена где-то ещё — вызовется, иначе пропустится
        if (typeof autoResize === 'function') autoResize(rightTextarea);
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

    // Расширение зависит от выбранного целевого языка
    const ext = LANGUAGES[targetLanguage]?.ext || '.txt';
    const fullFileName = `${fileName}${ext}`;

    let contentToSave = rightTextarea.value;
    if (contentToSave.trim() === '') {
        contentToSave = '// Generated file\n';
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
    const header = document.querySelector('.header-title');
    const panels = document.querySelector('.panels');

    if (!header || !panels) return 320;

    const headerTop = header.getBoundingClientRect().top;
    const windowHeight = window.innerHeight;
    const panelsTop = panels.getBoundingClientRect().top;
    const bottomOffset = headerTop + 30;

    let availableHeight = windowHeight - panelsTop - bottomOffset;

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

function updateHeightOnResize() {
    if (isManualResizing) return;
    const newHeight = calculateOptimalHeight();
    setBothTextareasHeight(newHeight);
}

// ========================
// СИНХРОННЫЙ РУЧНОЙ РЕСАЙЗ
// ========================
function setupManualResize(textarea, otherTextarea, sideName) {
    let wrapper = textarea.parentElement;
    if (!wrapper.classList.contains('textarea-wrapper')) {
        const newWrapper = document.createElement('div');
        newWrapper.className = 'textarea-wrapper';
        textarea.parentNode.insertBefore(newWrapper, textarea);
        newWrapper.appendChild(textarea);
        wrapper = newWrapper;
    }

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

            const minHeight = 150;
            const maxHeight = 800;
            newHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));

            console.log(sideName + ': newHeight = ' + newHeight);

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
// МОДАЛЬНОЕ ОКНО ВЫБОРА ЯЗЫКА
// ========================
function renderLanguageList() {
    languageList.innerHTML = '';

    const allowed = AVAILABLE_LANGUAGES[languageModalTarget] || [];
    const activeLang = languageModalTarget === 'source' ? sourceLanguage : targetLanguage;

    allowed.forEach(code => {
        const info = LANGUAGES[code];
        if (!info) return;

        const btn = document.createElement('button');
        btn.className = 'language-option';
        btn.dataset.lang = code;
        if (code === activeLang) btn.classList.add('active');

        btn.innerHTML = `
            <span class="lang-flag">${info.flag}</span>
            <span class="lang-name">${info.name}</span>
            <span class="lang-check">✓</span>
        `;
        languageList.appendChild(btn);
    });
}

function openLanguageModal(side) {
    languageModalTarget = side;
    languageModalTitle.textContent = side === 'source'
        ? '🌐 Source language'
        : '🌐 Target language';
    renderLanguageList();
    languageModal.classList.add('show');
}

function closeLanguageModal() {
    languageModal.classList.remove('show');
    languageModalTarget = null;
}

function applyLanguage(side, langCode) {
    if (!AVAILABLE_LANGUAGES[side]?.includes(langCode)) return;
    if (!LANGUAGES[langCode]) return;

    if (side === 'source') {
        sourceLanguage = langCode;
        leftLangName.textContent = LANGUAGES[langCode].name;
    } else if (side === 'target') {
        targetLanguage = langCode;
        rightLangName.textContent = LANGUAGES[langCode].name;

        // Обновляем плейсхолдер правого поля — расширение могло измениться
        const ext = LANGUAGES[langCode].ext;
        rightTextarea.placeholder =
            `Translated code appears here...\nYou can also edit this field manually.\nClick Download to save this as ${ext} file.`;
    }

    console.log('Current languages (ready for JSON):', {
        source: sourceLanguage,
        target: targetLanguage
    });
}

// Обработчики модального окна
closeLanguageModalBtn.addEventListener('click', closeLanguageModal);
cancelLanguageModalBtn.addEventListener('click', closeLanguageModal);
languageModal.addEventListener('click', (e) => {
    if (e.target === languageModal) closeLanguageModal();
});
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape' && languageModal.classList.contains('show')) {
        closeLanguageModal();
    }
});

// Клик по языку в списке
languageList.addEventListener('click', (e) => {
    const option = e.target.closest('.language-option');
    if (!option || !languageModalTarget) return;
    const lang = option.dataset.lang;
    const side = languageModalTarget;
    applyLanguage(side, lang);
    closeLanguageModal();
});

// Открытие модалки по клику на заголовки панелей
leftLangBtn.addEventListener('click', () => openLanguageModal('source'));
rightLangBtn.addEventListener('click', () => openLanguageModal('target'));

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

viewTreeBtn.addEventListener('click', () => {
    if (lastTreeData) TreeView.open(lastTreeData);
});

downloadBtn.addEventListener('click', openFilenameModal);

// Модальное окно сохранения
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
    console.log('Инициализация...');

    const optimalHeight = calculateOptimalHeight();
    setBothTextareasHeight(optimalHeight);

    setupManualResize(leftTextarea, rightTextarea, 'LEFT');
    setupManualResize(rightTextarea, leftTextarea, 'RIGHT');

    updateTranslateButtonState();

    // Стартовые названия языков в заголовках панелей
    leftLangName.textContent = LANGUAGES[sourceLanguage].name;
    rightLangName.textContent = LANGUAGES[targetLanguage].name;

    // Скрываем оверлеи
    progressToast.classList.remove('show');
    successToast.classList.remove('show');
    modal.classList.remove('show');
    languageModal.classList.remove('show');

    console.log('Готово. Языки:', { source: sourceLanguage, target: targetLanguage });
}

document.addEventListener('DOMContentLoaded', init);