# Lleitmotif Telegram Bot 🤖

Telegram-бот для внутрішньої спільноти з інтерактивною щоденною грою, системою лідербордів та вбудованою адмін-панеллю.

## 🛠 Технологічний стек
* **Мова:** C#
* **Фреймворк:** .NET 8 (Worker Service / Long Polling)
* **База даних:** SQLite (Entity Framework Core)
* **Бібліотеки:** `Telegram.Bot` (v22+), `NLog`

## ⚙️ Архітектура
* **Отримання оновлень:** Реалізовано через Long Polling (`BotBackgroundService`), що дозволяє запускати бота на ізольованих серверах без необхідності налаштування HTTPS/SSL та Webhooks.
* **Кешування текстів:** Усі текстові відповіді бота зберігаються в базі даних та кешуються в оперативній пам'яті (`MessageCacheService`) для мінімізації запитів до БД.
* **Безпека:** Доступ до адмін-команд перевіряється на рівні Telegram ID. Токен та ID адміністратора не зберігаються в коді.

## 🚀 Розгортання на Linux (Ubuntu / Systemd)

### Компіляція проекту
Виконати локально для підготовки файлів:

```bash
dotnet publish -c Release -r linux-x64 --self-contained false
```

### Налаштування змінних середовища (Systemd)
Бот вимагає наявності двох змінних для роботи.
Приклад файлу конфігурації служби /etc/systemd/system/lleitmotifbot.service:
```bash
[Unit]
Description=Lleitmotif Telegram Bot
After=network.target

[Service]
WorkingDirectory=/var/www/bot
ExecStart=/usr/bin/dotnet /var/www/bot/backend.dll
Restart=always
RestartSec=10
SyslogIdentifier=lleitmotifbot

Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=BotToken=ТЕЛЕГРАМ_ТОКЕН
Environment=AdminSettings__MasterAdminId=ТЕЛЕГРАМ_ID

[Install]
WantedBy=multi-user.target
```

###  Запуск служби
```bash
sudo systemctl daemon-reload
sudo systemctl enable lleitmotifbot.service
sudo systemctl start lleitmotifbot.service
```

###  Основні команди
/start — ініціалізація та привітання.
/dick — щоденна гра з динамічним нарахуванням/списанням балів.
/top — рейтинг гравців поточного чату.
/admin — панель адміністратора (доступна лише для MasterAdminId).