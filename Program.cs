using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace OvergownBot
{
    class Program
    {
        public static Sheet StrikeSheet = new Sheet(
            "Strike Sheet",
            new []
            {
                new Header { Name = "IN GAME NAME", Kind = CellKind.Name, Width = 1 },
                new Header { Name = "STEAM ID", Kind = CellKind.Id, Width = 1 },
                new Header { Name = "OFFENSE", Kind = CellKind.Offense, Width = 1 },
                new Header { Name = "REASON", Kind = CellKind.Reason, Width = 4 },
                new Header { Name = "REPORT", Kind = CellKind.Report, Width = 4 },
                new Header { Name = "DATE ISSUED", Kind = CellKind.Date, Width = 4 },
                new Header { Name = "LAST REMOVAL", Kind = CellKind.Date, Width = 1 },
            });

        public static Sheet VerbalSheet = new Sheet(
            "Verbal reminder Sheet",
            new[]
            {
                new Header { Name = "IN GAME NAME", Kind = CellKind.Name, Width = 1 },
                new Header { Name = "STEAM ID", Kind = CellKind.Id, Width = 1 },
                new Header { Name = "REASON", Kind = CellKind.Reason, Width = 5 },
                new Header { Name = "REPORT", Kind = CellKind.Report, Width = 5 },
                new Header { Name = "DATE ISSUED", Kind = CellKind.Date, Width = 5 },
            });
            
        
        static void Main(string[] args)
        {
            var r = new Requester();
            StrikeSheet.Init(r);
            VerbalSheet.Init(r);

            try
            {
                StrikeSheet.Validate();
                VerbalSheet.Validate();
            }
            catch (ValidationException e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}