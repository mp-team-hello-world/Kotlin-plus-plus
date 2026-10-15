# API Kotlin++: ручка `POST /translate`

Одна ручка делает всё: принимает **исходный код** и **выбранные пользователем языки**
(с какого переводим и на какой), возвращает **переведённый код** и **дерево разбора** для экрана View Tree.

```
Пользователь выбрал языки + ввёл код
        │   { code, source, target }
        ▼
   POST /translate  ──►  сервер: разбор исходного языка ─► общее представление (IR) ─► генерация целевого языка
        │   { targetCode, tree }
        ▼
 правая панель = targetCode,  View Tree = tree
```

## 1. Запрос

| | |
|---|---|
| Метод / путь | `POST /translate` |
| Заголовок | `Content-Type: application/json` |
| Тело | JSON, три поля (см. ниже) |

```json
{
  "code": "fun main() { val x = 1 }",
  "source": "kotlin",
  "target": "cpp"
}
```

| Поле | Тип | Обязательное | Что это |
|---|---|---|---|
| `code` | string | да | Исходный код из левой панели. Пустая строка / одни пробелы → ошибка 400. |
| `source` | string | нет (по умолчанию `kotlin`) | Язык, **с которого** переводим (левая панель). |
| `target` | string | нет (по умолчанию `cpp`) | Язык, **на который** переводим (правая панель). |

Правила:

- Регистр не важен (`KT` = `kt`), пробелы по краям обрезаются. Имена полей тоже без учёта регистра.
- Лишние поля сервер игнорирует.
- **Именно `source` и `target`.** Не `from`/`to` и не `lang`: другие имена сервер не увидит
  и молча подставит значения по умолчанию (Kotlin → C++).
- В `source`/`target` отправляется **код языка** (таблица ниже), а не отображаемое имя («Kotlin»).

### Коды языков

Сервер принимает любой из кодов в строке:

| Язык | Коды | Как источник (`source`) | Как цель (`target`) |
|---|---|---|---|
| Kotlin | `kotlin` | да | — |
| C++ | `cpp` | — | да |
| Python | `python` | да | — |
| C# | `csharp` | да | — |

Актуальный список всегда в `TranslationFactory` (словари `Frontends` — исходные, `Backends` — целевые).
Работает **любая пара «источник → цель»** из этих списков. Если пара не поддерживается — см. ошибки (раздел 3).

### Как это связано с интерфейсом

В `script.js` уже есть всё нужное — выбранные в модалке языки хранятся в
`sourceLanguage` и `targetLanguage`, и `translateText()` отправляет их как есть:

```js
const payload = {
    source: sourceLanguage,   // "kt"
    target: targetLanguage,   // "cpp"
    code: leftTextarea.value
};
```

Отдельного запроса «сохранить выбор языков» **нет**: языки едут в каждом запросе вместе с кодом.
Ключи в `LANGUAGES` во фронтенде должны быть кодами из таблицы выше.

## 2. Ответ при успехе (`200 OK`)

```json
{
  "targetCode": "using namespace std;\n\n\nint main()\n{\n    const auto x = 1;\n    return 0;\n}\n",
  "cppCode":    "using namespace std;\n\n\nint main()\n{\n    const auto x = 1;\n    return 0;\n}\n",
  "tree": {
    "name": "Root",
    "source": "fun main() { val x = 1 }",
    "children": [ /* вложенные узлы такого же вида, сокращено */ ]
  }
}
```

| Поле | Что это |
|---|---|
| `targetCode` | Переведённый код на языке `target` → кладём в правую панель. **Используйте это поле.** |
| `cppCode` | Устаревший дубль `targetCode` (название осталось с первых версий, когда цель была только C++). Сейчас `script.js` читает его; **перейдите на `targetCode`** — дубль потом уберём. |
| `tree` | Дерево разбора **исходного** кода для экрана View Tree. |

### Формат `tree`

Каждый узел одинаковый:

| Поле | Тип | Смысл |
|---|---|---|
| `name` | string | Правило грамматики (`Statement`, `FunctionDeclaration`…) или токен в одинарных кавычках (`'val'`, `'+'`). |
| `children` | array | Дочерние узлы такого же вида; у токенов — пустой массив. |
| `source` | string | Кусок **исходного** кода, который покрывает узел (показывается по ПКМ на узле). |

