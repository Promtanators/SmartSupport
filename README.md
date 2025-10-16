# Руководство по запуску проекта

Данный проект использует **Docker Compose** для упрощения развёртывания. Перед началом убедитесь, что на вашей машине установлены **Docker** и **Docker Compose**.

## 1. Клонирование репозитория

```bash
git clone https://github.com/Promtanators/UserSuccessCatalyst.git
cd UserSuccessCatalyst
````

## 2. Настройка переменных окружения

В корневой директории проекта создайте файл `.env` и добавьте в него ваш API-ключ:

```env
SCIBOX_API_KEY=your_actual_token_here
```

> **Примечание:** Замените `your_actual_token_here` на действительный токен SciBox.

## 3. Запуск контейнеров

Для сборки и запуска всех сервисов в фоновом режиме выполните:

```bash
docker compose up -d --build
```

После успешного запуска:

* **Backend** доступен по адресу: [http://localhost:5000](http://localhost:5000)
* **Frontend** доступен по адресу: [http://localhost:8080](http://localhost:8080)