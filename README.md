# o-backend

TrustVPN backend server

## Как это работает

### Захожу администратором

```
POST https://kreel0.samsonov.net:1443/auth/login
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
Токен - JSON Web Token (RFC 7519), используется для авторизации в следующих запросах. Нужно передавать в HTTP заголовке "Authorization".
Всё остальноt - для справки.

### Создаю пользователя
```
POST https://kreel0.samsonov.net:1443/user/add
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
    "id": 11
}
```
Обязательные поля "Email", "IsAdmin", "ProfileId"
Отсутсвие поля трактуется как пустая строка, а не null

### Получаю конфигурацию пользователя
```
GET https://kreel0.samsonov.net:1443/user/11
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

### Изменить профиль
```
PUT https://kreel0.samsonov.net:1443/user/11
{
     "FirstName": "Роман",
     "LastName": "Ойра-Ойра",
     "Email": "oyra1@example.com",
     "IsAdmin": false,
     "ProfileId": 1
}
```
Нужно передавать те поля, которые хочется поменять.
