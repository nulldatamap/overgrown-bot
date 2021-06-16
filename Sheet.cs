using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Apis.Http;

namespace OvergownBot
{
    public enum CellKind
    {
        Name,
        Id,
        Offense,
        Reason,
        Report,
        Date,
        Note
    }

    public static class Validator
    {
        private static readonly Regex _rSteamId =
            new Regex(@"\s*(NEED ID|(/?\s*7656\d{13})+)\s*", RegexOptions.Compiled);
        private static readonly Regex _rReportNumber =
            new Regex(@"\s*Report\s*(\d+)\s*(\s*\d+)*\s*",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex _rDate = 
            new Regex(@"\s*(\d{1,2})/(\d{1,2})/(\d{2}|\d{4})\s*");
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

    public class ValidationResults
    {
        struct InvalidCell
        {
            public int Row, Col;
            public string Value;
            public CellKind Kind;
        }
        
        private List<InvalidCell> _invalidCells = new List<InvalidCell>();

        public void AddInvalidCell(int row, int col, string v, CellKind k)
        {
            _invalidCells.Add(new InvalidCell
            {
                Row = row,
                Col = col,
                Value = v,
                Kind = k,
            });
        }

        public bool IsValid()
        {
            return _invalidCells.Count == 0;
        }

        public void ReportResults()
        {
            if (IsValid()) 
            { 
                Console.WriteLine("Valid");
                return;
            }

            foreach (var cell in _invalidCells)
            {
                var msg = string.IsNullOrEmpty(cell.Value) ? "<empty>" : cell.Value;
                Console.WriteLine($"{Utils.A1(cell.Col, cell.Row)}: Invalid {cell.Kind}: {msg}");
            }
        }
    }

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

    public struct Header
    {
        public string Name;
        public CellKind Kind;
        public int Width;
    }
    
    public class Sheet
    {
        public string Name { get; private set; }
        public Header[] Headers { get; private set; }

        private Requester _r;
        private int _width;
        

        public Sheet(string name, Header[] headers)
        {
            Name = name;
            Headers = headers;
            _width = headers.Sum(h => h.Width);
        }

        public void Init(Requester r)
        {
            _r = r;
        }

        public IList<IList<Object>> GetRange(int x0, int y0, int x1, int y1)
        {
            string range;
            if (x0 == x1 && y0 == y1)
            {
                range = $"{Name}!{Utils.R1C1(x0, y0)}";
            }
            else
            {
                range = $"{Name}!{Utils.R1C1(x0, y0)}:{Utils.R1C1(x1, y1)}";
            }
            return _r.RawExecute(range).Values;
        }

        public void ValidateHeaders()
        {
            var headerCells = GetRange(0, 0, 0, _width);
            if (headerCells?.Count != 1)
            {
                throw new ValidationException(Name, $"Malformed header row");
            }

            int j = 0;
            int k = 0;
            for (int i = 0; i < _width; i++)
            {
                var value = i >= headerCells[0].Count ? "" : headerCells[0][i];
                if (!value.Equals(j == 0 ? Headers[k].Name : ""))
                {
                    throw new ValidationException(
                        Name, 
                        j == 0 ?
                            $"expected column header '{Headers[k].Name}' got: '{value}'"
                            : $"expected empty header cell, got '{value}'");
                }

                j++;
                if (j == Headers[k].Width)
                {
                    j = 0;
                    k++;
                }
            }
        }

        public void ValidateValues()
        {
            var cells = _r.RawExecute($"{Name}!R2C1:C{1 + _width}").Values;
            var ks = Headers.SelectMany(h => Enumerable.Repeat(h.Kind, h.Width)).ToArray();
            var results = new ValidationResults();

            // One and not zero because 0 is the header row
            int row = 1;
            foreach (var rows in cells)
            {
                int col = 0;
                foreach (var (v, k) in rows.Zip(ks))
                {
                    if (!k.Validate(v))
                        // TODO: Eh this v as string is not good
                        results.AddInvalidCell(row, col, v as string, k);
                    col++;
                }
                row++;
            }

            results.ReportResults();
        }
        
        public void Validate()
        {
            Console.WriteLine($"Validating {Name}...");
            ValidateHeaders();
            ValidateValues();
        }
    }
}