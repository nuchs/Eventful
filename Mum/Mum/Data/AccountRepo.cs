using EventStore.Client;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace Mum.Data;

internal sealed class AccountRepo
{
    public AccountRepo(EventStoreClient es, ILogger<AccountRepo> log)
    {
        this.es = es;
        this.log = log;
    }

    internal int NumberAccounts => accounts.Count;

    internal async Task Initialise()
    {
        log.LogInformation("Initialising account repository");

        var events = es.ReadStreamAsync(Direction.Forwards, AccountStream, StreamPosition.Start);

        await foreach (var rawEvent in events)
        {
            ProcessEvent(rawEvent);
        }

        log.LogInformation("Account repository initialised.");
    }

    internal IEnumerable<Account> GetAllAccounts()
        => accounts.Values;

    internal async Task AddOrUpdateAccount(Account account)
    {
        var eventType = AccountEventTypes.Added;

        if (accounts.ContainsKey(account.Id))
        {
            if (account == accounts[account.Id])
            {
                return;
            }

            eventType = AccountEventTypes.Updated;
        }

        accounts.AddOrUpdate(account.Id, _ => account, (_, _) => account);

        await RecordAccountEvent(account, eventType);

        log.LogDebug("{} account {} for {}", eventType, account.Id, account.Name);
    }

    internal async Task RemoveAccount(Guid accountId)
    {
        if (accounts.Remove(accountId, out var account))
        {
            await RecordAccountEvent(account.Id, AccountEventTypes.Deleted);

            log.LogDebug("Removed account {} for {}", accountId, account?.Name);
        }
    }

    private async Task RecordAccountEvent<T>(T payload, AccountEventTypes eventType)
    {
        var json = JsonSerializer.Serialize(payload);
        var eventData = new EventData(
           Uuid.NewUuid(),
           eventType.ToString(),
           Encoding.UTF8.GetBytes(json)
       );

        await es.AppendToStreamAsync(
            AccountStream,
            StreamState.Any,
            new[] { eventData }
        );
    }

    private void ProcessEvent(ResolvedEvent rawEvent)
    {
        switch (GetEventType(rawEvent))
        {
            case AccountEventTypes.Added:
            case AccountEventTypes.Updated:
                var accountData = DeserialiseEvent<Account>(rawEvent);
                accounts.AddOrUpdate(accountData.Id, _ => accountData, (_, _) => accountData);
                log.LogDebug("{} account {} for {}", rawEvent.Event.EventType, accountData.Id, accountData.Name);
                break;

            case AccountEventTypes.Deleted:
                var accountId = DeserialiseEvent<Guid>(rawEvent);
                accounts.Remove(accountId, out var account);
                log.LogDebug("Removed account {} for {}", accountId, account?.Name);
                break;

            default:
                log.LogWarning("Unable to process event - Unknown event type: {}", rawEvent.Event.EventType);
                break;
        }
    }

    private T DeserialiseEvent<T>(ResolvedEvent raw)
    {
        try
        {
            var json = Encoding.UTF8.GetString(raw.Event.Data.Span);
            var value = JsonSerializer.Deserialize<T>(json);

            if (value != null)
            {
                return value;
            }

            throw new NullReferenceException("Event contained no data");
        }
        catch
        {
            log.LogError("Failed to deserialise {} event {}", raw.Event.EventType, raw.Event.EventId);
            throw;
        }
    }

    private AccountEventTypes GetEventType(ResolvedEvent rawEvent)
    {
        if (Enum.TryParse<AccountEventTypes>(rawEvent.Event.EventType, out var type))
        {
            return type;
        }
        else
        {
            return AccountEventTypes.Unknown;
        }
    }

    private const string AccountStream = "accounts2";
    private readonly ConcurrentDictionary<Guid, Account> accounts = new();
    private readonly EventStoreClient es;
    private readonly ILogger log;
}
