//Thread pool
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    private static readonly object balanceLock = new object();
    private static AccountState accountState = new AccountState();

    static async Task Main(string[] args)
    {
        string inputFolder = "/Users/miroshnicheenko/Proj/emul1/emulator/input";
        string outputFolder = "/Users/miroshnicheenko/Proj/emul1/emulator/output";

        List<string> csvFiles = Directory.GetFiles(inputFolder, "*.csv")
                                          .OrderBy(file => file)
                                          .ToList();

        var tasks = csvFiles.Select(csvFile => ProcessFileAsync(csvFile, outputFolder)).ToList();

        await Task.WhenAll(tasks);

        var summaryResult = new SummaryResult
        {
            FinalBalance = accountState.Balance,
            SuccessfulTransactions = accountState.SuccessfulTransactions,
            UnsuccessfulTransactions = accountState.UnsuccessfulTransactions,
            TotalFilesProcessed = csvFiles.Count
        };

        File.WriteAllText(Path.Combine(outputFolder, "result.json"), summaryResult.ToJson());
    }

    static async Task ProcessFileAsync(string file, string outputFolder)
    {
        try
        {
            var lines = await File.ReadAllLinesAsync(file);

            foreach (var line in lines)
            {
                await ProcessLineAsync(line, file, outputFolder);
            }
        }
        catch (Exception ex)
        {
            LogError(file, "Reading failed CSV file: " + ex.Message);
        }
    }

    static async Task ProcessLineAsync(string line, string filePath, string outputFolder)
    {
        try
        {
            var parts = line.Split(',');

            if (parts.Length == 3)
            {
                var dateStr = parts[0];
                var typeStr = parts[1];
                var amountStr = parts[2];

                if (DateTime.TryParse(dateStr, out DateTime date) &&
                    Enum.TryParse<TransactionType>(typeStr, true, out TransactionType transactionType) &&
                    decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal amount))
                {
                    var transaction = new Transaction
                    {
                        Date = date,
                        Type = transactionType,
                        Amount = amount
                    };

                    ProcessTransaction(transaction, outputFolder);
                }
                else
                {
                    LogError(filePath, "Incorrect CSV line format: " + line);
                }
            }
            else
            {
                LogError(filePath, "Incorrect CSV line: " + line);
            }
        }
        catch (Exception ex)
        {
            LogError(filePath, "Failed processing line: " + ex.Message);
        }
    }

    static void ProcessTransaction(Transaction transaction, string outputFolder)
    {
        if (transaction.Type == TransactionType.Deposit)
        {
            accountState.UpdateBalance(transaction.Amount);
            accountState.AddSuccessfulTransaction(transaction);
            LogTransaction(outputFolder, transaction, "Successfully");
        }
        else if (transaction.Type == TransactionType.Withdrawal)
        {
            if (accountState.CanWithdraw(transaction.Amount))
            {
                accountState.UpdateBalance(-transaction.Amount);
                accountState.AddSuccessfulTransaction(transaction);
                LogTransaction(outputFolder, transaction, "Successfully");
            }
            else
            {
                accountState.AddUnsuccessfulTransaction(transaction);
                LogTransaction(outputFolder, transaction, "Failed (Not enough funds)");
            }
        }
    }

    static void LogTransaction(string outputFolder, Transaction transaction, string status)
    {
        string logPath = Path.Combine(outputFolder, "transaction_log.csv");
        string logEntry = $"{transaction.Date},{transaction.Type},{transaction.Amount},{status}\n";
        lock (balanceLock)
        {
            File.AppendAllText(logPath, logEntry);
        }
    }

    static void LogError(string filePath, string error)
    {
        Console.WriteLine($"Failed. In File Find Error {filePath}: {error}");
    }
}
enum TransactionType
{
    Deposit,
    Withdrawal
}

class Transaction
{
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
}

class AccountState
{
    public decimal Balance { get; private set; }
    public int SuccessfulTransactions { get; private set; }
    public int UnsuccessfulTransactions { get; private set; }
    private readonly object balanceLock = new object();
    private readonly object successLock = new object();
    private readonly object failureLock = new object();

    public void UpdateBalance(decimal amount)
    {
        lock (balanceLock)
        {
            Balance += amount;
        }
    }

    public void AddSuccessfulTransaction(Transaction transaction)
    {
        lock (successLock)
        {
            SuccessfulTransactions++;
        }
    }

    public void AddUnsuccessfulTransaction(Transaction transaction)
    {
        lock (failureLock)
        {
            UnsuccessfulTransactions++;
        }
    }

    public bool CanWithdraw(decimal amount)
    {
        return Balance >= amount;
    }
}

class SummaryResult
{
    public decimal FinalBalance { get; set; }
    public int SuccessfulTransactions { get; set; }
    public int UnsuccessfulTransactions { get; set; }
    public int TotalFilesProcessed { get; set; }

    public string ToJson()
    {
        return $"{nameof(FinalBalance)}: {FinalBalance}, {nameof(SuccessfulTransactions)}: {SuccessfulTransactions}, {nameof(UnsuccessfulTransactions)}: {UnsuccessfulTransactions}, {nameof(TotalFilesProcessed)}: {TotalFilesProcessed}";
    }
}




//Task realisation


