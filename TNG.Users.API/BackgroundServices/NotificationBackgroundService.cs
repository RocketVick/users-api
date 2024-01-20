using EasyNetQ;
using MediatR;
using TNG.Users.API.Contracts;
using TNG.Users.API.Features.Notification;

namespace TNG.Users.API.BackgroundServices;

public class NotificationBackgroundService(IBus bus, IServiceScopeFactory serviceScopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bus.PubSub.Subscribe<ActivityCreated>("ActivityCreatedSub", HandleActivityCreated, cancellationToken: stoppingToken);
        // если операция не отменена, то выполняем задержку в 200 миллисекунд
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(200, stoppingToken);
        }
        await Task.CompletedTask;
    }

    async Task HandleActivityCreated(ActivityCreated message)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new NotifyActivityCreated.Command { ActivityId = message.Id });
    }
}