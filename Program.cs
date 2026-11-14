using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

string? token = Environment.GetEnvironmentVariable("BOT_TOKEN");

if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Не найден токен BOT_TOKEN!");
    return;
}

using var cts = new System.Threading.CancellationTokenSource();
var bot = new TelegramBotClient(token, cancellationToken: cts.Token);

//var keyboard = new ReplyKeyboardMarkup(new KeyboardButton[][]
//{
//    new KeyboardButton[] { new("🎲 Случайный совет") },
//    new KeyboardButton[] { new("🌱 Жизнь"), new("📚 Учёба") },
//    new KeyboardButton[] { new("💼 Работа"), new("🔥 Мотивация") },
//    new KeyboardButton[] { new("❤️ Отношения"), new("ℹ️ Помощь") }
//})
//{
//    ResizeKeyboard = true,
//    IsPersistent = true
//};


var lastAdvice = new ConcurrentDictionary<long, string>();

var Categories = new Dictionary<string, (string Name, string[] Advice)>
{
    ["🌱 Жизнь"] = ("Жизнь", new[]
    {
        "Цени своё время, ведь его невозможно вернуть.",
        "Не сравнивай свою жизнь с жизнью других людей.",
        "Каждый день старайся узнавать что-нибудь новое.",
        "Не бойся ошибаться. Ошибки помогают развиваться.",
        "Учись радоваться небольшим достижениям."
    }),

    ["📚 Учёба"] = ("Учёба", new[]
    {
        "Занимайся по 30 минут каждый день.",
        "Не бойся задавать вопросы, если чего-то не понимаешь.",
        "Делай небольшие перерывы во время занятий.",
        "Практика часто полезнее простого чтения теории.",
        "Повторяй изученное через несколько дней."
    }),

    ["💼 Работа"] = ("Работа", new[]
    {
        "Начинай день с самой важной задачи.",
        "Составляй список дел перед работой.",
        "Разбивай большие задачи на маленькие этапы.",
        "Не бойся просить помощи у коллег.",
        "Выделяй время на обучение новым навыкам." 
    }),

    ["🔥 Мотивация"] = ("Мотивация", new[]
    {
        "Начни с пяти минут. Главное — начать.",
        "Маленький прогресс лучше полного бездействия.",
        "Не сдавайся, если результат не появился сразу.",
        "Каждая большая цель начинается с первого шага.",
        "Сосредоточься на прогрессе, а не на совершенстве."
    }),

    
    ["❤️ Отношения"] = ("Отношения", new[]
    {
        "Умей не только говорить, но и внимательно слушать.",
        "Будь честен с людьми и с самим собой.",
        "Не забывай благодарить людей за помощь.",
        "Уважай чужое мнение, даже если не согласен.",
        "Уделяй время людям, которые тебе дороги."
    })
};

var keyboard = new ReplyKeyboardMarkup
(
    Categories.Keys
        .Select(key => new KeyboardButton(key))
        .Chunk(2)
        .Prepend(new[] { new KeyboardButton("🎲 Случайный совет") })
        .Append(new[] { new KeyboardButton("ℹ️ Помощь") })
        .ToArray()
)
{
    ResizeKeyboard = true,
    IsPersistent = true
};

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

async Task HandleMessage(Message message, UpdateType type)
{
    if (message.Text is null)
        return;

    string text = message.Text.Trim();
    long userId = message.From?.Id ?? message.Chat.Id;

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

    string? category = null;

    bool randomAdvice = text == "🎲 Случайный совет";

    if (!randomAdvice) {
        if (!Categories.TryGetValue(text, out var SelectedCategory))
        {
            await bot.SendMessage(
                message.Chat,
                "Пожалуйста, выбери одну из кнопок меню 👇",
                replyMarkup: keyboard
            );

            return;
        }
        category = SelectedCategory.Name;
    }

    var possible = Categories
        .Where(a => category == null || a.Value.Name == category)
        .SelectMany(a => a.Value.Advice.Select(c => (Category: a.Value.Name, Text: c)))
        .ToArray();

    if (lastAdvice.TryGetValue(userId, out var previous))
    {
        var newAdvice = possible
            .Where(a => a.Text != previous)
            .ToArray();

        if (newAdvice.Length > 0)
            possible = newAdvice;
    }

    var selected = possible[Random.Shared.Next(possible.Length)];

    lastAdvice[userId] = selected.Text;

    await bot.SendMessage(
        message.Chat,
        $"💡 Твой случайный совет:\n\n" +
        $"{selected.Text}\n\n" +
        $"📌 Категория: {selected.Category}",
        replyMarkup: keyboard
    );
}