// using System;
// using System.Collections.Generic;
// using System.Globalization;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;

// class Emulator
// {
//     static async Task Main(string[] arguments)
//     {
//         string inputDirectory = "/Users/miroshnicheenko/Proj/emul1/emulator/input";
//         string outputDirectory = "/Users/miroshnicheenko/Proj/emul1/emulator/output";
//         var files = Directory.GetFiles(inputDirectory, "*.csv")
//                              .OrderBy(file => file)
//                              .ToList();

//         CardStatus cardStatus = new CardStatus();

//         await Task.WhenAll(files.Select(file => ProcessAsync(file, cardStatus, outputDirectory)));

//         Summary summary = new Summary
//         {
//             FinalBalance = cardStatus.Balance,
//             SuccessfulTransactions = cardStatus.SuccessfulTransactions,
//             UnsuccessfulTransactions = cardStatus.UnsuccessfulTransactions,
//             TotalProcessedFiles = files.Count
//         };

//         File.WriteAllText(Path.Combine(outputDirectory, "result.json"), summary.ToJson());
//     }

//     static async Task ProcessAsync(string filePath, CardStatus cardStatus, string outputDir)
//     {
//         try
//         {
//             var lines = await File.ReadAllLinesAsync(filePath);

//             foreach (var line in lines)
//             {
//                 var parts = line.Split(',');

//                 if (parts.Length == 3)
//                 {
//                     var dateStr = parts[0];
//                     var typeStr = parts[1];
//                     var amountStr = parts[2];

//                     if (DateTime.TryParse(dateStr, out DateTime date) &&
//                         Enum.TryParse<TransactionType>(typeStr, true, out TransactionType transactionType) &&
//                         decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal amount))
//                     {
//                         var transaction = new Transaction
//                         {
//                             Date = date,
//                             Type = transactionType,
//                             Amount = amount
//                         };

//                         ProcessTransaction(transaction, cardStatus, outputDir);
//                     }
//                     else
//                     {
//                         LogError(filePath, "Incorrect CSV line format: " + line);
//                     }
//                 }
//                 else
//                 {
//                     LogError(filePath, "Incorrect CSV line: " + line);
//                 }
//             }
//         }
//         catch (Exception ex)
//         {
//             LogError(filePath, "Reading failed CSV file: " + ex.Message);
//         }
//     }

//     static void ProcessTransaction(Transaction transaction, CardStatus cardStatus, string outputDir)
//     {
//         if (transaction.Type == TransactionType.Deposit)
//         {
//             cardStatus.UpdateBalance(transaction.Amount);
//             cardStatus.AddSuccessfulTransaction(transaction);
//             LogTransaction(outputDir, transaction, "Successfully");
//         }
//         else if (transaction.Type == TransactionType.Withdrawal)
//         {
//             if (cardStatus.CanWithdraw(transaction.Amount))
//             {
//                 cardStatus.UpdateBalance(-transaction.Amount);
//                 cardStatus.AddSuccessfulTransaction(transaction);
//                 LogTransaction(outputDir, transaction, "Successfully");
//             }
//             else
//             {
//                 cardStatus.AddUnsuccessfulTransaction(transaction);
//                 LogTransaction(outputDir, transaction, "Failed (Not enough funds)");
//             }
//         }
//     }

//     static void LogTransaction(string outputDir, Transaction transaction, string status)
//     {
//         string logPath = Path.Combine(outputDir, "transaction_log.csv");
//         string logEntry = $"{transaction.Date},{transaction.Type},{transaction.Amount},{status}\n";
//         File.AppendAllText(logPath, logEntry);
//     }

//     static void LogError(string filePath, string error)
//     {
//         Console.WriteLine($"Failed. In File Find Error {filePath}: {error}");
//     }
// }

// enum TransactionType
// {
//     Deposit,
//     Withdrawal
// }

// class Transaction
// {
//     public DateTime Date { get; set; }
//     public TransactionType Type { get; set; }
//     public decimal Amount { get; set; }
// }

// class CardStatus
// {
//     public decimal Balance { get; private set; }
//     public int SuccessfulTransactions { get; private set; }
//     public int UnsuccessfulTransactions { get; private set; }

//     public void UpdateBalance(decimal amount)
//     {
//         Balance += amount;
//     }

//     public void AddSuccessfulTransaction(Transaction transaction)
//     {
//         SuccessfulTransactions++;
//     }

//     public void AddUnsuccessfulTransaction(Transaction transaction)
//     {
//         UnsuccessfulTransactions++;
//     }

//     public bool CanWithdraw(decimal amount)
//     {
//         return Balance >= amount;
//     }
// }

// class Summary
// {
//     public decimal FinalBalance { get; set; }
//     public int SuccessfulTransactions { get; set; }
//     public int UnsuccessfulTransactions { get; set; }
//     public int TotalProcessedFiles { get; set; }

//     public string ToJson()
//     {
//         return $"{nameof(FinalBalance)}: {FinalBalance}, {nameof(SuccessfulTransactions)}: {SuccessfulTransactions}, {nameof(UnsuccessfulTransactions)}: {UnsuccessfulTransactions}, {nameof(TotalProcessedFiles)}: {TotalProcessedFiles}";
//     }
// }
