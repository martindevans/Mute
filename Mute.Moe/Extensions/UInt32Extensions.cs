using System;


namespace Mute.Moe.Extensions
{
    public struct FriendlyId32
    {
        private static readonly string[] Prefixes = {
            "doz", "mar", "bin", "wan", "sam", "lit", "sig", "hid",
            "fid", "lis", "sog", "dir", "wac", "sab", "wis", "sib",
            "rig", "sol", "dop", "mod", "fog", "lid", "hop", "dar",
            "dor", "lor", "hod", "fol", "rin", "tog", "sil", "mir",
            "hol", "pas", "lac", "rov", "liv", "dal", "sat", "lib",
            "tab", "han", "tic", "pid", "tor", "bol", "fos", "dot",
            "los", "dil", "for", "pil", "ram", "tir", "win", "tad",
            "bic", "dif", "roc", "wid", "bis", "das", "mid", "lop",
            "ril", "nar", "dap", "mol", "san", "loc", "nov", "sit",
            "nid", "tip", "sic", "rop", "wit", "nat", "pan", "min",
            "rit", "pod", "mot", "tam", "tol", "sav", "pos", "nap",
            "nop", "som", "fin", "fon", "ban", "mor", "wor", "sip",
            "ron", "nor", "bot", "wic", "soc", "wat", "dol", "mag",
            "pic", "dav", "bid", "bal", "tim", "tas", "mal", "lig",
            "siv", "tag", "pad", "sal", "div", "dac", "tan", "sid",
            "fab", "tar", "mon", "ran", "nis", "wol", "mis", "pal",
            "las", "dis", "map", "rab", "tob", "rol", "lat", "lon",
            "nod", "nav", "fig", "nom", "nib", "pag", "sop", "ral",
            "bil", "had", "doc", "rid", "moc", "pac", "rav", "rip",
            "fal", "tod", "til", "tin", "hap", "mic", "fan", "pat",
            "tac", "lab", "mog", "sim", "son", "pin", "lom", "ric",
            "tap", "fir", "has", "bos", "bat", "poc", "hac", "tid",
            "hav", "sap", "lin", "dib", "hos", "dab", "bit", "bar",
            "rac", "par", "lod", "dos", "bor", "toc", "hil", "mac",
            "tom", "dig", "fil", "fas", "mit", "hob", "har", "mig",
            "hin", "rad", "mas", "hal", "rag", "lag", "fad", "top",
            "mop", "hab", "nil", "nos", "mil", "fop", "fam", "dat",
            "nol", "din", "hat", "nac", "ris", "fot", "rib", "hoc",
            "nim", "lar", "fit", "wal", "rap", "sar", "nal", "mos",
            "lan", "don", "dan", "lad", "dov", "riv", "bac", "pol",
            "lap", "tal", "pit", "nam", "bon", "ros", "ton", "fod",
            "pon", "sov", "noc", "sor", "lav", "mat", "mip", "fip"
        };
        private static readonly string[] Suffixes = {
            "zod", "nec", "bud", "wes", "sev", "per", "sut", "let",
            "ful", "pen", "syt", "dur", "wep", "ser", "wyl", "sun",
            "ryp", "syx", "dyr", "nup", "heb", "peg", "lup", "dep",
            "dys", "put", "lug", "hec", "ryt", "tyv", "syd", "nex",
            "lun", "mep", "lut", "sep", "pes", "del", "sul", "ped",
            "tem", "led", "tul", "met", "wen", "byn", "hex", "feb",
            "pyl", "dul", "het", "mev", "rut", "tyl", "wyd", "tep",
            "bes", "dex", "sef", "wyc", "bur", "der", "nep", "pur",
            "rys", "reb", "den", "nut", "sub", "pet", "rul", "syn",
            "reg", "tyd", "sup", "sem", "wyn", "rec", "meg", "net",
            "sec", "mul", "nym", "tev", "web", "sum", "mut", "nyx",
            "rex", "teb", "fus", "hep", "ben", "mus", "wyx", "sym",
            "sel", "ruc", "dec", "wex", "syr", "wet", "dyl", "myn",
            "mes", "det", "bet", "bel", "tux", "tug", "myr", "pel",
            "syp", "ter", "meb", "set", "dut", "deg", "tex", "sur",
            "fel", "tud", "nux", "rux", "ren", "wyt", "nub", "med",
            "lyt", "dus", "neb", "rum", "tyn", "seg", "lyx", "pun",
            "res", "red", "fun", "rev", "ref", "mec", "ted", "rus",
            "bex", "leb", "dux", "ryn", "num", "pyx", "ryg", "ryx",
            "fep", "tyr", "tus", "tyc", "leg", "nem", "fer", "mer",
            "ten", "lus", "nus", "syl", "tec", "mex", "pub", "rym",
            "tuc", "fyl", "lep", "deb", "ber", "mug", "hut", "tun",
            "byl", "sud", "pem", "dev", "lur", "def", "bus", "bep",
            "run", "mel", "pex", "dyt", "byt", "typ", "lev", "myl",
            "wed", "duc", "fur", "fex", "nul", "luc", "len", "ner",
            "lex", "rup", "ned", "lec", "ryd", "lyd", "fen", "wel",
            "nyd", "hus", "rel", "rud", "nes", "hes", "fet", "des",
            "ret", "dun", "ler", "nyr", "seb", "hul", "ryl", "lud",
            "rem", "lys", "fyn", "wer", "ryc", "sug", "nys", "nyl",
            "lyn", "dyn", "dem", "lux", "fed", "sed", "bec", "mun",
            "lyr", "tes", "mud", "nyt", "byr", "sen", "weg", "fyr",
            "mur", "tel", "rep", "teg", "pec", "nel", "nev", "fes"
        };

