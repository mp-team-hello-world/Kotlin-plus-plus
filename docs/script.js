// ========================
// ЭЛЕМЕНТЫ DOM
// ========================
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const loadFileBtn = document.getElementById('loadFileBtn');
const translateBtn = document.getElementById('translateBtn');
const viewTreeBtn = document.getElementById('viewTreeBtn');
const downloadBtn = document.getElementById('downloadBtn');

// Дерево разбора последнего успешного перевода
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
// ЯЗЫКИ (source — выбирается; target — всегда C++)
// ========================
const LANGUAGES = {
    kt:     { name: 'Kotlin', flag: '💎', ext: '.kt',    theme: 'kotlin' },
    sharp:  { name: 'Sharp',  flag: '☕', ext: '.sharp', theme: 'sharp'  },
    python: { name: 'Python', flag: '🐍', ext: '.py',    theme: 'python' },
};

const AVAILABLE_SOURCE_LANGS = ['kt', 'sharp', 'python'];

// Язык ещё не выбран — стартовая нейтральная тема
let sourceLanguage = null;
const targetLanguage = 'cpp';
const TARGET_EXT = '.cpp';

// DOM модального окна языков
const languageModal = document.getElementById('languageModal');
const languageModalTitle = document.getElementById('languageModalTitle');
const closeLanguageModalBtn = document.getElementById('closeLanguageModalBtn');
const cancelLanguageModalBtn = document.getElementById('cancelLanguageModalBtn');
const languageList = document.getElementById('languageList');

const leftLangBtn = document.getElementById('leftLangBtn');
const leftLangName = document.getElementById('leftLangName');

// ========================
// ТЕМЫ ОФОРМЛЕНИЯ
// ========================
const THEMES = {
    none:   { title: 'Lang++'   },
    kotlin: { title: 'Kotlin++' },
    sharp:  { title: 'Sharp++'  },
    python: { title: 'Python++' },
};

const appTitle = document.getElementById('appTitle');

function applyTheme(themeKey) {
    if (!themeKey || !THEMES[themeKey]) themeKey = 'none';
    document.documentElement.setAttribute('data-theme', themeKey);
    if (appTitle) {
        appTitle.textContent = THEMES[themeKey].title;
        document.title = THEMES[themeKey].title + ' | Internet code transpiler';
    }
}

// ========================
// УВЕДОМЛЕНИЯ
// ========================
function showInProgressMessage() {
    progressToast.classList.remove('show');
    void progressToast.offsetWidth;
    progressToast.classList.add('show');
    setTimeout(() => progressToast.classList.remove('show'), 2000);
}

function showSuccessSavedMessage() {
    successToast.classList.remove('show');
    void successToast.offsetWidth;
    successToast.classList.add('show');
    setTimeout(() => successToast.classList.remove('show'), 2000);
}

// ========================
// УПРАВЛЕНИЕ КНОПКОЙ TRANSLATE
// ========================
function updateTranslateButtonState() {
    translateBtn.disabled =
        leftTextarea.value.trim() === '' || sourceLanguage === null;
}

