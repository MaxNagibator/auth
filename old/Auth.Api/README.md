### Обновить/создать БД

dotnet ef database update -c Auth.Api.Data.ApplicationDbContext --project Auth.Api

### Добавить миграцию

dotnet ef migrations add CreateCategoryTable -c Auth.Api.Data.ApplicationDbContext --project Auth.Api

### Подсказки для автора, которые облегчат его жизнь (чтоб не лазить в powershell'e):

### Быстро и удобно добавить миграцию:

1) выбрать как startup project "Auth.API"
2) в nuget package manager в графе "Default Project" выбрать "Auth.Data"
3) ввести "add-migration Имя_Миграции"

### Быстро и удобно удалить миграцию:

1) выбрать как startup project "Auth.API"
2) в nuget package manager в графе "Default Project" выбрать "Auth.Data"
3) ввести "remove-migration" (удалит последнюю миграцию)

### Быстро и удобно схлопнуть несколько миграций в одну:

1) выбрать как startup project "Auth.API"
2) в nuget package manager в графе "Default Project" выбрать "Auth.Data"
3) "update-database Имя_Последней_Хорошей_Миграции"
4) "remove-migration" до тех пор, пока не останется только хорошая
5) "add-migration Имя_Новой_Миграции"

### Быстро и удобно обновить БД:

1) выбрать как startup project "Auth.API"
2) в nuget package manager в графе "Default Project" выбрать "Auth.Data"
3) "update-database"