        private const uint Offset = 1646229403;

        private const uint Multiply = 143435305;
        private const uint MultiplyInverse = 824333849;

        public uint Value { get; }

        public FriendlyId32(uint value)
        {
            Value = value;
        }

        public static FriendlyId32? Parse( string str)
        {
            var parts = str.Split('-');
            if (parts.Length != 2)
                return null;

            var ab = parts[0];
            if (ab.Length != 6)
                return null;
            var cd = parts[1];
            if (cd.Length != 6)
                return null;

            var a = ab.Substring(0, 3);
            var b = ab.Substring(3, 3);
            var c = cd.Substring(0, 3);
            var d = cd.Substring(3, 3);

            var an = Array.IndexOf(Prefixes, a);
            if (an < 0)
                return null;

            var bn = Array.IndexOf(Suffixes, b);
            if (bn < 0)
                return null;

            var cn = Array.IndexOf(Prefixes, c);
            if (cn < 0)
                return null;

            var dn = Array.IndexOf(Suffixes, d);
            if (dn < 0)
                return null;

            uint number = 0;
            unsafe
            {
                // ReSharper disable once ObjectCreationAsStatement (assigning into the underlying pointer)
                new Span<byte>(&number, sizeof(uint)) {
                    [0] = (byte)an,
                    [2] = (byte)bn,
                    [3] = (byte)cn,
                    [1] = (byte)dn,
                };
            }

            unchecked
            {
                number *= MultiplyInverse;
                number -= Offset;
            }

            return new FriendlyId32(number);
        }

         public override string ToString()
        {
            var number = Value;
            unchecked {
                number += Offset;
                number *= Multiply;
            }

            unsafe
            {
                var bytes = new Span<byte>(&number, sizeof(uint));

                var a = Prefixes[bytes[0]];
                var b = Suffixes[bytes[2]];
                var c = Prefixes[bytes[3]];
                var d = Suffixes[bytes[1]];

                return $"{a}{b}-{c}{d}";
            }
        }
    }

    public static class UInt32Extensions
    {
         public static string MeaninglessString(this uint number)
        {
            return new FriendlyId32(number).ToString();
        }
    }
}
