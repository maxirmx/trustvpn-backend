[![build-and-push](https://github.com/maxirmx/trustvpn-backend/actions/workflows/build-and-push.yml/badge.svg)](https://github.com/maxirmx/trustvpn-backend/actions/workflows/build-and-push.yml)

# O!Service backend server API
## Установка
Приложение поставляется в виде трёх контейнеров докер.
В качестве примера развёртывания приведён файл ```docker-compose-ghrc.yml```

```
services:
  trustvpn-backend:
    container_name: trustvpn-backend
    image: ghcr.io/maxirmx/trustvpn-backend:latest
    environment:
      ConnectionStrings__DefaultConnection: "Host=trustvpn-db;Database=postgres;Username=postgres;Password=postgres"
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_HTTP_PORTS: 8080
      ASPNETCORE_HTTPS_PORTS: 8081
      ASPNETCORE_Kestrel__Certificates__Default__Path: "/etc/certificate/sw_consulting.pfx"  
      ASPNETCORE_Kestrel__Certificates__Default__Password: <password>
    ports:
      - "8080:8080"
      - "8081:8081"
    depends_on:
      trustvpn-db:
        condition: service_healthy
      trustvpn-container:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "localhost:8080/api/auth/status"]
      interval: 10s
      timeout: 5s
      retries: 2
    volumes:
      - /etc/nginx/certificate:/etc/certificate
      - /var/run/docker.sock:/var/run/docker.sock
      - /var/o-service/logs:/app/logs

  trustvpn-db:
    container_name: trustvpn-db
    image: postgres:16.2
    environment:
      - PGUSER=postgres
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_DB=postgres
    healthcheck:
      test: ["CMD-SHELL", "pg_isready"]
      interval: 10s
      timeout: 5s
      retries: 5
    volumes:
      - /var/o-service/pgdata:/var/lib/postgresql/data

  trustvpn-container:
    container_name: trustvpn-container
    image: ghcr.io/maxirmx/trustvpn-container:latest
    command: ["trustvpn-container-if-start", "-u", "o-service.sw.consulting"]
    ports:
      - "1194:1194/udp"
    cap_add:
      - NET_ADMIN
    sysctls:
      - net.ipv6.conf.all.disable_ipv6=0
      - net.ipv6.conf.all.forwarding=1
    healthcheck:
      test: ["CMD", "openvpn-check"]
      interval: 10s
      timeout: 5s
      retries: 2
    volumes:
      - /var/o-service/ovpn:/etc/openvpn
```



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
