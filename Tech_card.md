== Техническая карта проекта ==

# Используемые технологии:

ANTLR - синтаксический анализатор
С# - для бэкенд-слоя проекта
ASP.NET - интеграция фронтенд и бэкенд слоёв
JS, HTML - фронтенд слой, GUI проекта

# Схема проекта:

```
  
  ====FRONTEND====  |            ====HTTP=INTEGRATION=UNIT====           |         ====BACKEND====
  
  
                                                                         /-->  ANTLR + Грамматика
  GUI интерфейс  <----->  ASP.NET обёртка для обработки http зпросов  <--+-->  Токенайзинг и перевод
                                                                         \-->  Обработка ввода и вывод
  
  
  User -> Graphical interface -> Http in -> Http server processing -> Sintax manipulating -> 
              -> Translating -> Http server processing -> Http out -> Graphical interface -> User
```
