using System.Collections.Immutable;
using System.Linq;
using Reventuous;
using static Reventuous.Tests.Model.Events; 

namespace Reventuous.Tests.Model {
    public class BankAccount : Aggregate<BankAccountState, BankAccountId> {
        public static BankAccount CreateAccount(BankAccountId id) {
            var account = new BankAccount();
            account.Apply(new AccountCreated(id));
            return account;
        }

        public void LodgeAmount(decimal amount) {
            Apply(new AmountLodged(amount));
        }

        public void WithdrawAmount(decimal amount) {
            Apply(new AmountWithdrawn(amount));
        }
    }

    public record BankAccountState : AggregateState<BankAccountState, BankAccountId> {
        public decimal Balance { get; init; }

        public override BankAccountState When(object @event)
            => @event switch {
                AccountCreated created => this with { Id = new BankAccountId(created.AccountNumber), Balance = 0 },
                AmountLodged lodged => this with { Balance = Balance + lodged.Amount },
                AmountWithdrawn withdrawn => this with { Balance = Balance - withdrawn.Amount },
                _ => this
            };
    }

    public record BankAccountId(string Value) : AggregateId(Value);

}