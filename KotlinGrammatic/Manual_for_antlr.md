# Установка и настройка ANTLR для проекта Kotlin++

ANTLR требует Java Runtime. Проверьте:

```bash
java -version
```

Перейдите по ссылке и скачайте файл

https://www.antlr.org/download/antlr-4.13.2-complete.jar

Рекомендую создать папку antlr\ рядом с корнем ропозитория Kotlin-plus-plus\ и туда поместить этот файл

Ещё написать это:

```bash
dotnet add package Antlr4.Runtime.Standard
```

Чтобы сгенерировать необходимые файлы для работы Program.cs надо запустить такие команды в корне репозитория

```bash
java -jar ..\antlr\antlr-4.13.2-complete.jar -Dlanguage=CSharp KotlinGrammatic\KotlinLexer.g4
java -jar ..\antlr\antlr-4.13.2-complete.jar -Dlanguage=CSharp -visitor KotlinGrammatic\KotlinParser.g4
```

Обратно в репозиторий все нагенерированные файлы добавлять не надо 