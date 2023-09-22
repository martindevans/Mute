//using System.Threading.Tasks;
//using Discord;
//using Discord.Commands;
//using Discord.Interactions;
//using Discord.WebSocket;
//using JetBrains.Annotations;
//using Mute.Moe.Discord.Modules.Payment;
//using Mute.Moe.Discord.Services.Users;
//using Mute.Moe.Services.Payment;

//namespace Mute.Moe.Discord.Interactions.Payment;

//[UsedImplicitly]
//public class IOU
//    : InteractionModuleBase
//{
//    private readonly ITransactions _transactions;
//    private readonly BaseSocketClient _client;
//    private readonly IUserService _users;

//    public IOU(ITransactions transactions, BaseSocketClient client, IUserService users)
//    {
//        _transactions = transactions;
//        _client = client;
//        _users = users;
//    }

//    [SlashCommand("iou", "Declare that you owe someone something")]
//    public async Task CreateDebt(IUser user, decimal amount, string unit, [Remainder] string? note = null)
//    {
//        if (amount < 0)
//        {
//            await RespondAsync("You cannot owe a negative amount!");
//            return;
//        }

//        var message = $"{Context.User.Mention} owes {TransactionFormatting.FormatCurrency(amount, unit)} to {user.Mention}";

//        var confirmed = await InteractionUtility.ConfirmAsync(_client, Context.Channel, TimeSpan.FromMinutes(1), message);
//        if (!confirmed)
//        {
//            await RespondAsync("Confirmation timed out. Cancelled transaction!");
//            return;
//        }

//        await _transactions.CreateTransaction(user.Id, Context.User.Id, amount, unit, note, DateTime.UtcNow);
//        await RespondAsync(message);
//    }

//    //[SlashCommand("transactions", "I will show all your transactions")]
//    //public async Task ListTransactions(IUser? other = null)
//    //{
//    //    //Get all transactions in both directions
//    //    var all = (await _transactions.GetAllTransactions(Context.User.Id, other?.Id)).ToList();
//    //    if (all.Count == 0)
//    //        await RespondAsync("No transactions");
//    //    else
//    //        await DisplayTransactions(all);
//    //}

//    #region balance query
//    //[SlashCommand("io", "I will tell you what you owe")]
//    //public async Task ListDebtsByBorrower(IUser? lender = null)
//    //{
//    //    await ShowBalances(lender, b => b.UserA == Context.User.Id ^ b.Amount > 0, "You are debt free");
//    //}

//    //[SlashCommand("oi", "I will tell you what you are owed")]
//    //public async Task ListDebtsByLender(IUser? borrower = null)
//    //{
//    //    await ShowBalances(borrower, b => b.UserB == Context.User.Id ^ b.Amount > 0, "Nobody owes you anything");
//    //}

//    //[SlashCommand("balance", "I will tell you your balance")]
//    //public async Task ShowBalance(IUser? other = null)
//    //{
//    //    await ShowBalances(other, _ => true, "No non-zero balances");
//    //}

//    //private async Task ShowBalances(IUser? other, Func<IBalance, bool> filter, string none)
//    //{
//    //    var balances = (await _transactions.GetBalances(Context.User.Id, other?.Id)).Where(filter).ToArray();
//    //    if (balances.Length == 0)
//    //        await RespondAsync(none);
//    //    else
//    //        await DisplayBalances(balances);
//    //}
//    #endregion

//    #region helpers
//    private async Task<string> FormatBalance(IBalance bal)
//    {
//        return await bal.Format(_users);
//    }

//    //private async Task DisplayTransactions(IReadOnlyCollection<ITransaction> transactions)
//    //{
//    //    var tsx = transactions.Select(tsx => tsx.Format(_users)).ToList();

//    //    //If the number of transactions is small, display them all.
//    //    //Otherwise batch and show them in pages
//    //    if (transactions.Count < 13)
//    //        await RespondAsync(string.Join("\n", tsx));
//    //    else
//    //        await PagedReplyAsync(new PaginatedMessage { Pages = tsx.Batch(10).Select(d => string.Join("\n", d)) });
//    //}

//    //private async Task DisplayBalances(IReadOnlyCollection<IBalance> balances)
//    //{
//    //    async Task DebtTotalsPerUnit()
//    //    {
//    //        if (balances.Count > 1)
//    //        {
//    //            var totals = balances.GroupBy(a => a.Unit)
//    //                                 .Select(a => (a.Key, a.Sum(o => o.Amount)))
//    //                                 .OrderByDescending(a => a.Item2)
//    //                                 .ToArray();

//    //            var r = new StringBuilder("```\nTotals:\n");
//    //            foreach (var (key, amount) in totals)
//    //                r.AppendLine($" => {TransactionFormatting.FormatCurrency(amount, key)}");
//    //            r.AppendLine("```");

//    //            await FollowupAsync(r.ToString());
//    //        }
//    //    }

//    //    var balancesList = new List<string>(balances.Count);
//    //    foreach (var balance in balances)
//    //        balancesList.Add(await FormatBalance(balance));

//    //    //If the number of transactions is small, display them all.
//    //    //Otherwise batch and show them in pages
//    //    if (balancesList.Count < 10)
//    //        await RespondAsync(string.Join("\n", balancesList));
//    //    else
//    //        await PagedReplyAsync(new PaginatedMessage { Pages = balancesList.Batch(7).Select(d => string.Join("\n", d)) });

//    //    await DebtTotalsPerUnit();
//    //}
//    #endregion
//}