Важно: имена правил зависят от грамматики **исходного** языка (у Kotlin, Python и C# они разные),
поэтому фронтенд не должен завязываться на конкретные имена — только на структуру `name / children / source`.

## 3. Ошибки

Тело ошибки от нашего кода: `{ "error": "текст для пользователя" }`, статус `400`.

| Ситуация | Статус | `error` |
|---|---|---|
| `code` пустой / из пробелов / поля нет | 400 | `Входной код пуст` |
| Пара языков не поддерживается | 400 | `Перевод из <source> в <target> пока не поддерживается.` |
| Исключение внутри перевода | 400 | текст исключения |
| Невалидный JSON в теле, неверный `Content-Type` | 400 / 415 | **стандартный ответ ASP.NET — поля `error` может не быть** (и тело может быть не JSON) |
| Сервер недоступен / CORS | — | `fetch` бросает `TypeError: Failed to fetch`, ответа нет |

Что должен сделать фронтенд:

- Читать `error` через `errorData.error || 'Ошибка перевода'`, а `response.json()` оборачивать в `try/catch`
  (в `translateText()` уже сделана `catch`-ветка, достаточно учесть не-JSON тело).
- При любой ошибке сбрасывать дерево и блокировать View Tree (уже так).

Нюансы, из-за которых «успех» не всегда значит «идеальный перевод»:

- **Синтаксические ошибки в исходном коде не дают 400.** Парсер восстанавливается и идёт дальше
  (ошибки пишутся только в консоль сервера), поэтому ответ будет `200`, а в `targetCode` могут появиться
  комментарии вида `/* Unsupported statement construction: ... */`. Это штатное поведение, не сбой API.
- Конструкции, которые пока не умеем переводить, тоже выходят комментариями, а не ошибкой.

## 4. Примеры

### JavaScript (`fetch`)

```js
const response = await fetch(API_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code: leftTextarea.value, source: sourceLanguage, target: targetLanguage })
});

if (!response.ok) {
    let message = 'Ошибка перевода';
    try { message = (await response.json()).error || message; } catch { /* тело не JSON */ }
    throw new Error(message);
}

const data = await response.json();
rightTextarea.value = data.targetCode;      // а не data.cppCode
lastTreeData = data.tree || null;
viewTreeBtn.disabled = !lastTreeData;
```

### curl

```bash
curl -X POST http://localhost:5000/translate \
  -H "Content-Type: application/json" \
  -d '{"code":"fun main() { val x = 1 }","source":"kt","target":"cpp"}'
```

(В PowerShell проще положить JSON в файл `req.json` и вызвать `curl.exe -X POST ... -d @req.json`,
или воспользоваться Postman.)

## 5. Куда слать запрос (адрес API)

| Как открыт сайт | Адрес в `fetch` | CORS |
|---|---|---|
| `http://localhost:5000/` (сервер сам раздаёт `index.html` из `wwwroot`) | `/translate` или `http://localhost:5000/translate` | не нужен, тот же origin |
| Live Server (`http://127.0.0.1:5500`) | `http://localhost:5000/translate` | разрешён в `Program.cs` |
| GitHub Pages (`https://mp-team-hello-world.github.io`) | адрес localtunnel + `/translate`, заголовок `Bypass-Tunnel-Reminder: true` | разрешён в `Program.cs` |

Чтобы не править `script.js` руками при каждом переключении, адрес можно вынести в одну константу:

```js
const API_URL = ['localhost', '127.0.0.1'].includes(location.hostname)
    ? 'http://localhost:5000/translate'
    : 'https://mkn-kotlin-compiler.loca.lt/translate';
```

Если открываете сайт с нового адреса — его нужно добавить в `WithOrigins(...)` в `Program.cs`
и перезапустить сервер, иначе браузер заблокирует запрос.

## 6. Чек-лист: добавляем новый язык

**Бэкенд**
1. Исходный язык: грамматика (`.g4`) + `XAstBuilder` + `XFrontend`; целевой язык: `XBackend`.
2. Добавить строку в `Frontends` и/или `Backends` в `TranslationFactory` (коды из таблицы раздела 1, можно несколько алиасов).
3. Перезапустить сервер.

**Фронтенд**
1. Добавить язык в `LANGUAGES` (`name`, `flag`, `ext`), ключ — код языка для API.
2. Добавить ключ в `AVAILABLE_LANGUAGES.source` и/или `AVAILABLE_LANGUAGES.target`.
3. Больше ничего: выбор уйдёт на сервер в `source`/`target` автоматически, расширение скачиваемого файла
   берётся из `LANGUAGES[targetLanguage].ext`.

Желательно не давать выбрать одинаковые языки слева и справа (пара «Kotlin → Kotlin» вернёт 400).