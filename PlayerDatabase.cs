using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OvergownBot
{
    public enum PunishmentKind
    {
        Verbal,
        Warning,
        Strike
    }

    public enum PunishmentStatus
    {
        Active,
        Permanent,
        Removed
    }
    
    public class Punishment
    {
        public PunishmentKind Kind;
        public PunishmentStatus Status;
        public uint ReportId;
        public string Reason;
        public DateTime DateIssued;
        public DateTime DateRemoved;
    }
    
    public class Player
    {
        public string[] IGNs;
        public SteamUser[] Accounts;
        public Punishment[] Punishments;

        public static Player Build(Context ctx, string igns, string sids)
        {
                var names = igns
                    .Split(new []{'/', '|'})
                    .Select(x => x.Trim())
                    .Where(x => !String.IsNullOrEmpty(x))
                    .ToArray();
                var ids = sids
                    .Split(new []{'/', '|'})
                    .Select(x => x.Trim())
                    .Where(x => !String.IsNullOrEmpty(x))
                    .ToArray();

                var accounts = ids.Select(id => ctx.R.GetSteamUser(id)).ToArray();
                
                var p = new Player
                {
                    IGNs = names,
                    Accounts = accounts.Where(x => x != null).ToArray(),
                };

                foreach (var (id, acc) in ids.Zip(accounts))
                {
                    if (id.ToUpper() != "NEED ID" && acc == null)
                    {
                        ctx.VR.InvalidSteamId(id);
                    }
                }

                return p;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendJoin(" / ", Accounts.Select(x => x.personaname));
            sb.Append(" | ");
            sb.AppendJoin(" / ", Accounts.Select(x => x.steamid));
            sb.Append(" (aliases: ");
            sb.AppendJoin(" / ", IGNs);
            sb.Append(")");
            return sb.ToString();
        }
    }
    
    public class PlayerDatabase
    {
        public readonly List<Player> Players = new List<Player>();

        public bool AddPlayer(Player p, out (string, Player)? duplicatedId)
        {
            foreach (var player in Players)
            {
                foreach (var acc in player.Accounts)
                {
                    foreach (var pacc in p.Accounts)
                    {
                        if (pacc.steamid == acc.steamid)
                        {
                            duplicatedId = (pacc.steamid, player);
                            return false;
                        }
                    }
                }
            }
            
            Players.Add(p);
            duplicatedId = null;
            return true;
        } 

        public string Dump()
        {
            var sb = new StringBuilder();

            sb.AppendLine("==== PLAYER DATABASE ====");
            foreach (var player in Players)
            {
                var s = player.ToString();
                if (sb.Length + s.Length > 5000) break;
                sb.AppendLine(s);
            }
            

            return sb.ToString();
        }
    }
}