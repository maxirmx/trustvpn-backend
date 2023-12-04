# TrustVPN backend server API
## Swagger
После установки документация swagger доступна по адресу <host>:<port>/swagger

## Пример использования
### Захожу администратором

```
POST <host>:<port>/api/auth/login
{
   "Password": "... посылал отдельно ...",
   "Email": "ivanov@example.com"
}
```
Ответ:
```
{
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjUiLCJuYmYiOjE3MDExOTUyMDYsImV4cCI6MTcwMTgwMDAwNiwiaWF0IjoxNzAxMTk1MjA2fQ.ZF21pHD4C-7bCZuyAuICcYGxxmOIOjGeg6F4n8zaxvU",
    "id": 5,
    "firstName": "Иван",
    "lastName": "Иванов",
    "patronimic": "Иванович",
    "email": "ivanov@example.com",
    "isAdmin": true,
    "profileId": 2,
    "config": ""
}
```
Токен - JSON Web Token (RFC 7519), используется для авторизации в следующих запросах. Нужно передавать в HTTP заголовке "Authorization". Всё остальноe - для справки.

### Создаю пользователя
```
POST <host>:<port>/api/user/add
{
    "FirstName": "Роман",
    "LastName": "Ойра-Ойра",
    "Password": "12345",
    "Email": "oyra@example.com",
    "IsAdmin": false,
    "ProfileId": 2
}
```

* profileId = 1   --  блокировка
* profileId = 2   --  профиль с ограничением пропускной способности
* profileId = 3   --  профиль без ограничения пропускной способности

Ответ:
```
{
    "id": 12
}
```
Обязательные поля "Email", "IsAdmin", "ProfileId"
Отсутсвие поля трактуется как пустая строка, а не null

### Получаю конфигурацию пользователя
```
GET <host>:<port>/api//user/12
```
Ответ:
```
{
    "id": 12,
    "firstName": "Роман",
    "lastName": "Ойра-Ойра",
    "patronimic": "",
    "email": "oyra@example.com",
    "isAdmin": false,
    "profileId": 2,
    "config": " ... "
}
```
В поле config будет содержимое конфигурацинного файла. Нужно просто сохранить с расширением  'openvpn'
Eсли поле config пустое, значит пользователь заблокирован (profileId=1). Это ошибка, конфиг будет выдаваться всегда.

### Меняю профиль пользователя
```
PUT <host>:<port>/api//user/12
{
     "ProfileId": 3
}
```
Нужно передавать те поля, которые хочется поменять. Можно поменять несколько полей одним запросом.
