using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Google.Apis.Http;
using Google.Apis.Sheets.v4.Data;

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

    public struct Header
    {
        public string Name;
        public CellKind Kind;
        public int Width;
    }
    
    public class Sheet : BaseSheet
    {
        public Header[] Headers { get; private set; }

        private int _width;
        private IList<IList<object>> _cells;
        private PlayerDatabase _players = new PlayerDatabase();
        private int _playerNameIdx = -1;
        private int _playerIdIdx = -1;

        public Sheet(string name, Header[] headers) : base(name)
        {
            Headers = headers;
            _width = headers.Sum(h => h.Width);
        }

        public string DumpHeaders()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Headers.Length; i++)
            {
                if (i != 0) sb.Append("|");
                var h = Headers[i];
                sb.Append($"{h.Name}:{h.Kind}:{h.Width}");
            }

            return sb.ToString();
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
            return _ctx.R.Query(range).Values;
        }

        public void ValidateHeaders()
        {
            var headerCells = GetRange(0, 0, _width, 0 );
            if (headerCells?.Count != 1)
            {
                throw new ValidationException(Name, $"Malformed header row");
            }

            int j = 0;
            int k = 0;
            for (int i = 0; i < _width; i++)
            {
                var value = i >= headerCells[0].Count ? "" : headerCells[0][i];

                if (Headers[k].Kind == CellKind.Name) _playerNameIdx = k;
                else if (Headers[k].Kind == CellKind.Id) _playerIdIdx = k;
                    
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
            _cells = _ctx.R.Query($"{Name}!R2C1:C{1 + _width}").Values;
            var ks = Headers.SelectMany(h => Enumerable.Repeat(h.Kind, h.Width)).ToArray();

            // One and not zero because 0 is the header row
            int row = 1;
            foreach (var rows in _cells)
            {
                int col = 0;
                foreach (var (v, k) in rows.Zip(ks))
                {
                    if (!k.Validate(v))
                    {
                        // TODO: Eh this v as string is not good
                        _ctx.VR.AddInvalidCell(Name, row, col, v as string, k);
                    }

                    col++;
                }
                row++;
            }
        }
        
        public void Validate()
        {
            ValidateHeaders();
            ValidateValues();
        }

        public void BuildPlayerDatabase()
        {
            _ctx.R.CacheSteamIds(_cells.Where(e => e != null && e.Count > _playerIdIdx)
                .SelectMany(e => ((string) e?[_playerIdIdx])
                .Split('/')
                .Select(x => x.Trim())));
            
            var db = new PlayerDatabase();
            foreach (var entry in _cells)
            {
                if (entry == null || entry.Count <= Math.Max(_playerNameIdx, _playerIdIdx)) continue;
                
                var p = Player.Build(_ctx, (string) entry[_playerNameIdx], (string) entry[_playerIdIdx]);
                if (!db.AddPlayer(p, out var dup))
                {
                    _ctx.VR.DuplicateSteamId(Name, dup?.Item1, p, dup?.Item2);
                }
            }
        }

        public static Sheet FromFormat(string name, string format)
        {
            var headers =
                format.Split('|')
                .Select(f =>
                {
                    var xs = f.Split(':');
                    if (xs.Length != 3) throw new Exception("Invalid header format");
                    return new Header()
                    {
                        Name = xs[0],
                        Kind = Enum.Parse<CellKind>(xs[1], true),
                        Width = int.Parse(xs[2]),
                    };
                })
                .ToArray();
            
            return new Sheet(name, headers);
        }
    }
}