// ========================
// TRANSLATE
// ========================
async function translateText() {
    if (translateBtn.disabled) return;

    const payload = {
        source: sourceLanguage,
        target: targetLanguage,
        code: leftTextarea.value
    };

    translateBtn.style.transform = 'scale(0.97)';
    setTimeout(() => { translateBtn.style.transform = ''; }, 120);

    try {
        const tunnelUrl = 'https://mkn-kotlin-compiler.loca.lt/translate';
        // const tunnelUrl = 'http://localhost:5000/translate';

        const response = await fetch(tunnelUrl, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Bypass-Tunnel-Reminder': 'true'
            },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            const errorData = await response.json();
            throw new Error(errorData.error || 'Ошибка компиляции');
        }

        const data = await response.json();

        rightTextarea.value = data.cppCode;

        lastTreeData = data.tree || null;
        viewTreeBtn.disabled = !lastTreeData;

    } catch (error) {
        console.error("Ошибка трансляции:", error);
        rightTextarea.value = `/* \n[ОШИБКА ТРАНСЛЯЦИИ]\nНе удалось связаться с сервером или парсер ANTLR обнаружил ошибку:\n${error.message}\n*/`;
        lastTreeData = null;
        viewTreeBtn.disabled = true;
    } finally {
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

    const fullFileName = `${fileName}${TARGET_EXT}`;

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

        isManualResizing = true;
        const startY = e.clientY;
        const startHeight = textarea.offsetHeight;

        function onMouseMove(moveEvent) {
            const delta = moveEvent.clientY - startY;
            let newHeight = startHeight + delta;

            const minHeight = 150;
            const maxHeight = 800;
            newHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));

            textarea.style.height = newHeight + 'px';
            textarea.style.overflowY = 'auto';
            otherTextarea.style.height = newHeight + 'px';
            otherTextarea.style.overflowY = 'auto';
        }

        function onMouseUp() {
            isManualResizing = false;
            document.removeEventListener('mousemove', onMouseMove);
            document.removeEventListener('mouseup', onMouseUp);
        }

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    });
}

// ========================
// МОДАЛЬНОЕ ОКНО ВЫБОРА ИСХОДНОГО ЯЗЫКА
// ========================
function renderLanguageList() {
    languageList.innerHTML = '';

    AVAILABLE_SOURCE_LANGS.forEach(code => {
        const info = LANGUAGES[code];
        if (!info) return;

        const btn = document.createElement('button');
        btn.className = 'language-option';
        btn.dataset.lang = code;
        if (code === sourceLanguage) btn.classList.add('active');

        btn.innerHTML = `
            <span class="lang-flag">${info.flag}</span>
            <span class="lang-name">${info.name}</span>
            <span class="lang-check">✓</span>
        `;
        languageList.appendChild(btn);
    });
}

function openLanguageModal() {
    languageModalTitle.textContent = '🌐 Source language';
    renderLanguageList();
    languageModal.classList.add('show');
}

function closeLanguageModal() {
    languageModal.classList.remove('show');
}

function applySourceLanguage(langCode) {
    if (!AVAILABLE_SOURCE_LANGS.includes(langCode)) return;
    if (!LANGUAGES[langCode]) return;

    sourceLanguage = langCode;
    leftLangName.textContent = LANGUAGES[langCode].name;

    // Смена исходного языка автоматически меняет тему
    const themeForLang = LANGUAGES[langCode].theme;
    if (themeForLang && THEMES[themeForLang]) applyTheme(themeForLang);

    // Обновляем доступность Translate
    updateTranslateButtonState();
}

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

languageList.addEventListener('click', (e) => {
    const option = e.target.closest('.language-option');
    if (!option) return;
    applySourceLanguage(option.dataset.lang);
    closeLanguageModal();
});

// Только ЛЕВАЯ панель открывает модалку
leftLangBtn.addEventListener('click', openLanguageModal);

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

translateBtn.addEventListener('click', () => {
    if (!translateBtn.disabled) translateText();
});

viewTreeBtn.addEventListener('click', () => {
    if (lastTreeData) TreeView.open(lastTreeData);
});

downloadBtn.addEventListener('click', openFilenameModal);

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
    setTimeout(updateTranslateButtonState, 10);
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

    // Стартовая нейтральная тема (до выбора языка)
    applyTheme('none');

    const optimalHeight = calculateOptimalHeight();
    setBothTextareasHeight(optimalHeight);

    setupManualResize(leftTextarea, rightTextarea, 'LEFT');
    setupManualResize(rightTextarea, leftTextarea, 'RIGHT');

    // Пока язык не выбран — «Choose language»
    leftLangName.textContent = sourceLanguage
        ? LANGUAGES[sourceLanguage].name
        : 'Choose language';

    updateTranslateButtonState();

    progressToast.classList.remove('show');
    successToast.classList.remove('show');
    modal.classList.remove('show');
    languageModal.classList.remove('show');

    console.log('Готово. Source:', sourceLanguage, '→ Target:', targetLanguage);
}

document.addEventListener('DOMContentLoaded', init);