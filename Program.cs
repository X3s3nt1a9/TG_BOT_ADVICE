
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

// Получаем токен
string? token = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Не найден токен BOT_TOKEN!");
    return;
}

using var cts = new System.Threading.CancellationTokenSource();
var bot = new TelegramBotClient(token, cancellationToken: cts.Token);

// Создаём клавиатуру
var keyboard = new ReplyKeyboardMarkup(new KeyboardButton[][]
{
    new KeyboardButton[] { new("🎲 Случайный совет") },
    new KeyboardButton[] { new("🌱 Жизнь"), new("📚 Учёба") },
    new KeyboardButton[] { new("💼 Работа"), new("🔥 Мотивация") },
    new KeyboardButton[] { new("❤️ Отношения"), new("ℹ️ Помощь") }
})
{
    ResizeKeyboard = true,
    IsPersistent = true
};

// Запоминаем последний совет пользователя
var lastAdvice = new ConcurrentDictionary<long, string>();

// Список советов
var adviceList = new (string Category, string Text)[]
{
    // Жизнь
    ("Жизнь", "Цени своё время, ведь его невозможно вернуть."),
    ("Жизнь", "Не сравнивай свою жизнь с жизнью других людей."),
    ("Жизнь", "Каждый день старайся узнавать что-нибудь новое."),
    ("Жизнь", "Не бойся ошибаться. Ошибки помогают развиваться."),
    ("Жизнь", "Учись радоваться небольшим достижениям."),

    // Учёба
    ("Учёба", "Занимайся по 30 минут каждый день."),
    ("Учёба", "Не бойся задавать вопросы, если чего-то не понимаешь."),
    ("Учёба", "Делай небольшие перерывы во время занятий."),
    ("Учёба", "Практика часто полезнее простого чтения теории."),
    ("Учёба", "Повторяй изученное через несколько дней."),

    // Работа
    ("Работа", "Начинай день с самой важной задачи."),
    ("Работа", "Составляй список дел перед работой."),
    ("Работа", "Разбивай большие задачи на маленькие этапы."),
    ("Работа", "Не бойся просить помощи у коллег."),
    ("Работа", "Выделяй время на обучение новым навыкам."),

    // Мотивация
    ("Мотивация", "Начни с пяти минут. Главное — начать."),
    ("Мотивация", "Маленький прогресс лучше полного бездействия."),
    ("Мотивация", "Не сдавайся, если результат не появился сразу."),
    ("Мотивация", "Каждая большая цель начинается с первого шага."),
    ("Мотивация", "Сосредоточься на прогрессе, а не на совершенстве."),

    // Отношения
    ("Отношения", "Умей не только говорить, но и внимательно слушать."),
    ("Отношения", "Будь честен с людьми и с самим собой."),
    ("Отношения", "Не забывай благодарить людей за помощь."),
    ("Отношения", "Уважай чужое мнение, даже если не согласен."),
    ("Отношения", "Уделяй время людям, которые тебе дороги.")
};

// Названия кнопок и соответствующие категории
var categories = new Dictionary<string, string>
{
    ["🌱 Жизнь"] = "Жизнь",
    ["📚 Учёба"] = "Учёба",
    ["💼 Работа"] = "Работа",
    ["🔥 Мотивация"] = "Мотивация",
    ["❤️ Отношения"] = "Отношения"
};

// Подключаем обработчик сообщений
bot.OnMessage += HandleMessage;

try
{
    var me = await bot.GetMe();

    Console.WriteLine($"Бот @{me.Username} запущен!");
    Console.WriteLine("Нажми Enter для остановки.");

    Console.ReadLine();
}
catch (Telegram.Bot.Exceptions.RequestException ex)
{
    Console.WriteLine("Ошибка подключения к Telegram!");
    Console.WriteLine(ex.Message);
}
finally
{
    cts.Cancel();
}

// Обработка сообщений
async Task HandleMessage(Message message, UpdateType type)
{
    if (message.Text is null)
        return;

    string text = message.Text.Trim();
    long userId = message.From?.Id ?? message.Chat.Id;

    // Главное меню
    if (text == "/start" || text == "ℹ️ Помощь")
    {
        await bot.SendMessage(
            message.Chat,
            "Привет! Я бот «Случайный совет»! 💡\n\n" +
            "Выбери интересующую категорию или получи " +
            "случайный совет прямо сейчас!",
            replyMarkup: keyboard
        );

        return;
    }

    // Проверяем нажатие кнопки случайного совета
    string? category = null;

    bool randomAdvice = text == "🎲 Случайный совет";

    // Проверяем выбор категории
    if (!randomAdvice && !categories.TryGetValue(text, out category))
    {
        await bot.SendMessage(
            message.Chat,
            "Пожалуйста, выбери одну из кнопок меню 👇",
            replyMarkup: keyboard
        );

        return;
    }

    // Получаем подходящие советы
    var possible = adviceList
        .Where(a => category == null || a.Category == category)
        .ToArray();

    // Исключаем последний совет пользователя
    if (lastAdvice.TryGetValue(userId, out var previous))
    {
        var newAdvice = possible
            .Where(a => a.Text != previous)
            .ToArray();

        if (newAdvice.Length > 0)
            possible = newAdvice;
    }

    // Выбираем случайный совет
    var selected = possible[Random.Shared.Next(possible.Length)];

    lastAdvice[userId] = selected.Text;

    // Отправляем совет
    await bot.SendMessage(
        message.Chat,
        $"💡 Твой случайный совет:\n\n" +
        $"{selected.Text}\n\n" +
        $"📌 Категория: {selected.Category}",
        replyMarkup: keyboard
    );
}
