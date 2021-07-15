using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OvergownBot
{
    public class Context
    {
        public Requester R;
        public PlayerDatabase DB;
        public DashboardSheet D;
        public ValidationResults VR;
        public List<Sheet> S;
    }
    
    class Program
    {

        public static DashboardSheet Dashboard = new DashboardSheet("Bot Dashboard")
        {
            OutputCell = (0, 4),
            LastUpdatedCell = (6, 3),
            ConfigStart = (7, 2),
            NumberOfProperties = 2,
            HeaderConfigStart = (9, 2),
            NumberOfSheets = 8,
        };
        
        static async Task Main(string[] args)
        {
            Context ctx;
            
            try
            {
                ctx = Init();
                foreach (var sheet in ctx.S)
                {
                    Console.WriteLine($"{sheet.Name}\t{sheet.DumpHeaders()}");
                }
            }
            catch (Exception e)
            {
                Dashboard.Error(e);
                return;
            }

            while (true)
            {
                try
                {
                    RunChecks(ctx);
                }
                catch (Exception e)
                {
                    Dashboard.Error(e);
                    Console.WriteLine(e);
                }

                await Task.Delay(TimeSpan.FromMinutes(Dashboard.CheckInteraval));
            }
        }

        public static Context Init()
        {
            var r = new Requester();
            
            var ctx = new Context();
            ctx.R = r;
            Dashboard.Init(ctx);
            ctx.DB = new PlayerDatabase();
            ctx.D = Dashboard;
            ctx.VR = new ValidationResults();
            ctx.S = Dashboard.ParseSheetFormats();
            
            foreach (var sheet in ctx.S)
            {
                sheet.Init(ctx);
            }

            return ctx;
        }

        public static void RunChecks(Context ctx)
        {
            Console.WriteLine("Running checks...");
            ctx.VR.Clear();
            var sb = new StringBuilder();
            try
            {
                ctx.D.LoadConfig();
                
                foreach (var sheet in ctx.S)
                {
                    Console.WriteLine($"Validating {sheet.Name}...");
                    sheet.Validate();
                    Console.WriteLine($"Building player database for {sheet.Name}...");
                    sheet.BuildPlayerDatabase();
                }
                
                sb.AppendLine(ctx.VR.ReportResults());
                if (ctx.D.DumpPlayerDatabase)
                {
                    sb.AppendLine(ctx.DB.Dump());
                }
            }
            catch (ValidationException e)
            {
                sb.AppendLine(e.Message);
            }

            var diagnostics = sb.ToString();
            Console.WriteLine(diagnostics);
            Dashboard.Publish(diagnostics);
        }
    }
}