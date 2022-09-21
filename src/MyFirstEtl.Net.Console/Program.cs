using System;
using System.Threading.Tasks;
using Paillave.Etl.Core;
using Paillave.Etl.FileSystem;
using Paillave.Etl.Zip;
using Paillave.Etl.TextFile;
using Paillave.Etl.EntityFrameworkCore;
using System.Data.SqlClient;
using System.Linq;
using MyFirstEtl.Net.Console.Data;
using Microsoft.EntityFrameworkCore;


// See https://aka.ms/new-console-template for more information
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var processRunner = StreamProcessRunner.Create<string>(DefineProcess);
        processRunner.DebugNodeStream += (sender, e) => { /* place a conditional breakpoint here for debug */ };
        using (var cnx = new SampleContext())
        {
            await cnx.Database.EnsureCreatedAsync();
            var executionOptions = new ExecutionOptions<string>
            {
                Resolver = new SimpleDependencyResolver().Register(cnx),
            };
            var res = await processRunner.ExecuteAsync("SampleInput", executionOptions);
            Console.Write(res.Failed ? "Failed" : "Succeeded");
            if (res.Failed)
                Console.Write($"{res.ErrorTraceEvent.NodeName}({res.ErrorTraceEvent.NodeTypeName}):{res.ErrorTraceEvent.Content.Message}");
        }
        Console.WriteLine("Press Enter to end!");
        Console.ReadLine();
    }


    private static void DefineProcess(ISingleStream<string> contextStream)
    {
        contextStream
            .CrossApplyFolderFiles("list all required files", "*.zip", true)
            .CrossApplyZipFiles("extract files from zip", "*.csv")
            .CrossApplyTextFile("parse file", FlatFileDefinition.Create(i => new Person
            {
                Email = i.ToColumn("email"),
                FirstName = i.ToColumn("first name"),
                LastName = i.ToColumn("last name"),
                DateOfBirth = i.ToDateColumn("date of birth", "yyyy-MM-dd"),
                Reputation = i.ToNumberColumn<int?>("reputation", ".")
            }).IsColumnSeparated(','))
            .Distinct("exclude duplicates based on the Email", i => i.Email)
            .EfCoreSave("upsert using Email as key and ignore the Id", o => o
                .SeekOn(p => p.Email)
                )
            .Select("define row to report", i => new { i.Email, i.Id })
            .ToTextFileValue("write summary to file", "report.csv", FlatFileDefinition.Create(i => new
            {
                Email = i.ToColumn("Email"),
                Id = i.ToNumberColumn<int>("new or existing Id", ".")
            }).IsColumnSeparated(','))
            .WriteToFile("save log file", i => i.Name);
    }
}