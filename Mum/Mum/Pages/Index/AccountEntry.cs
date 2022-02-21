using Mum.Data;

namespace Mum.Pages.Index;

internal sealed class AccountEntry
{
    internal AccountEntry(Account Account) => this.Account = Account;

    internal Account Account { get; }

    internal bool IsDeleted { get; set; }

    internal bool IsEditable { get; set; }
}