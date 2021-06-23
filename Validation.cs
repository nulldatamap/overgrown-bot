using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;

namespace OvergownBot
{
    public class ValidationException : Exception
    {
        public string Sheet;
        public string Reason;

        public override string Message => $"Invalid sheet {Sheet}: {Reason}";

        public ValidationException(string sheet, string reason)
        {
            Sheet = sheet;
            Reason = reason;
        }
    }

    public class ValidationResults
    {
        struct InvalidCell
        {
            public string Sheet;
            public int Row, Col;
            public string Value;
            public CellKind Kind;
        }

        private Dictionary<(string, string), List<Player>> _duplicatedSteamIds = new Dictionary<(string, string), List<Player>>();
        private List<InvalidCell> _invalidCells = new List<InvalidCell>();
        private List<string> _invalidSteamIds = new List<string>();

        public void Clear()
        {
            _invalidCells.Clear();
            _invalidSteamIds.Clear();
            _duplicatedSteamIds.Clear();
        }

        public void AddInvalidCell(string sheet, int row, int col, string v, CellKind k)
        {
            _invalidCells.Add(new InvalidCell
            {
                Sheet = sheet,
                Row = row,
                Col = col,
                Value = v,
                Kind = k,
            });
        }

        public void InvalidSteamId(string id)
        {
            _invalidSteamIds.Add(id);
        }

        public string ReportResults()
        {
            var s = new StringBuilder();

            foreach (var entries in _duplicatedSteamIds.GroupBy(kv => kv.Key.Item1))
            {
                var sheet = entries.Key;
                s.AppendLine($"Duplicated steam IDs in {sheet}");
                foreach (var ((_, id), dups) in entries)
                {
                    s.AppendLine($"Steam ID '{id}' occours in both of these users:");
                    foreach (var p in dups)
                        s.AppendLine($"  {p}");
                    
                    s.AppendLine();
                }

                s.AppendLine();
            }

            foreach (var invCells in _invalidCells.GroupBy(x => x.Sheet))
            {
                s.AppendLine($"Invalid cells in: {invCells.Key}");
                foreach (var cell in invCells)
                {
                    var msg = string.IsNullOrEmpty(cell.Value) ? "<empty>" : cell.Value;
                    s.AppendLine($"{Utils.A1(cell.Col, cell.Row)}: Invalid {cell.Kind}: {msg}");
                }

                s.AppendLine();
            }

            s.AppendLine();

            if (_invalidSteamIds.Count != 0)
            {
                s.AppendLine("Invalid steam ids:");
                foreach (var id in _invalidSteamIds.OrderBy(x => x))
                {
                    s.AppendLine(id);
                }
            }
            
            return s.ToString();
        }

        public void DuplicateSteamId(string sheet, string id, Player p0, Player p1)
        {
            if (_duplicatedSteamIds.TryGetValue((sheet, id), out var entry))
            {
                if (!entry.Contains(p0)) entry.Add(p0);
                if (!entry.Contains(p1)) entry.Add(p1);
            }
            else
            {
                _duplicatedSteamIds.Add((sheet, id), new List<Player>() { p0, p1 });
            }
        }
    }

    public static class Validator
    {
        private static readonly Regex _rSteamId =
            new Regex(@"\s*(NEED ID|(/?\s*7656\d{13})+)\s*", RegexOptions.Compiled);

        private static readonly Regex _rReportNumber =
            new Regex(@"\s*Report\s*(\d+)\s*(\s*\d+)*\s*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rDate =
            new Regex(@"\s*(no appeal|appeal deined.+|(\d{1,2})/(\d{1,2})/(\d{2}|\d{4})\s*(|.+)?)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex _rOffense =
            new Regex(@"\s*(Warning|Strike *\d+)\s*(.*)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool Validate(this CellKind k, object v)
        {
            Regex re = null;
            bool canBeEmpty = true;
            switch (k)
            {
                case CellKind.Id:
                    re = _rSteamId;
                    canBeEmpty = false;
                    break;
                case CellKind.Report:
                    re = _rReportNumber;
                    break;
                case CellKind.Date:
                    re = _rDate;
                    break;
                case CellKind.Offense:
                    re = _rOffense;
                    break;
                default:
                    break;
            }

            var x = v as string ?? "";
            if (re == null || canBeEmpty && x == "") return true;
            return re.IsMatch(x);
        }
    }
}