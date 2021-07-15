using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4;

namespace OvergownBot
{
    enum PropertyType
    {
        String,
        Number
    }

    public class DashboardSheet : BaseSheet
    {

        public (int, int) OutputCell;
        public (int, int) LastUpdatedCell;
        public (int, int) ConfigStart;
        public int NumberOfProperties;
        public (int, int) HeaderConfigStart;
        public int NumberOfSheets;

        public float CheckInteraval { get; private set; }
        public bool DumpPlayerDatabase { get; private set; }

        public DashboardSheet(string name) : base(name)
        {
        }

        public void LoadConfig()
        {
            // Read configuration
            var (cx, cy) = ConfigStart;
            var vals =
                _ctx.R.Query($"{Name}!{Utils.R1C1(cx + 1, cy)}:{Utils.R1C1(cx + 1, cy + NumberOfProperties - 1)}")
                    .Values;
            var props = vals
                    .SelectMany(x => x ?? new List<object>())
                    .Cast<string>()
                    .ToArray();

            CheckInteraval = float.Parse(props[0]);
            DumpPlayerDatabase = bool.Parse(props[1]);
        }

        public void UpdateLastUpdate()
        {
            var date = DateTime.Now;
            date = date.ToUniversalTime();
            var (x, y) = LastUpdatedCell;
            _ctx.R.WriteSingle($"{Name}!{Utils.R1C1(x, y)}", Utils.EST(date));
        }

        public void Publish(string msg)
        {
            var (x, y) = OutputCell;
            _ctx.R.WriteSingle($"{Name}!{Utils.R1C1(x, y)}", msg);

            UpdateLastUpdate();
        }

        public void Error(Exception e)
        {
            Publish($"Something went wrong! Show this to Rocket:\n\n{e}");
        }

        public List<Sheet> ParseSheetFormats()
        {
            var (cx, cy) = HeaderConfigStart;
            var vals =
                _ctx.R.Query($"{Name}!{Utils.R1C1(cx, cy)}:{Utils.R1C1(cx + 1, cy + NumberOfSheets - 1)}")
                    .Values;

            var sheets = new List<Sheet>();
            foreach (var entry in vals)
            {
                if (entry == null) continue;
                if (entry.Count != 2) throw new Exception($"Invalid spreadsheet config! Expected 2 columns got {entry.Count}");
                var name = (string)entry[0];
                var format = (string)entry[1];
                if (name == null || format == null) throw new Exception("Invalid spreadsheet config!");

                sheets.Add(Sheet.FromFormat(name, format));
            }

            return sheets;
        }
    }
}