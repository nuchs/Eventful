using Mum.Data;
using System.Collections;

namespace Mum.Pages.Index;

internal sealed class IndexViewModel : IEnumerable<AccountEntry>
{
    public IndexViewModel(AccountRepo accountRepo)
    {
        this.accountRepo = accountRepo;

        ResetAccountEntries();
    }

    public IEnumerator<AccountEntry> GetEnumerator()
    {
        return accountEntries.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal void AddAccount()
    {
        var account = new Account
        {
            Name = "Your name here",
            Org = "Where you work",
            Title = "Salutation"
        };

        accountEntries[account.Id] = new(account)
        {
            IsEditable = true
        };
    }

    internal void EditAccount(Guid id)
        => accountEntries[id].IsEditable = true;

    internal void DeleteAccount(Guid id)
        => accountEntries[id].IsDeleted = true;

    internal async Task Flush()
    {
        foreach (var entry in accountEntries.Values)
        {
            if (entry.IsDeleted)
            {
                await accountRepo.RemoveAccount(entry.Account.Id);
            }
            else
            {
                await accountRepo.AddOrUpdateAccount(entry.Account);
            }
        }

        ResetAccountEntries();
    }

    internal void ResetAccountEntries()
    {
        accountEntries.Clear();

        foreach (var account in accountRepo.GetAllAccounts())
        {
            accountEntries[account.Id] = new(account with { });
        }
    }

    private readonly Dictionary<Guid, AccountEntry> accountEntries = new();
    private readonly AccountRepo accountRepo;
}