using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Threading.Tasks;
using Discord;

namespace Mute.Services
{
    public class IouDatabaseService
    {
        #region SQL
        private const string InsertSql = "INSERT INTO `IOU_Debts` (`LenderId`, `BorrowerId`, `Amount`, `Unit`, `Note`) VALUES (@LenderId, @BorrowerId, @Amount, @Unit, @Note)";

        private const string FindOwedByPerson = @"SELECT *
            FROM (
                SELECT `lent`.`Unit`, `lent`.`Amount` - ifnull(`borrowed`.`Amount`, 0) AS 'Amount', `LenderId`, @PersonId as `BorrowerId`
            FROM (
                SELECT `Unit`, Sum(`Amount`) as 'Amount', `LenderId`
            FROM IOU_Debts
            WHERE `BorrowerId` = @PersonId
            GROUP BY `Unit`, `LenderId`
            ) lent
                LEFT OUTER JOIN (
                SELECT `Unit`, Sum(`Amount`) as 'Amount', `BorrowerId`
            FROM IOU_Debts
            WHERE `LenderId` = @PersonId
            GROUP BY `Unit`, `BorrowerId`
            ) borrowed
                ON `lent`.`LenderId` = `borrowed`.`BorrowerId` AND `lent`.`Unit` = `borrowed`.`Unit`
            )
            WHERE `Amount` > 0";

        private const string FindLentByPerson = @"SELECT *
            FROM (
                SELECT `lent`.`Unit`, `lent`.`Amount` - ifnull(`borrowed`.`Amount`, 0) AS 'Amount', `BorrowerId`, @PersonId as `LenderId`
            FROM (
                SELECT `Unit`, Sum(`Amount`) as 'Amount', `BorrowerId`
            FROM IOU_Debts
            WHERE `LenderId` = @PersonId
            GROUP BY `Unit`, `BorrowerId`
            ) lent
                LEFT OUTER JOIN (
                SELECT `Unit`, Sum(`Amount`) as 'Amount', `LenderId`
            FROM IOU_Debts
            WHERE `BorrowerId` = @PersonId
            GROUP BY `Unit`, `LenderId`
            ) borrowed
                ON `lent`.`BorrowerId` = `borrowed`.`LenderId` AND `lent`.`Unit` = `borrowed`.`Unit`
            )
            WHERE `Amount` > 0";
        #endregion

        private readonly DatabaseService _database;

        public IouDatabaseService(DatabaseService database)
        {
            _database = database;

            _database.Exec("CREATE TABLE IF NOT EXISTS `IOU_Debts` (`ID` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, `LenderId` TEXT NOT NULL, `BorrowerId` TEXT NOT NULL, `Amount` TEXT NOT NULL, `Unit` TEXT NOT NULL, `Note` TEXT);");
            _database.Exec("CREATE INDEX IF NOT EXISTS `DebtsIndexBorrowerLeading` ON `IOU_Debts` ( `BorrowerId` ASC, `Unit` ASC, `LenderId` ASC, `Amount` ASC )");
            _database.Exec("CREATE INDEX IF NOT EXISTS `DebtsIndexLenderLeading` ON `IOU_Debts` ( `LenderId` ASC, `Unit` ASC, `BorrowerId` ASC, `Amount` ASC )");
        }

        public async Task Insert(IUser lender, IUser borrower, decimal amount, string unit, string note)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertSql;
                cmd.Parameters.Add(new SQLiteParameter("@LenderId", System.Data.DbType.String) {Value = lender.Id.ToString()});
                cmd.Parameters.Add(new SQLiteParameter("@BorrowerId", System.Data.DbType.String) {Value = borrower.Id.ToString()});
                cmd.Parameters.Add(new SQLiteParameter("@Amount", System.Data.DbType.String) {Value = amount.ToString(CultureInfo.InvariantCulture)});
                cmd.Parameters.Add(new SQLiteParameter("@Unit", System.Data.DbType.String) {Value = unit.ToLowerInvariant()});
                cmd.Parameters.Add(new SQLiteParameter("@Note", System.Data.DbType.String) {Value = note ?? ""});

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private static async Task<IReadOnlyList<Owed>> ParseOwed(DbDataReader reader)
        {
            var debts = new List<Owed>();

            while (await reader.ReadAsync())
            {
                debts.Add(new Owed(
                    ulong.Parse((string)reader["LenderId"]),
                    ulong.Parse((string)reader["BorrowerId"]),
                    decimal.Parse(reader["Amount"].ToString()),
                    (string)reader["Unit"])
                );
            }

            return debts;
        }

        public async Task<IReadOnlyList<Owed>> GetOwed(IUser borrower)
        {
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = FindOwedByPerson;
                    cmd.Parameters.Add(new SQLiteParameter("@PersonId", System.Data.DbType.String) {Value = borrower.Id.ToString()});

                    using (var results = await cmd.ExecuteReaderAsync())
                        return await ParseOwed(results);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<IReadOnlyList<Owed>> GetLent(IUser lender)
        {
            try
            {
                using (var cmd = _database.CreateCommand())
                {
                    cmd.CommandText = FindLentByPerson;
                    cmd.Parameters.Add(new SQLiteParameter("@PersonId", System.Data.DbType.String) {Value = lender.Id.ToString()});

                    using (var results = await cmd.ExecuteReaderAsync())
                        return await ParseOwed(results);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public struct Owed
    {
        public readonly ulong LenderId;
        public readonly ulong BorrowerId;
        public readonly decimal Amount;
        public readonly string Unit;

        public Owed(ulong lenderId, ulong borrowerId, decimal amount, string unit)
        {
            LenderId = lenderId;
            BorrowerId = borrowerId;
            Amount = amount;
            Unit = unit;
        }
    }
}
