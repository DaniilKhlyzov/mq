# mq
Блок по очередям сообщений

## 0. Подготовка
Установи или скачай Docker-образ RabbitMQ.

## 1. Настраиваем общие модели
Для работы с RabbitMQ будем использовать библиотеку MassTransit. Она умеет автоматически создавать все связи, но для этого нужно, чтобы она использовала модели с одинаковым fully qualified name (то есть должны совпадать и названия классов, и неймспейсы). Поэтому удобнее сразу использовать одни и те же модели для создания биндингов.

Создай в репозитории проект с моделями под названием `Models` командой
```bash
dotnet new classlib -n Models
```

Подключи его как зависимость в `consumer` и `producer`.

После этого создай там модель с любым названием и любым наполнением.

## 2. Настраиваем producer
Producer - приложение, отправляющее сообщение в очередь.

В оба приложения уже подключен MassTransit (обрати внимание! Для своих целей нужно использовать 8-ую версию).

Для подключения к локальному RabbitMQ допиши в `Program.cs` проекта `producer` следующий код:
```cs
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) => { 
        cfg.Host("localhost", "/", h => { 
            h.Username("guest");
            h.Password("guest");
        });
    });
});
```

Что есть что:
- `localhost` - адрес хоста (без порта);
- `/` - так называемый "virtual host". В RabbitMQ один и тот же кластер может обслуживать разных потребителей, чтобы разграничить очереди разных потребителей, существуют виртуальные хосты;
- `guest` и `guest` - логин и пароль. В реальных проектах они должны получаться из конфигурации приложения.

Теперь нужно создать класс, который будет отправлять сообщения в очередь. Он может выглядеть вот так:
```cs
public class MyPublisher
{
    private readonly ISendEndpointProvider _sendEndpointProvider;

    public MyPublisher(ISendEndpointProvider sendEndpointProvider)
    {
        _sendEndpointProvider = sendEndpointProvider;
    }

    public async Task Publish(string content)
    {
        var publishEndpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri("queue:my-queue"));
        await publishEndpoint.Send(new MyMessage { MessageId = Guid.NewGuid(), Content = content });
    }
}
```
Отправлять он должен ту модельку, которую ты положил в `Models`.

Тонкий момент - адрес очереди. Его не всегда обязательно задавать, равно как и использовать `sendEndpointProvider`, существуют другие способы подключения, но мы рассмотрим этот. Адрес начинается всегда с `queue`, после двоеточия идёт название очереди. Так мы получаем больше ручного контроля над подключением, например, если есть клиенты на других платформах (MassTransit сам умеет именовать очереди).

Теперь зарегистрируй класс в DI-контейнере приложения `producer` и допиши в обработчик запроса `/weatherforecast` отправку сообщения с помощью этого класса.

Запусти приложение, отправь в него запрос с помощью файла `mq.http` в том же проекте, посмотри в мониторинге RabbitMQ, создалось ли сообщение. Мониторинг RabbitMQ: http://localhost:15672/ Интересует вкладка Management, где можно посмотреть состояние очередей.

## 3. Настраиваем consumer
Теперь настроим потребителя - приложение, которое читает сообщения из очереди.

Для начала нужен класс, реализующий интерфейс `IConsumer<T>`, где `T` - твоя модель. Пусть он выглядит так:
```cs
public class MyConsumer: IConsumer<MyMessage>
{
    public Task Consume(ConsumeContext<MyMessage> context)
    {
        Console.WriteLine($"[{context.Message.MessageId}] {context.Message.Content}");
        return Task.CompletedTask;
    }
}
```

Подключим теперь его в `Program.cs`:
```cs
builder.Services.AddMassTransit(config =>
{
    config.UsingRabbitMq((context, cfg) => { 
        cfg.Host("localhost", "/", h => { 
            h.Username("guest");
            h.Password("guest");
        });

        cfg.ReceiveEndpoint("my-queue", e =>
        {
            e.UseInMemoryOutbox(context);
            e.Consumer<MyConsumer>();
        });
    });
});
```
Обрати внимание, конфигурация похожая, но появилась настройка чтения конкретной очереди.

Ещё интересная опция - `e.UseInMemoryOutbox(context);`. Она реализует паттерн `Outbox`, при котором сообщения сохраняются в базу, чтобы они не терялись при обрыве сети. В данном случае мы сохраняем сообщения в память вместо отдельной базы.

Теперь запусти оба приложения, попробуй отправить из первого сообщение во второе. Проверь, что они читаются (как - зависит от твоей реализации consumer'а). Ещё можно посмотреть в мониторинг RabbitMQ на состояние очереди, там будет видно, разгребается ли она.

## 4.* TestHarness
Не всегда удобно вручную проверять отправку сообщений (хотя часто это нужно). Разработчики MassTransit придумали концепцию `TestHarness` - эмуляцию отправки сообщений по абстрактной (а можно и по реальной) шине с методами для проверки того, что это действительно произошло. Подробнее можно прочитать на [странице про TestHarness](https://masstransit.io/documentation/concepts/testing).

Попробуй написать тесты, которые будут проверять то же самое, что ты проделал в предыдущих пунктах, но автоматизированно. RabbitMQ в тесты подключать не нужно.