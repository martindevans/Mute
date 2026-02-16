using BalderHash;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Mute.Tests;

[TestClass]
public class BalderhashTests
{
    [TestMethod]
    public void KnownValues()
    {
        // It's critical that these **DO NOT CHANGE**. They're used as IDs for the transaction system

        var values = new[]
        {
                "pagmyr-hapnex", "dacbyr-satbex", "sabned-boldex", "pagmet-novwes", "pagpyx-pagnub", "minlud-mignec", "motpun-foltyc", "mattyr-famtyc",
                "somwyn-dalryc", "torseg-bolmed", "ropdus-nibter", "satnul-filwes", "tipteg-larpub", "socmep-finhes", "fodneb-morwyn", "tipweb-sitfep",
                "magluc-dovbep", "tomsyt-sitreg", "raprud-tocdem", "lonreg-somret", "tadlyn-borled", "tidret-narrus", "sonlys-dasdyr", "socrun-sabpub",
                "nillyd-simlux", "naphep-parlyr", "ricmer-sollus", "palrut-sidwyc", "ponhep-rablex", "fosfet-nibfep", "sogset-namned", "doldyr-tarryd",
                "novsut-libpub", "racmep-lopdel", "nalnep-lodrud", "lisnys-ricluc", "hilpyl-labsyr", "wicbyn-morfep", "falweg-fotder", "sitlyn-mochec",
                "dosmev-mosryx", "firlec-lomnum", "balrut-wicber", "libwes-hatnys", "wacmyl-mirsyx", "palsed-sonmep", "sicdex-divpex", "nodbyt-palmec",
                "mosrep-noches", "binfer-migrex", "maldec-dolrun", "labluc-bidryp", "mattyv-lodrud", "raddyl-dindex", "habryg-masbel", "nodwet-dilryn",
                "sonsyp-nidped", "tamlys-tonryg", "sopmel-marduc", "lisryg-pilbes", "motsun-pagwex", "lombyl-wanpen", "tidser-ticfet", "foprul-lignet",
                "rosmyr-todwyx", "ralnep-dantuc", "diblug-sorsut", "soctep-bintul", "podpur-fipdeb", "falbel-nibrev", "tirbyr-nistep", "datbyr-baclex",
                "sorsyp-hinlur", "sitdyt-fiprud", "tansug-nidsub", "bicfed-dovfep", "bacset-dabnys", "libmug-rocpyl", "binrup-mittyr", "satder-wordut",
                "barmud-foldex", "pitpur-taltyp", "dappen-tamdet", "radlug-follug", "ridnyd-rolred", "modnub-siptyp", "bismul-silnus", "libsyp-livsyp",
                "hadfus-roptyp", "fidfyl-sipsub", "bicrud-hilsyn", "middeb-lisfel", "tapnyd-lodlyt", "botbyt-listun", "pattuc-ribdyt", "lanrem-daptep",
                "laglug-rismyn", "rillev-massel", "litmud-patdul", "rithul-mocdyr", "patfeb-lisnul", "parsym-habdex", "sogdun-hocref", "sovsym-locrec",
                "tacsut-hasned", "bidned-famtyr", "tarsud-marper", "navlev-lorlep", "matsec-magmur", "timsut-daswyt", "sanduc-motbyl", "libdux-rapteg",
                "livdut-hadrym", "dopwyn-monmul", "lavdut-bacnec", "pilwen-sornet", "wistex-lontec", "parsec-malwet", "lidrel-nidsyp", "nibmyl-rigmex",
                "sattyd-pidbyl", "wintud-poctus", "wolber-pagsyp", "namdyl-tanbet", "sogmes-sonreg", "masmex-nilmes", "dinbyr-sogsem", "timryp-siltun"

            };

        var r = new Random(346234);

        for (var i = 0; i < 128; i++)
            Assert.AreEqual(values[i], new BalderHash32(unchecked((uint)r.Next())).ToString());
    }
}