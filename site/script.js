// Получаем элементы
const leftTextarea = document.getElementById('leftText');
const rightTextarea = document.getElementById('rightText');

const clearLeftBtn = document.getElementById('clearLeftBtn');
const clearRightBtn = document.getElementById('clearRightBtn');
const copyLeftToRightBtn = document.getElementById('copyLeftToRightBtn');
const swapBtn = document.getElementById('swapBtn');

// Функция для автоматического изменения высоты textarea
function autoResize(textarea) {
    if (!textarea) return;
    // Сбрасываем высоту
    textarea.style.height = 'auto';
    // Устанавливаем новую высоту на основе содержимого
    textarea.style.height = textarea.scrollHeight + 'px';
}

// Применяем автовысоту к обоим полям
function attachAutoResize(textarea) {
    if (!textarea) return;
    // Первоначальная настройка
    autoResize(textarea);
    // При вводе текста
    textarea.addEventListener('input', function() {
        autoResize(this);
    });
    // При изменении размера окна
    window.addEventListener('resize', function() {
        autoResize(textarea);
    });
}

// Инициализация авто-расширения для обоих полей
attachAutoResize(leftTextarea);
attachAutoResize(rightTextarea);

// Функции для кнопок
function clearLeft() {
    leftTextarea.value = '';
    autoResize(leftTextarea);
}

function clearRight() {
    rightTextarea.value = '';
    autoResize(rightTextarea);
}

function copyLeftToRight() {
    rightTextarea.value = leftTextarea.value;
    autoResize(rightTextarea);
}

function swapContents() {
    const leftValue = leftTextarea.value;
    const rightValue = rightTextarea.value;
    leftTextarea.value = rightValue;
    rightTextarea.value = leftValue;
    autoResize(leftTextarea);
    autoResize(rightTextarea);
}

// Назначаем обработчики кнопкам
clearLeftBtn.addEventListener('click', clearLeft);
clearRightBtn.addEventListener('click', clearRight);
copyLeftToRightBtn.addEventListener('click', copyLeftToRight);
swapBtn.addEventListener('click', swapContents);

// Дополнительно: при вставке текста через Ctrl+V обновляем высоту
leftTextarea.addEventListener('paste', function() {
    setTimeout(() => autoResize(leftTextarea), 10);
});
rightTextarea.addEventListener('paste', function() {
    setTimeout(() => autoResize(rightTextarea), 10);
});

// Вызываем авто-размер при загрузке страницы
window.addEventListener('load', function() {
    autoResize(leftTextarea);
    autoResize(rightTextarea);
});